using System.Globalization;
using System.Text;
using EcommerceApp.Data;
using EcommerceApp.Models;
using EcommerceApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApp.Controllers;

public class CheckoutController(
    ApplicationDbContext db,
    StripeCheckoutService stripe,
    IConfiguration config) : Controller
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Index(int productId, int qty = 1)
    {
        var product = await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == productId);
        if (product is null) return NotFound();

        qty = Math.Clamp(qty, 1, 500);
        var vm = new CheckoutViewModel
        {
            ProductId = product.Id,
            ProductName = product.Name,
            ImageUrl = product.ImageUrl,
            UnitPrice = product.Price,
            Quantity = qty,
            PaymentMethod = "Registro"
        };
        ViewBag.StripeEnabled = stripe.IsConfigured;
        ViewBag.Payment = GetPaymentOptions();
        return View(vm);
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(CheckoutViewModel model)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == model.ProductId);
        if (product is null) return NotFound();

        model.ProductName = product.Name;
        model.ImageUrl = product.ImageUrl;
        model.UnitPrice = product.Price;
        ViewBag.StripeEnabled = stripe.IsConfigured;
        ViewBag.Payment = GetPaymentOptions();

        if (!ModelState.IsValid)
            return View(model);

        var method = (model.PaymentMethod ?? "Registro").Trim();
        if (method is not ("Registro" or "Stripe" or "Transfer" or "WhatsApp"))
            method = "Registro";

        if (method == "Stripe" && !stripe.IsConfigured)
        {
            ModelState.AddModelError(nameof(model.PaymentMethod),
                "El pago con tarjeta no está activo. Elige registrar pedido, transferencia o WhatsApp.");
            return View(model);
        }

        var order = new CustomerOrder
        {
            OrderCode = GenerateOrderCode(),
            ProductId = product.Id,
            ProductName = product.Name,
            Category = product.Category,
            Quantity = model.Quantity,
            UnitPrice = product.Price,
            TotalAmount = product.Price * model.Quantity,
            CustomerName = model.CustomerName.Trim(),
            CustomerPhone = model.CustomerPhone?.Trim(),
            CustomerEmail = model.CustomerEmail?.Trim(),
            PaymentMethod = method,
            Status = "Pending",
            Notes = model.Notes?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        db.CustomerOrders.Add(order);
        await db.SaveChangesAsync();

        // Registro local: solo anota en la página (pedido + venta), sin pasarela externa
        if (method == "Registro")
        {
            order.Notes = string.IsNullOrWhiteSpace(order.Notes)
                ? "Pedido registrado en la web (sin cobro online)"
                : order.Notes + " · Registro web";
            await MarkPaidAsync(order, null);
            return RedirectToAction(nameof(Success), new { code = order.OrderCode });
        }

        if (method == "Stripe")
        {
            // Stripe usa centavos (o unidad menor). BOB no tiene centavos en la práctica
            // pero Stripe espera amount * 100 para monedas de 2 decimales.
            var amountCents = (long)Math.Round(order.TotalAmount * 100m, MidpointRounding.AwayFromZero);
            var currency = (config["Stripe:Currency"]
                            ?? Environment.GetEnvironmentVariable("STRIPE_CURRENCY")
                            ?? "bob").ToLowerInvariant();

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var successUrl = $"{baseUrl}/Checkout/Success?code={Uri.EscapeDataString(order.OrderCode)}&session_id={{CHECKOUT_SESSION_ID}}";
            var cancelUrl = $"{baseUrl}/Checkout/Cancel?code={Uri.EscapeDataString(order.OrderCode)}";

            var (ok, url, sessionId, error) = await stripe.CreateCheckoutSessionAsync(
                order.OrderCode,
                $"{order.ProductName} x{order.Quantity}",
                amountCents,
                currency,
                successUrl,
                cancelUrl,
                order.CustomerEmail ?? "");

            if (!ok || string.IsNullOrEmpty(url))
            {
                order.Status = "Failed";
                order.Notes = (order.Notes + " | Stripe: " + error).Trim(' ', '|');
                await db.SaveChangesAsync();
                TempData["Error"] = error ?? "No se pudo iniciar el pago con tarjeta.";
                return RedirectToAction(nameof(Index), new { productId = product.Id, qty = model.Quantity });
            }

            order.StripeSessionId = sessionId;
            await db.SaveChangesAsync();
            return Redirect(url!);
        }

        if (method == "WhatsApp")
        {
            var pay = GetPaymentOptions();
            var msg = BuildWhatsAppMessage(order);
            var phone = new string((pay.WhatsAppNumber ?? "59175950776").Where(char.IsDigit).ToArray());
            var wa = $"https://wa.me/{phone}?text={Uri.EscapeDataString(msg)}";
            return Redirect(wa);
        }

        // Transferencia
        return RedirectToAction(nameof(Transfer), new { code = order.OrderCode });
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Transfer(string code)
    {
        var order = await db.CustomerOrders.AsNoTracking()
            .FirstOrDefaultAsync(o => o.OrderCode == code);
        if (order is null) return NotFound();
        ViewBag.Payment = GetPaymentOptions();
        return View(order);
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Success(string code, string? session_id)
    {
        var order = await db.CustomerOrders
            .FirstOrDefaultAsync(o => o.OrderCode == code);
        if (order is null) return NotFound();

        // Si volvió de Stripe, marcar pagado (confirmación simple; webhook refuerza)
        if (order.Status == "Pending" &&
            (order.PaymentMethod == "Stripe" || !string.IsNullOrEmpty(session_id)))
        {
            await MarkPaidAsync(order, session_id);
        }

        return View(order);
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Cancel(string code)
    {
        var order = await db.CustomerOrders.FirstOrDefaultAsync(o => o.OrderCode == code);
        if (order is null) return NotFound();
        if (order.Status == "Pending")
        {
            order.Status = "Cancelled";
            await db.SaveChangesAsync();
        }
        return View(order);
    }

    /// <summary>Admin: marcar transferencia como pagada</summary>
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkPaid(string code)
    {
        var order = await db.CustomerOrders.FirstOrDefaultAsync(o => o.OrderCode == code);
        if (order is null) return NotFound();
        await MarkPaidAsync(order, null);
        TempData["Success"] = $"Pedido {code} marcado como pagado.";
        return RedirectToAction(nameof(Orders));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Orders()
    {
        var list = await db.CustomerOrders.AsNoTracking()
            .OrderByDescending(o => o.CreatedAt)
            .Take(200)
            .ToListAsync();
        return View(list);
    }

    private async Task MarkPaidAsync(CustomerOrder order, string? sessionId)
    {
        if (order.Status == "Paid") return;

        order.Status = "Paid";
        order.PaidAt = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(sessionId))
            order.StripeSessionId ??= sessionId;

        // Registrar en ventas (gráfica / reporte admin)
        db.Sales.Add(new Sale
        {
            ProductId = order.ProductId,
            ProductName = order.ProductName,
            Category = order.Category,
            Quantity = order.Quantity,
            UnitPrice = order.UnitPrice,
            TotalAmount = order.TotalAmount,
            SoldAt = DateTime.UtcNow,
            Notes = $"Pedido web {order.OrderCode} · {order.PaymentMethod} · {order.CustomerName}"
        });

        await db.SaveChangesAsync();
    }

    private static string GenerateOrderCode()
    {
        var d = DateTime.UtcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var rnd = Convert.ToHexString(Guid.NewGuid().ToByteArray())[..4];
        return $"TV-{d}-{rnd}";
    }

    private PaymentOptions GetPaymentOptions()
    {
        return new PaymentOptions
        {
            BankName = config["Payment:BankName"] ?? Environment.GetEnvironmentVariable("PAYMENT_BANK_NAME") ?? "Banco (configurar en appsettings)",
            AccountHolder = config["Payment:AccountHolder"] ?? "Industrias Alimenticias TropiValle",
            AccountNumber = config["Payment:AccountNumber"] ?? Environment.GetEnvironmentVariable("PAYMENT_ACCOUNT_NUMBER") ?? "",
            AccountType = config["Payment:AccountType"] ?? "Caja de ahorro",
            QrImageUrl = config["Payment:QrImageUrl"] ?? "",
            WhatsAppNumber = config["Payment:WhatsAppNumber"]
                             ?? Environment.GetEnvironmentVariable("WHATSAPP_NUMBER")
                             ?? "59175950776"
        };
    }

    private static string BuildWhatsAppMessage(CustomerOrder o)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Hola TropiValle, quiero realizar un pedido:");
        sb.AppendLine($"• Código: {o.OrderCode}");
        sb.AppendLine($"• Producto: {o.ProductName}");
        sb.AppendLine($"• Cantidad: {o.Quantity}");
        sb.AppendLine($"• Total: {o.TotalAmount:N2} BOB");
        sb.AppendLine($"• Nombre: {o.CustomerName}");
        if (!string.IsNullOrWhiteSpace(o.CustomerPhone))
            sb.AppendLine($"• Tel: {o.CustomerPhone}");
        if (!string.IsNullOrWhiteSpace(o.Notes))
            sb.AppendLine($"• Notas: {o.Notes}");
        sb.AppendLine("¿Me confirman forma de pago y entrega?");
        return sb.ToString();
    }
}
