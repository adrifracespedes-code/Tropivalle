using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcommerceApp.Models;

public class Sale
{
    [Key]
    public int Id { get; set; }

    public int ProductId { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product? Product { get; set; }

    [MaxLength(150)]
    public string ProductName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Category { get; set; }

    public int Quantity { get; set; }

    [Column(TypeName = "numeric(18,2)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "numeric(18,2)")]
    public decimal TotalAmount { get; set; }

    public DateTime SoldAt { get; set; } = DateTime.UtcNow;

    [MaxLength(300)]
    public string? Notes { get; set; }
}
