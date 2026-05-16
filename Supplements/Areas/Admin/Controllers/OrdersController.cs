using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Supplements.Infrastructure.Data;
using Supplements.ViewModels.Admin;

namespace Supplements.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class OrdersController : Controller
{
    private readonly AppDbContext _context;

    public OrdersController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? status, int page = 1)
    {
        var query = _context.Orders
            .Include(o => o.User)
            .Where(o => !o.IsDeleted);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(o => o.Status == status);

        var pageSize = 20;
        var total = await query.CountAsync();
        var orders = await query
            .OrderByDescending(o => o.OrderDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Status = status;
        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
        ViewBag.Statuses = new[] { "Pending", "Processing", "Shipped", "Delivered", "Cancelled" };

        return View(orders);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(o => o.User)
            .Include(o => o.Address)
            .Include(o => o.Items)
            .ThenInclude(i => i.ProductVariant)
            .ThenInclude(v => v.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();

        var items = order.Items
            .Where(i => !i.IsDeleted && i.ProductVariant != null && i.ProductVariant.Product != null)
            .Select(i => new OrderItemViewModel
            {
                ProductName = i.ProductVariant.Product.Name,
                Flavor = i.ProductVariant.Flavor,
                Size = i.ProductVariant.Size,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                TotalPrice = i.TotalPrice
            }).ToList();

        var viewModel = new OrderDetailsViewModel
        {
            Id = order.Id,
            Status = order.Status,
            OrderDate = order.OrderDate,
            TotalPrice = order.TotalPrice,
            ShippingCost = order.ShippingCost,
            Notes = order.Notes,
            CustomerName = order.User?.FullName ?? "N/A",
            CustomerEmail = order.User?.Email ?? "N/A",
            AddressStreet = order.Address?.Street ?? "",
            AddressCity = order.Address?.City ?? "",
            AddressCountry = order.Address?.Country ?? "",
            Items = items
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(Guid id, string status)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);

        if (order == null) return NotFound();

        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Order status updated to {status}";
        return RedirectToAction("Details", new { id });
    }
}


