namespace AquaPrior.Models;

/// <summary>
/// Tabla intermedia (muchos a muchos entre Reporte y Zona) y línea de detalle del maestro-detalle.
/// Lleva un dato propio: las personas afectadas en ese distrito.
/// </summary>
public class ReporteZona
{
    public int ReporteId { get; set; }
    public Reporte Reporte { get; set; } = null!;
    public int ZonaId { get; set; }
    public Zona Zona { get; set; } = null!;
    public int? PersonasAfectadas { get; set; }
}
