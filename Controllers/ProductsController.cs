using EcommerceApp.Data;
using EcommerceApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApp.Controllers;

[Authorize]
public class ProductsController(ApplicationDbContext context, IWebHostEnvironment env) : Controller
{
    private static readonly string[] KnownCategories =
    [
        "Nectar TropiValle",
        "Agua de Mesa"
    ];

    private static readonly HashSet<string> AllowedExt = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif"
    };

    private const long MaxImageBytes = 5 * 1024 * 1024; // 5 MB

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
    public async Task<IActionResult> Create(Product product, IFormFile? imageFile)
    {
        // La imagen no es obligatoria: puede ser archivo, URL o ninguna
        if (imageFile is { Length: > 0 })
        {
            var saved = await SaveProductImageAsync(imageFile);
            if (saved is null)
            {
                ModelState.AddModelError("ImageUrl", "Archivo no válido. Usa JPG, PNG, WEBP o GIF (máx. 5 MB).");
            }
            else
            {
                product.ImageUrl = saved;
            }
        }
        else if (!string.IsNullOrWhiteSpace(product.ImageUrl))
        {
            product.ImageUrl = product.ImageUrl.Trim();
        }

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
    public async Task<IActionResult> Edit(int id, Product product, IFormFile? imageFile)
    {
        if (id != product.Id)
            return NotFound();

        var existing = await context.Products.FindAsync(id);
        if (existing is null)
            return NotFound();

        // Prioridad: archivo subido > URL escrita > mantener la actual
        if (imageFile is { Length: > 0 })
        {
            var saved = await SaveProductImageAsync(imageFile);
            if (saved is null)
            {
                ModelState.AddModelError("ImageUrl", "Archivo no válido. Usa JPG, PNG, WEBP o GIF (máx. 5 MB).");
            }
            else
            {
                // Borrar archivo local anterior si era de /images/uploads/
                TryDeleteLocalUpload(existing.ImageUrl);
                product.ImageUrl = saved;
            }
        }
        else if (!string.IsNullOrWhiteSpace(product.ImageUrl))
        {
            product.ImageUrl = product.ImageUrl.Trim();
        }
        else
        {
            // No envió archivo ni URL: conservar la imagen actual
            product.ImageUrl = existing.ImageUrl;
        }

        if (!ModelState.IsValid)
            return View(product);

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
            TryDeleteLocalUpload(product.ImageUrl);
            context.Products.Remove(product);
            await context.SaveChangesAsync();
            TempData["Success"] = "Producto eliminado correctamente.";
        }
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Guarda la imagen en wwwroot/images/uploads y devuelve la URL relativa, o null si falla.</summary>
    private async Task<string?> SaveProductImageAsync(IFormFile file)
    {
        if (file.Length <= 0 || file.Length > MaxImageBytes)
            return null;

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(ext) || !AllowedExt.Contains(ext))
            return null;

        // Content-Type básico
        var contentType = file.ContentType?.ToLowerInvariant() ?? "";
        if (contentType.Length > 0 && !contentType.StartsWith("image/"))
            return null;

        var uploadsDir = Path.Combine(env.WebRootPath, "images", "uploads");
        Directory.CreateDirectory(uploadsDir);

        var fileName = $"{Guid.NewGuid().ToString("N")}{ext.ToLowerInvariant()}";
        var physicalPath = Path.Combine(uploadsDir, fileName);

        await using (var stream = System.IO.File.Create(physicalPath))
        {
            await file.CopyToAsync(stream);
        }

        return $"/images/uploads/{fileName}";
    }

    private void TryDeleteLocalUpload(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return;
        // Solo borrar archivos que subimos nosotros
        if (!imageUrl.StartsWith("/images/uploads/", StringComparison.OrdinalIgnoreCase))
            return;

        var relative = imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var full = Path.Combine(env.WebRootPath, relative);
        try
        {
            if (System.IO.File.Exists(full))
                System.IO.File.Delete(full);
        }
        catch
        {
            // no bloquear si no se puede borrar
        }
    }
}
