using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AquaPrior.Models;

public class Reporte
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public int ZonaId { get; set; }
    public Zona Zona { get; set; } = null!;
    [Required, StringLength(80)] public string Direccion { get; set; } = "";
    public DateTime InicioCorte { get; set; }
    public int? PersonasAfectadas { get; set; }
    public EstadoReporte Estado { get; set; } = EstadoReporte.Recibido;
    public DateTime FechaRegistro { get; set; } = DateTime.Now;

    [NotMapped]
    public double HorasSinServicio => Math.Max(0, (DateTime.Now - InicioCorte).TotalHours);
}
