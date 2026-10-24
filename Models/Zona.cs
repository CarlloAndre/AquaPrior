using System.ComponentModel.DataAnnotations;

namespace AquaPrior.Models;

public class Zona
{
    public int Id { get; set; }
    [Required, StringLength(80)] public string Nombre { get; set; } = "";
    public ICollection<ReporteZona> ReporteZonas { get; set; } = new List<ReporteZona>();
}
