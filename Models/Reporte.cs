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
    /// <summary>Qué ocurre (corte total, baja presión, agua turbia...). Lo escribe quien reporta.</summary>
    [Required, StringLength(500)] public string Motivo { get; set; } = "";
    public double? Latitud { get; set; }
    public double? Longitud { get; set; }
    public DateTime InicioCorte { get; set; }
    public EstadoReporte Estado { get; set; } = EstadoReporte.Recibido;
    /// <summary>Cuándo volvió el servicio. Mientras sea null el corte sigue abierto y las horas siguen corriendo.</summary>
    public DateTime? FinCorte { get; set; }
    /// <summary>Por qué se rechazó o verificó (lo escribe el administrador o las reglas automáticas). Lo ve el ciudadano.</summary>
    [StringLength(160)] public string? MotivoEstado { get; set; }
    public DateTime FechaRegistro { get; set; } = DateTime.Now;
    public ICollection<ReporteZona> Zonas { get; set; } = new List<ReporteZona>();

    [NotMapped]
    public double HorasSinServicio => Math.Max(0, ((FinCorte ?? DateTime.Now) - InicioCorte).TotalHours);

    [NotMapped]
    public bool EstaAbierto => FinCorte is null;

    [NotMapped]
    public int Personas => Zonas.Select(z => z.PersonasAfectadas ?? 0).DefaultIfEmpty(0).Max();
}
