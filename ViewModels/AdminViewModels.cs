using AquaPrior.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AquaPrior.ViewModels;

public class ReporteFilaVM
{
    public Reporte Reporte { get; set; } = null!;
    public bool EsPosibleDuplicado { get; set; }
    public int? PersonasEnZona { get; set; }
}

/// <summary>Maestro (zona) con su detalle (reportes de esa zona).</summary>
public class ZonaDetalleVM
{
    public Zona Zona { get; set; } = null!;
    public List<ReporteFilaVM> Reportes { get; set; } = new();
    public int Verificados { get; set; }
}

public class ColaAdminVM
{
    public string Filtro { get; set; } = "pendientes";
    public int? ZonaId { get; set; }
    public ColaAdmin Cola { get; set; } = new();
    public List<SelectListItem> Zonas { get; set; } = new();
}
