using AquaPrior.Models;

namespace AquaPrior.ViewModels;

public class ReporteFilaVM
{
    public Reporte Reporte { get; set; } = null!;
    public bool EsPosibleDuplicado { get; set; }
}

/// <summary>Maestro (zona) con su detalle (reportes de esa zona).</summary>
public class ZonaDetalleVM
{
    public Zona Zona { get; set; } = null!;
    public List<ReporteFilaVM> Reportes { get; set; } = new();
    public int Verificados { get; set; }
}
