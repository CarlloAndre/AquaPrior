using AquaPrior.Models;
using AquaPrior.Repositories;
using AquaPrior.ViewModels;

namespace AquaPrior.Services;

/// <summary>
/// Prepara una intensidad territorial basada en incidentes confirmados por distrito.
/// </summary>
public class MapaService
{
    private readonly IReporteRepository _reportes;
    private readonly IZonaRepository _zonas;

    public MapaService(IReporteRepository reportes, IZonaRepository zonas)
    {
        _reportes = reportes;
        _zonas = zonas;
    }

    public async Task<MapaCalorVM> ObtenerAsync()
    {
        var todos = await _reportes.ListarTodosAsync();
        var usables = todos
            .Where(r => r.Latitud.HasValue && r.Longitud.HasValue
                        && r.FinCorte is null
                        && r.Estado == EstadoReporte.Verificado)
            .ToList();

        var incidentes = Incidentes.Agrupar(usables);
        var zonas = await _zonas.ListarConReportesAsync();
        var conteos = incidentes.GroupBy(i => i.ZonaId).ToDictionary(g => g.Key, g => g.Count());
        var vm = new MapaCalorVM
        {
            Puntos = incidentes.Select(i => new PuntoCalorVM
            {
                Lat = i.Lat,
                Lng = i.Lng,
                Reportes = i.Reportes.Count,
                RadioMetros = Math.Max(110, 50 + i.Reportes.Max(r => Incidentes.DistanciaMetros(i.Lat, i.Lng, r.Latitud!.Value, r.Longitud!.Value))),
                Intensidad = Math.Clamp(i.Reportes.Count / 10.0, 0.2, 1),
                Direccion = i.ZonaNombre,
                Tipo = i.Tipo.ToString()
            }).ToList(),
            Distritos = zonas.Select(z =>
            {
                var activos = z.ReporteZonas
                    .Where(rz => rz.Reporte.FinCorte is null && rz.Reporte.Estado == EstadoReporte.Verificado)
                    .ToList();
                return new MapaVM
                {
                    Nombre = z.Nombre,
                    Total = activos.Count,
                    Verificados = activos.Count,
                    Recibidos = 0,
                    Incidentes = conteos.GetValueOrDefault(z.Id)
                };
            }).ToList()
        };
        var maxIncidentes = Math.Max(1, vm.Distritos.Select(z => z.Incidentes).DefaultIfEmpty().Max());
        foreach (var zona in vm.Distritos)
            zona.Intensidad = zona.Incidentes == 0 ? 0 : (double)zona.Incidentes / maxIncidentes;
        return vm;
    }
}
