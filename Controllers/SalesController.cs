using EcommerceApp.Data;
using EcommerceApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EcommerceApp.Controllers;

[Authorize(Roles = "Admin")]
public class SalesController(ApplicationDbContext context, IWebHostEnvironment env) : Controller
{
    static SalesController()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string period = "mensual")
    {
        period = NormalizePeriod(period);
        ViewBag.Period = period;
        ViewBag.Summary = await BuildSummaryAsync(period);
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> ChartData(string period = "mensual", string groupBy = "categoria")
    {
        period = NormalizePeriod(period);
        var (from, to) = GetRange(period);
        var sales = await context.Sales.AsNoTracking()
            .Where(s => s.SoldAt >= from && s.SoldAt < to)
            .ToListAsync();

        if (string.Equals(groupBy, "producto", StringComparison.OrdinalIgnoreCase))
        {
            var groups = sales
                .GroupBy(s => s.ProductName)
                .Select(g => new { label = g.Key, value = g.Sum(x => x.TotalAmount) })
                .OrderByDescending(x => x.value)
                .Take(12)
                .ToList();
            return Json(new
            {
                period,
                groupBy = "producto",
                labels = groups.Select(g => g.label).ToArray(),
                values = groups.Select(g => Math.Round(g.value, 2)).ToArray(),
                total = Math.Round(sales.Sum(s => s.TotalAmount), 2),
                count = sales.Sum(s => s.Quantity)
            });
        }

        var byCat = sales
            .GroupBy(s => string.IsNullOrWhiteSpace(s.Category) ? "Sin categoría" : s.Category!)
            .Select(g => new { label = g.Key, value = g.Sum(x => x.TotalAmount) })
            .OrderByDescending(x => x.value)
            .ToList();

        return Json(new
        {
            period,
            groupBy = "categoria",
            labels = byCat.Select(g => g.label).ToArray(),
            values = byCat.Select(g => Math.Round(g.value, 2)).ToArray(),
            total = Math.Round(sales.Sum(s => s.TotalAmount), 2),
            count = sales.Sum(s => s.Quantity)
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadProductsAsync();
        return View(new SaleCreateViewModel { Quantity = 1, SoldAt = DateTime.Now });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SaleCreateViewModel model)
    {
        await LoadProductsAsync();
        if (!ModelState.IsValid)
            return View(model);

        var product = await context.Products.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == model.ProductId);
        if (product is null)
        {
            ModelState.AddModelError(nameof(model.ProductId), "Producto no encontrado.");
            return View(model);
        }

        var soldAt = model.SoldAt == default ? DateTime.Now : model.SoldAt;
        if (soldAt.Kind == DateTimeKind.Unspecified)
            soldAt = DateTime.SpecifyKind(soldAt, DateTimeKind.Local);
        soldAt = soldAt.ToUniversalTime();

        var sale = new Sale
        {
            ProductId = product.Id,
            ProductName = product.Name,
            Category = product.Category ?? "Sin categoría",
            Quantity = model.Quantity,
            UnitPrice = product.Price,
            TotalAmount = Math.Round(product.Price * model.Quantity, 2),
            SoldAt = soldAt,
            Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim()
        };

        try
        {
            context.Sales.Add(sale);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty,
                "No se pudo guardar la venta. ¿Existe la tabla Sales? Detalle: " + (ex.InnerException?.Message ?? ex.Message));
            return View(model);
        }

        TempData["Success"] = $"Venta registrada: {sale.ProductName} × {sale.Quantity} = {sale.TotalAmount:N2} BOB";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> List(string period = "mensual")
    {
        period = NormalizePeriod(period);
        var (from, to) = GetRange(period);
        var sales = await context.Sales.AsNoTracking()
            .Where(s => s.SoldAt >= from && s.SoldAt < to)
            .OrderByDescending(s => s.SoldAt)
            .Take(200)
            .ToListAsync();
        ViewBag.Period = period;
        ViewBag.Summary = await BuildSummaryAsync(period);
        return View(sales);
    }

    /// <summary>Vista de reporte (vista previa antes del PDF).</summary>
    [HttpGet]
    public async Task<IActionResult> Report(string period = "mensual")
    {
        period = NormalizePeriod(period);
        var (from, to) = GetRange(period);
        var sales = await context.Sales.AsNoTracking()
            .Where(s => s.SoldAt >= from && s.SoldAt < to)
            .OrderBy(s => s.SoldAt)
            .ToListAsync();

        ViewBag.Period = period;
        ViewBag.From = from;
        ViewBag.To = to;
        ViewBag.Total = sales.Sum(s => s.TotalAmount);
        ViewBag.Units = sales.Sum(s => s.Quantity);
        return View(sales);
    }

    /// <summary>Descarga el reporte de ventas en PDF.</summary>
    [HttpGet]
    public async Task<IActionResult> ExportPdf(string period = "mensual")
    {
        period = NormalizePeriod(period);
        var (from, to) = GetRange(period);
        var sales = await context.Sales.AsNoTracking()
            .Where(s => s.SoldAt >= from && s.SoldAt < to)
            .OrderBy(s => s.SoldAt)
            .ToListAsync();

        var total = sales.Sum(s => s.TotalAmount);
        var units = sales.Sum(s => s.Quantity);
        var periodLabel = period switch
        {
            "semanal" => "Semanal",
            "anual" => "Anual",
            _ => "Mensual"
        };

        var logoPath = Path.Combine(env.WebRootPath, "images", "logo-tropivalle.png");
        var hasLogo = System.IO.File.Exists(logoPath);

        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(36);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    if (hasLogo)
                    {
                        col.Item().AlignCenter().Height(56).Image(logoPath).FitHeight();
                        col.Item().PaddingTop(4);
                    }
                    col.Item().AlignCenter().Text("INDUSTRIAS ALIMENTICIAS TROPIVALLE")
                        .Bold().FontSize(13).FontColor(Colors.Green.Darken3);
                    col.Item().AlignCenter().Text("Reporte de ventas — " + periodLabel)
                        .FontSize(11).FontColor(Colors.Grey.Darken2);
                    col.Item().AlignCenter().Text(
                            $"Periodo: {from.ToLocalTime():dd/MM/yyyy} — {to.ToLocalTime().AddSeconds(-1):dd/MM/yyyy}  ·  Generado: {DateTime.Now:dd/MM/yyyy HH:mm}")
                        .FontSize(8).FontColor(Colors.Grey.Medium);
                    col.Item().PaddingVertical(8).LineHorizontal(1.5f).LineColor(Colors.Green.Medium);
                });

                page.Content().Column(col =>
                {
                    col.Item().PaddingBottom(12).Row(row =>
                    {
                        row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2)
                            .Background(Colors.Grey.Lighten4).Padding(10).Column(c =>
                        {
                            c.Item().Text("Total vendido").FontSize(8).FontColor(Colors.Grey.Darken1);
                            c.Item().Text($"{total:N2} BOB").Bold().FontSize(14).FontColor(Colors.Green.Darken3);
                        });
                        row.ConstantItem(10);
                        row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2)
                            .Background(Colors.Grey.Lighten4).Padding(10).Column(c =>
                        {
                            c.Item().Text("Unidades").FontSize(8).FontColor(Colors.Grey.Darken1);
                            c.Item().Text($"{units}").Bold().FontSize(14).FontColor(Colors.Green.Darken3);
                        });
                        row.ConstantItem(10);
                        row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2)
                            .Background(Colors.Grey.Lighten4).Padding(10).Column(c =>
                        {
                            c.Item().Text("Registros").FontSize(8).FontColor(Colors.Grey.Darken1);
                            c.Item().Text($"{sales.Count}").Bold().FontSize(14).FontColor(Colors.Green.Darken3);
                        });
                    });

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(2.3f);
                            columns.RelativeColumn(1.6f);
                            columns.RelativeColumn(0.7f);
                            columns.RelativeColumn(1f);
                            columns.RelativeColumn(1.1f);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Green.Darken2).Padding(5)
                                .Text("Fecha").FontColor(Colors.White).Bold().FontSize(9);
                            header.Cell().Background(Colors.Green.Darken2).Padding(5)
                                .Text("Producto").FontColor(Colors.White).Bold().FontSize(9);
                            header.Cell().Background(Colors.Green.Darken2).Padding(5)
                                .Text("Categoría").FontColor(Colors.White).Bold().FontSize(9);
                            header.Cell().Background(Colors.Green.Darken2).Padding(5)
                                .AlignRight().Text("Cant.").FontColor(Colors.White).Bold().FontSize(9);
                            header.Cell().Background(Colors.Green.Darken2).Padding(5)
                                .AlignRight().Text("P. unit.").FontColor(Colors.White).Bold().FontSize(9);
                            header.Cell().Background(Colors.Green.Darken2).Padding(5)
                                .AlignRight().Text("Total").FontColor(Colors.White).Bold().FontSize(9);
                        });

                        var alt = false;
                        foreach (var s in sales)
                        {
                            var bg = alt ? Colors.Grey.Lighten4 : Colors.White;
                            alt = !alt;
                            table.Cell().Background(bg).Padding(4)
                                .Text(s.SoldAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm")).FontSize(8);
                            table.Cell().Background(bg).Padding(4)
                                .Text(s.ProductName).FontSize(8);
                            table.Cell().Background(bg).Padding(4)
                                .Text(s.Category ?? "—").FontSize(8);
                            table.Cell().Background(bg).Padding(4)
                                .AlignRight().Text(s.Quantity.ToString()).FontSize(8);
                            table.Cell().Background(bg).Padding(4)
                                .AlignRight().Text(s.UnitPrice.ToString("N2")).FontSize(8);
                            table.Cell().Background(bg).Padding(4)
                                .AlignRight().Text(s.TotalAmount.ToString("N2")).FontSize(8);
                        }

                        if (sales.Count > 0)
                        {
                            table.Cell().ColumnSpan(3).Background(Colors.Green.Lighten4).Padding(5)
                                .Text("TOTAL").Bold().FontSize(9);
                            table.Cell().Background(Colors.Green.Lighten4).Padding(5)
                                .AlignRight().Text(units.ToString()).Bold().FontSize(9);
                            table.Cell().Background(Colors.Green.Lighten4).Padding(5).Text("");
                            table.Cell().Background(Colors.Green.Lighten4).Padding(5)
                                .AlignRight().Text($"{total:N2} BOB").Bold().FontSize(9);
                        }
                    });

                    if (sales.Count == 0)
                    {
                        col.Item().PaddingTop(24).AlignCenter()
                            .Text("No hay ventas registradas en este periodo.")
                            .FontColor(Colors.Grey.Medium);
                    }

                    col.Item().PaddingTop(16).AlignCenter()
                        .Text("Calle Salazar No. 1691 · Zona La Chimba · Cochabamba, Bolivia · +591-7-595-0776")
                        .FontSize(8).FontColor(Colors.Grey.Medium);
                });

                page.Footer().AlignCenter().Text(txt =>
                {
                    txt.Span("TropiValle · Hecho en Bolivia  ·  Página ").FontSize(8).FontColor(Colors.Grey.Medium);
                    txt.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                    txt.Span(" / ").FontSize(8).FontColor(Colors.Grey.Medium);
                    txt.TotalPages().FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });
        }).GeneratePdf();

        var fileName = $"reporte-ventas-tropivalle-{period}-{DateTime.Now:yyyyMMdd-HHmm}.pdf";
        return File(bytes, "application/pdf", fileName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, string period = "mensual")
    {
        period = NormalizePeriod(period);
        var sale = await context.Sales.FindAsync(id);
        if (sale is null)
        {
            TempData["Success"] = null;
            TempData["Error"] = "La venta no existe o ya fue eliminada.";
            return RedirectToAction(nameof(List), new { period });
        }

        context.Sales.Remove(sale);
        await context.SaveChangesAsync();
        TempData["Success"] = $"Venta eliminada: {sale.ProductName} ({sale.TotalAmount:N2} BOB)";
        return RedirectToAction(nameof(List), new { period });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClearPeriod(string period = "mensual")
    {
        period = NormalizePeriod(period);
        var (from, to) = GetRange(period);
        var toDelete = await context.Sales
            .Where(s => s.SoldAt >= from && s.SoldAt < to)
            .ToListAsync();

        if (toDelete.Count == 0)
        {
            TempData["Error"] = "No hay ventas que borrar en este periodo.";
            return RedirectToAction(nameof(List), new { period });
        }

        context.Sales.RemoveRange(toDelete);
        await context.SaveChangesAsync();
        TempData["Success"] = $"Se eliminaron {toDelete.Count} venta(s) del periodo {period}.";
        return RedirectToAction(nameof(List), new { period });
    }

    private async Task LoadProductsAsync()
    {
        var products = await context.Products.AsNoTracking()
            .OrderBy(p => p.Category).ThenBy(p => p.Name)
            .ToListAsync();
        ViewBag.Products = products
            .Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = $"{p.Category} — {p.Name} ({p.Price:N2} BOB)"
            })
            .ToList();
    }

    private async Task<object> BuildSummaryAsync(string period)
    {
        var (from, to) = GetRange(period);
        var q = context.Sales.AsNoTracking().Where(s => s.SoldAt >= from && s.SoldAt < to);
        var total = await q.SumAsync(s => (decimal?)s.TotalAmount) ?? 0m;
        var units = await q.SumAsync(s => (int?)s.Quantity) ?? 0;
        var rows = await q.CountAsync();
        return new { Total = total, Units = units, Rows = rows, From = from, To = to };
    }

    private static string NormalizePeriod(string? period)
    {
        period = (period ?? "mensual").Trim().ToLowerInvariant();
        return period switch
        {
            "semanal" or "semana" or "week" or "weekly" => "semanal",
            "anual" or "año" or "year" or "yearly" => "anual",
            _ => "mensual"
        };
    }

    private static (DateTime from, DateTime to) GetRange(string period)
    {
        var now = DateTime.UtcNow;
        return period switch
        {
            "semanal" => (now.Date.AddDays(-6), now.Date.AddDays(1)),
            "anual" => (new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                        new DateTime(now.Year + 1, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
            _ => (new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc),
                  new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1))
        };
    }
}
