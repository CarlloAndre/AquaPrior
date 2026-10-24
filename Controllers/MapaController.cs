using AquaPrior.Helpers;
using AquaPrior.Models;
using AquaPrior.Services;
using AquaPrior.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AquaPrior.Controllers;

public class MapaController : Controller
{
    private readonly ZonaService _zonas;
    public MapaController(ZonaService zonas) => _zonas = zonas;

    public async Task<IActionResult> Index()
    {
        var zonas = await _zonas.ListarAsync();
        var datos = zonas.Select(z =>
        {
            var tiene = CoordenadasDistritos.Centros.TryGetValue(z.Nombre, out var c);
            return new MapaVM
            {
                Nombre = z.Nombre,
                // Un reporte puede afectar a varios distritos: se cuenta una vez por cada distrito (línea ReporteZona).
                Total = z.ReporteZonas.Count,
                Verificados = z.ReporteZonas.Count(rz => rz.Reporte.Estado == EstadoReporte.Verificado),
                Recibidos = z.ReporteZonas.Count(rz => rz.Reporte.Estado == EstadoReporte.Recibido),
                Lat = tiene ? c.Lat : null,
                Lng = tiene ? c.Lng : null
            };
        }).ToList();
        return View(datos);
    }
}