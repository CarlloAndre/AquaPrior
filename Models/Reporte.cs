using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AquaPrior.Models;

/// <summary>Maestro: un corte de agua. Sus líneas de detalle son los distritos afectados (ReporteZona).</summary>
public class Reporte
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    [Required, StringLength(80)] public string Direccion { get; set; } = "";
    public DateTime InicioCorte { get; set; }
    public EstadoReporte Estado { get; set; } = EstadoReporte.Recibido;
    public DateTime FechaRegistro { get; set; } = DateTime.Now;
    public ICollection<ReporteZona> Zonas { get; set; } = new List<ReporteZona>();

    [NotMapped]
    public double HorasSinServicio => Math.Max(0, (DateTime.Now - InicioCorte).TotalHours);
}
