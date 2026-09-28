using EcommerceApp.Data;
using EcommerceApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApp.Controllers;

public class HomeController(ApplicationDbContext context) : Controller
{
    /// <summary>
    /// Inicio: hero + sección Nosotros con carrusel de imágenes del stock.
    /// </summary>
    public async Task<IActionResult> Index()
    {
        // Productos del catálogo/stock con imagen (sin logo ni vacíos)
        var products = await context.Products
            .AsNoTracking()
            .Where(p => p.ImageUrl != null && p.ImageUrl != "")
            .OrderBy(p => p.Category)
            .ThenBy(p => p.Name)
            .Select(p => new ProductSlideVm
            {
                Name = p.Name,
                ImageUrl = p.ImageUrl!
            })
            .ToListAsync();

        // Filtrar rutas de logo por si alguien las usó como ImageUrl
        products = products
            .Where(p =>
                !p.ImageUrl.Contains("logo", StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Si la BD está vacía, usar las imágenes de stock en wwwroot/images
        if (products.Count == 0)
        {
            products =
            [
                new() { Name = "TropiValle 1 Litro Retornable", ImageUrl = "/images/nectar-1l-retornable.jpg" },
                new() { Name = "TropiValle 2 Litros Descartable", ImageUrl = "/images/nectar-2l-descartable.jpg" },
                new() { Name = "TropiValle 300ml Retornable", ImageUrl = "/images/nectar-300ml-retornable.jpg" },
                new() { Name = "TropiValle 330ml Descartable", ImageUrl = "/images/nectar-330ml-descartable.jpg" },
                new() { Name = "TropiValle 620ml Retornable", ImageUrl = "/images/nectar-620ml-retornable.jpg" },
                new() { Name = "TropiValle 630ml Descartable", ImageUrl = "/images/nectar-630ml-descartable.jpg" },
                new() { Name = "Agua de Mesa 2 Litros", ImageUrl = "/images/agua-2l.jpg" },
                new() { Name = "Agua de Mesa 330ml", ImageUrl = "/images/agua-330ml.jpg" },
                new() { Name = "Agua de Mesa 630ml", ImageUrl = "/images/agua-630ml.jpg" },
                new() { Name = "Agua de Mesa botellón de 20 litros", ImageUrl = "/images/agua-botellon-20l.jpg" },
            ];
        }

        return View(products);
    }

    public IActionResult Error() => View();
}

public class ProductSlideVm
{
    public string Name { get; set; } = "";
    public string ImageUrl { get; set; } = "";
}
