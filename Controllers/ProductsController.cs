using EcommerceApp.Data;
using EcommerceApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApp.Controllers;

[Authorize]
public class ProductsController(ApplicationDbContext context) : Controller
{
    private static readonly string[] KnownCategories =
    [
        "Nectar TropiValle",
        "Agua de Mesa"
    ];

    [AllowAnonymous]
    public async Task<IActionResult> Index(string? category, string? sort, string? view)
    {
        category = string.IsNullOrWhiteSpace(category) ? "todo" : category.Trim();
        sort = string.IsNullOrWhiteSpace(sort) ? "categoria" : sort.Trim().ToLowerInvariant();
        view = string.IsNullOrWhiteSpace(view) ? "instaview" : view.Trim().ToLowerInvariant();
        if (view is not ("instaview" or "lista"))
            view = "instaview";

        var query = context.Products.AsNoTracking().AsQueryable();

        if (!string.Equals(category, "todo", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(p => p.Category != null && p.Category == category);
        }

        query = sort switch
        {
            "precio_asc" or "menor" => query.OrderBy(p => p.Price).ThenBy(p => p.Name),
            "precio_desc" or "mayor" => query.OrderByDescending(p => p.Price).ThenBy(p => p.Name),
            "az" => query.OrderBy(p => p.Name),
            "za" => query.OrderByDescending(p => p.Name),
            _ => query.OrderBy(p => p.Category).ThenBy(p => p.Name)
        };

        var products = await query.ToListAsync();

        ViewBag.Category = category;
        ViewBag.Sort = sort is "menor" ? "precio_asc"
            : sort is "mayor" ? "precio_desc"
            : sort;
        ViewBag.ViewMode = view;
        ViewBag.Categories = KnownCategories;
        ViewBag.TotalCount = products.Count;

        return View(products);
    }

    [AllowAnonymous]
    public async Task<IActionResult> Details(int id)
    {
        var product = await context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
            return NotFound();

        return View(product);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public IActionResult Create() => View();

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Product product)
    {
        if (!ModelState.IsValid)
            return View(product);

        product.CreatedAt = DateTime.UtcNow;
        context.Products.Add(product);
        await context.SaveChangesAsync();

        TempData["Success"] = "Producto creado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var product = await context.Products.FindAsync(id);
        if (product is null)
            return NotFound();
        return View(product);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Product product)
    {
        if (id != product.Id)
            return NotFound();

        if (!ModelState.IsValid)
            return View(product);

        var existing = await context.Products.FindAsync(id);
        if (existing is null)
            return NotFound();

        existing.Name = product.Name;
        existing.Description = product.Description;
        existing.Price = product.Price;
        existing.Stock = product.Stock;
        existing.ImageUrl = product.ImageUrl;
        existing.Category = product.Category;
        existing.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        TempData["Success"] = "Producto actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
            return NotFound();

        return View(product);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var product = await context.Products.FindAsync(id);
        if (product is not null)
        {
            context.Products.Remove(product);
            await context.SaveChangesAsync();
            TempData["Success"] = "Producto eliminado correctamente.";
        }
        return RedirectToAction(nameof(Index));
    }
}
