using System.ComponentModel.DataAnnotations;

namespace AquaPrior.ViewModels;

public class LoginVM
{
    [Required(ErrorMessage = "Escribe tu correo."), EmailAddress(ErrorMessage = "El correo no es válido.")]
    public string Correo { get; set; } = "";

    [Required(ErrorMessage = "Escribe tu contraseña."), DataType(DataType.Password)]
    public string Clave { get; set; } = "";
}

public class RegistroVM
{
    [Required(ErrorMessage = "Escribe tu nombre."), StringLength(80, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 80 caracteres.")]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "Escribe tu correo."), EmailAddress(ErrorMessage = "El correo no es válido."), StringLength(120)]
    public string Correo { get; set; } = "";

    [Required(ErrorMessage = "Crea una contraseña."), StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres."), DataType(DataType.Password)]
    public string Clave { get; set; } = "";
}
