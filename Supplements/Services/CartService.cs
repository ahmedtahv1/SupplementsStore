using System.Data;
using Microsoft.EntityFrameworkCore;
using Supplements.Core;
using Supplements.Core.DTOs.Cart;
using Supplements.Core.Entities;
using Supplements.Infrastructure.Data;

namespace Supplements.Services;

public class CartService
{
    private readonly AppDbContext _context;
    private const int AddToCartMaxRetries = 3;

    public CartService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<CartResponse>> AddToCart(Guid userId, AddToCartRequest request)
    {
        if (request.Quantity <= 0)
            return Result<CartResponse>.Failure("Quantity must be greater than zero");

        for (var attempt = 1; attempt <= AddToCartMaxRetries; attempt++)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var variant = await _context.ProductVariants
                    .Include(v => v.Product)
                    .FirstOrDefaultAsync(v => v.Id == request.ProductVariantId && !v.IsDeleted);

                if (variant == null)
                    return Result<CartResponse>.Failure("Product variant not found");

                var cart = await _context.Carts
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null)
                {
                    cart = new Cart { Id = Guid.NewGuid(), UserId = userId };
                    _context.Carts.Add(cart);
                    await _context.SaveChangesAsync();
                }

                var existingItem = await _context.CartItems
                    .FirstOrDefaultAsync(i => i.CartId == cart.Id && i.ProductVariantId == request.ProductVariantId);

                var targetQuantity = request.Quantity + (existingItem?.Quantity ?? 0);
                if (variant.StockQuantity < targetQuantity)
                    return Result<CartResponse>.Failure("Not enough stock available");

                if (existingItem == null)
                {
                    _context.CartItems.Add(new CartItem
                    {
                        Id = Guid.NewGuid(),
                        CartId = cart.Id,
                        ProductVariantId = request.ProductVariantId,
                        Quantity = request.Quantity
                    });
                }
                else
                {
                    existingItem.Quantity = targetQuantity;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Result<CartResponse>.Success(await BuildCartResponse(cart.Id));
            }
            catch (DbUpdateConcurrencyException) when (attempt < AddToCartMaxRetries)
            {
                await transaction.RollbackAsync();
                _context.ChangeTracker.Clear();
            }
            catch (DbUpdateException) when (attempt < AddToCartMaxRetries)
            {
                await transaction.RollbackAsync();
                _context.ChangeTracker.Clear();
            }
        }

        return Result<CartResponse>.Failure("Could not update cart right now. Please try again.");
    }

    public async Task<Result> RemoveFromCart(Guid userId, Guid cartItemId)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        var item = cart?.Items.FirstOrDefault(i => i.Id == cartItemId);
        if (item == null)
            return Result.Failure("Cart item not found");

        _context.CartItems.Remove(item);
        await _context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> UpdateQuantity(Guid userId, Guid cartItemId, int quantity)
    {
        if (quantity <= 0)
            return Result.Failure("Quantity must be greater than zero");

        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        var item = cart?.Items.FirstOrDefault(i => i.Id == cartItemId);
        if (item == null)
            return Result.Failure("Cart item not found");

        var variant = await _context.ProductVariants.FindAsync(item.ProductVariantId);
        if (variant == null)
            return Result.Failure("Product variant not found");

        if (variant.StockQuantity < quantity)
            return Result.Failure("Not enough stock available");

        item.Quantity = quantity;
        await _context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<CartResponse>> GetCart(Guid userId)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.ProductVariant)
            .ThenInclude(v => v.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
            return Result<CartResponse>.Failure("Cart not found");

        return Result<CartResponse>.Success(new CartResponse
        {
            CartId = cart.Id,
            Items = cart.Items.Select(i =>
            {
                var unitPrice = GetFinalUnitPrice(i.ProductVariant);
                return new CartItemResponse
                {
                    Id = i.Id,
                    ProductVariantId = i.ProductVariantId,
                    ProductName = i.ProductVariant.Product.Name,
                    ImageUrl = i.ProductVariant.ImageUrl ?? i.ProductVariant.Product.MainImageUrl,
                    Currency = i.ProductVariant.Product.Currency,
                    Flavor = i.ProductVariant.Flavor,
                    Size = i.ProductVariant.Size,
                    UnitPrice = unitPrice,
                    Quantity = i.Quantity,
                    TotalPrice = unitPrice * i.Quantity
                };
            }).ToList(),
            TotalPrice = cart.Items.Sum(i => GetFinalUnitPrice(i.ProductVariant) * i.Quantity)
        });
    }

    private async Task<CartResponse> BuildCartResponse(Guid cartId)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.ProductVariant)
            .ThenInclude(v => v.Product)
            .FirstOrDefaultAsync(c => c.Id == cartId);

        return new CartResponse
        {
            CartId = cart!.Id,
            Items = cart.Items.Select(i =>
            {
                var unitPrice = GetFinalUnitPrice(i.ProductVariant);
                return new CartItemResponse
                {
                    Id = i.Id,
                    ProductVariantId = i.ProductVariantId,
                    ProductName = i.ProductVariant.Product.Name,
                    ImageUrl = i.ProductVariant.ImageUrl ?? i.ProductVariant.Product.MainImageUrl,
                    Currency = i.ProductVariant.Product.Currency,
                    Flavor = i.ProductVariant.Flavor,
                    Size = i.ProductVariant.Size,
                    UnitPrice = unitPrice,
                    Quantity = i.Quantity,
                    TotalPrice = unitPrice * i.Quantity
                };
            }).ToList(),
            TotalPrice = cart.Items.Sum(i => GetFinalUnitPrice(i.ProductVariant) * i.Quantity)
        };
    }

    private static decimal GetFinalUnitPrice(ProductVariant variant)
    {
        var unitPrice = variant.Product.Price + variant.AdditionalPrice;
        return unitPrice - (unitPrice * variant.Product.DiscountPercentage / 100);
    }
}
