using System.ComponentModel.DataAnnotations;

namespace AquaPrior.Models;

public class Zona
{
    public int Id { get; set; }
    [Required, StringLength(80)] public string Nombre { get; set; } = "";
    public ICollection<Reporte> Reportes { get; set; } = new List<Reporte>();
}
