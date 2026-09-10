using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.Models;

public class RegisterViewModel
{
    [Required, EmailAddress]
    [Display(Name = "Correo electrónico")]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Las contraseñas no coinciden.")]
    [Display(Name = "Confirmar contraseña")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Nombre completo")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Dirección")]
    public string? Address { get; set; }
}
