using EcommerceApp.Data;
using EcommerceApp.Helpers;
using EcommerceApp.Models;
using EcommerceApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApp.Controllers;

[Authorize]
public class ProductsController(
    ApplicationDbContext context,
    IWebHostEnvironment env,
    GlobalImageStorage globalImages) : Controller
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
        if (imageFile is { Length: > 0 })
        {
            var saved = await SaveProductImageAsync(imageFile);
            if (saved is null)
                ModelState.AddModelError("ImageUrl", "Archivo no válido. Usa JPG, PNG, WEBP o GIF (máx. 5 MB).");
            else
                product.ImageUrl = saved;
        }
        else if (!string.IsNullOrWhiteSpace(product.ImageUrl))
        {
            product.ImageUrl = NormalizeStoredUrl(product.ImageUrl);
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

        if (imageFile is { Length: > 0 })
        {
            var saved = await SaveProductImageAsync(imageFile);
            if (saved is null)
            {
                ModelState.AddModelError("ImageUrl", "Archivo no válido. Usa JPG, PNG, WEBP o GIF (máx. 5 MB).");
            }
            else
            {
                TryDeleteLocalUpload(existing.ImageUrl);
                product.ImageUrl = saved;
            }
        }
        else if (!string.IsNullOrWhiteSpace(product.ImageUrl))
        {
            product.ImageUrl = NormalizeStoredUrl(product.ImageUrl);
        }
        else
        {
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
        var product = await context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
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

    /// <summary>
    /// Guarda imagen de forma GLOBAL:
    /// 1) Supabase Storage (URL https pública) si está configurado
    /// 2) Si no, disco local + URL absoluta del sitio (https://tu-dominio/...)
    /// </summary>
    private async Task<string?> SaveProductImageAsync(IFormFile file)
    {
        if (file.Length <= 0 || file.Length > MaxImageBytes)
            return null;

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(ext) || !AllowedExt.Contains(ext))
            return null;

        var contentType = file.ContentType?.ToLowerInvariant() ?? "";
        if (contentType.Length > 0 && !contentType.StartsWith("image/"))
            return null;

        var fileName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";

        // 1) Almacenamiento global (Supabase)
        var globalUrl = await globalImages.UploadAsync(file, fileName);
        if (!string.IsNullOrWhiteSpace(globalUrl))
            return globalUrl;

        // 2) Fallback: disco del servidor + URL absoluta pública del host actual
        var uploadsDir = Path.Combine(env.WebRootPath, "images", "uploads");
        Directory.CreateDirectory(uploadsDir);
        var physicalPath = Path.Combine(uploadsDir, fileName);

        await using (var stream = System.IO.File.Create(physicalPath))
        {
            await file.CopyToAsync(stream);
        }

        var relative = $"/images/uploads/{fileName}";
        return ToAbsolutePublicUrl(relative);
    }

    /// <summary>Convierte /images/... en https://dominio-actual/images/...</summary>
    private string ToAbsolutePublicUrl(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return relativePath;

        if (relativePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            relativePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return relativePath;

        if (!relativePath.StartsWith('/'))
            relativePath = "/" + relativePath;

        var baseUrl = $"{Request.Scheme}://{Request.Host}".TrimEnd('/');
        // Preferir https en producción detrás de proxy
        if (Request.Headers.TryGetValue("X-Forwarded-Proto", out var proto) &&
            !string.IsNullOrWhiteSpace(proto))
        {
            baseUrl = $"{proto.ToString().Split(',')[0].Trim()}://{Request.Host}".TrimEnd('/');
        }

        return baseUrl + relativePath;
    }

    private static string NormalizeStoredUrl(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return "";

        var u = imageUrl.Trim();

        if (u.StartsWith("http://localhost", StringComparison.OrdinalIgnoreCase) ||
            u.StartsWith("https://localhost", StringComparison.OrdinalIgnoreCase) ||
            u.StartsWith("http://127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
            u.StartsWith("https://127.0.0.1", StringComparison.OrdinalIgnoreCase))
        {
            if (Uri.TryCreate(u, UriKind.Absolute, out var uri))
                u = uri.AbsolutePath;
        }

        if (!u.StartsWith("http", StringComparison.OrdinalIgnoreCase) && !u.StartsWith('/'))
            u = "/" + u;

        return u;
    }

    private void TryDeleteLocalUpload(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return;

        // Solo archivos locales /images/uploads/ o URL que termine en esa ruta
        string? pathPart = null;
        if (imageUrl.StartsWith("/images/uploads/", StringComparison.OrdinalIgnoreCase))
            pathPart = imageUrl;
        else if (Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri) &&
                 uri.AbsolutePath.StartsWith("/images/uploads/", StringComparison.OrdinalIgnoreCase))
            pathPart = uri.AbsolutePath;

        if (pathPart is null)
            return;

        var relative = pathPart.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var full = Path.Combine(env.WebRootPath, relative);
        try
        {
            if (System.IO.File.Exists(full))
                System.IO.File.Delete(full);
        }
        catch
        {
            // no bloquear
        }
    }
}
