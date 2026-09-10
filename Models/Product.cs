using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.Models;

public class Product
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    [Display(Name = "Nombre")]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    [Display(Name = "Descripción")]
    public string Description { get; set; } = string.Empty;

    [Required, Range(0.01, 999999.99)]
    [Display(Name = "Precio")]
    public decimal Price { get; set; }

    [Required, Range(0, int.MaxValue)]
    [Display(Name = "Stock")]
    public int Stock { get; set; }

    [Display(Name = "Imagen")]
    public string? ImageUrl { get; set; }

    [Display(Name = "Categoría")]
    public string? Category { get; set; }

    [Display(Name = "Creado")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Display(Name = "Actualizado")]
    public DateTime? UpdatedAt { get; set; }
}
