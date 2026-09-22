using Microsoft.AspNetCore.Mvc;

namespace EcommerceApp.Controllers;

public class HomeController : Controller
{
    // Página de inicio (hero). El catálogo está en Products.
    public IActionResult Index() => View();

    public IActionResult Error() => View();
}
