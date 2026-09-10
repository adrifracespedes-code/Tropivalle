using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.Models;

public class LoginViewModel
{
    [Required, EmailAddress]
    [Display(Name = "Correo electrónico")]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Recordarme")]
    public bool RememberMe { get; set; }
}
