using System.ComponentModel.DataAnnotations;

namespace AquaPrior.Models;

public class Usuario
{
    public int Id { get; set; }
    [Required, StringLength(80)] public string Nombre { get; set; } = "";
    [Required, EmailAddress, StringLength(120)] public string Correo { get; set; } = "";
    [Required] public string ClaveHash { get; set; } = "";
    public Rol Rol { get; set; } = Rol.Ciudadano;
    public ICollection<Reporte> Reportes { get; set; } = new List<Reporte>();
}
