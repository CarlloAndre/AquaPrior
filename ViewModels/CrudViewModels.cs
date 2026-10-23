using System.ComponentModel.DataAnnotations;
using AquaPrior.Models;

namespace AquaPrior.ViewModels;

public class ZonaVM
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Escribe el nombre del distrito."),
     StringLength(80, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 80 caracteres.")]
    public string Nombre { get; set; } = "";
}

public class UsuarioVM
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Escribe el nombre."),
     StringLength(80, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 80 caracteres.")]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "Escribe el correo."), EmailAddress(ErrorMessage = "El correo no es válido."), StringLength(120)]
    public string Correo { get; set; } = "";

    public Rol Rol { get; set; } = Rol.Ciudadano;

    [DataType(DataType.Password),
     StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    public string? Clave { get; set; }
}
