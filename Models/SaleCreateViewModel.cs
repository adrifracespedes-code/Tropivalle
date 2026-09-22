using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.Models;

public class SaleCreateViewModel
{
    [Required(ErrorMessage = "Selecciona un producto.")]
    [Display(Name = "Producto")]
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un producto.")]
    public int ProductId { get; set; }

    [Required(ErrorMessage = "Indica la cantidad.")]
    [Range(1, 100000, ErrorMessage = "La cantidad debe ser al menos 1.")]
    [Display(Name = "Cantidad")]
    public int Quantity { get; set; } = 1;

    [Required(ErrorMessage = "Indica la fecha de la venta.")]
    [Display(Name = "Fecha de venta")]
    public DateTime SoldAt { get; set; } = DateTime.Now;

    [MaxLength(300)]
    [Display(Name = "Notas")]
    public string? Notes { get; set; }
}
