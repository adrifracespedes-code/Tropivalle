using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcommerceApp.Models;

public class CustomerOrder
{
    [Key]
    public int Id { get; set; }

    /// <summary>Código público para el cliente (ej. TV-20260928-A1B2)</summary>
    [Required, MaxLength(40)]
    public string OrderCode { get; set; } = string.Empty;

    public int ProductId { get; set; }

    [Required, MaxLength(150)]
    public string ProductName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Category { get; set; }

    public int Quantity { get; set; }

    [Column(TypeName = "numeric(18,2)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "numeric(18,2)")]
    public decimal TotalAmount { get; set; }

    [Required, MaxLength(120)]
    public string CustomerName { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? CustomerPhone { get; set; }

    [MaxLength(120)]
    public string? CustomerEmail { get; set; }

    /// <summary>Stripe | Transfer | WhatsApp</summary>
    [Required, MaxLength(30)]
    public string PaymentMethod { get; set; } = "Transfer";

    /// <summary>Pending | Paid | Cancelled | Failed</summary>
    [Required, MaxLength(20)]
    public string Status { get; set; } = "Pending";

    [MaxLength(120)]
    public string? StripeSessionId { get; set; }

    [MaxLength(120)]
    public string? StripePaymentIntentId { get; set; }

    [MaxLength(400)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? PaidAt { get; set; }
}

public class CheckoutViewModel
{
    public int ProductId { get; set; }

    [Display(Name = "Producto")]
    public string ProductName { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public decimal UnitPrice { get; set; }

    [Required(ErrorMessage = "Indica la cantidad")]
    [Range(1, 500, ErrorMessage = "Cantidad entre 1 y 500")]
    [Display(Name = "Cantidad")]
    public int Quantity { get; set; } = 1;

    [Required(ErrorMessage = "Tu nombre es obligatorio")]
    [MaxLength(120)]
    [Display(Name = "Nombre completo")]
    public string CustomerName { get; set; } = string.Empty;

    [MaxLength(30)]
    [Display(Name = "Teléfono / WhatsApp")]
    public string? CustomerPhone { get; set; }

    [EmailAddress(ErrorMessage = "Correo no válido")]
    [MaxLength(120)]
    [Display(Name = "Correo (opcional)")]
    public string? CustomerEmail { get; set; }

    [Required(ErrorMessage = "Elige un método de pago")]
    [Display(Name = "Método de pago")]
    public string PaymentMethod { get; set; } = "Transfer";

    [MaxLength(400)]
    [Display(Name = "Notas del pedido")]
    public string? Notes { get; set; }

    public decimal Total => UnitPrice * Quantity;
}
