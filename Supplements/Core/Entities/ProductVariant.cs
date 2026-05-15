using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Supplements.Core.Entities;
public class ProductVariant : BaseEntity
{
    public Guid ProductId { get; set; }

    [ValidateNever]
    public Product Product { get; set; } = null!;

    public string? Flavor { get; set; }

    public string? Size { get; set; }

    public string? ImageUrl { get; set; }

    public decimal AdditionalPrice { get; set; }

    public int StockQuantity { get; set; }

    [ValidateNever]
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    [ValidateNever]
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
