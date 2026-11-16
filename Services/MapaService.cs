using AquaPrior.Models;
using AquaPrior.Repositories;
using AquaPrior.ViewModels;

namespace AquaPrior.Services;

/// <summary>
/// Prepara el mapa de calor: cada incidente consolidado aporta un punto aproximado con una intensidad
/// que mezcla personas afectadas y horas sin servicio (misma idea que la prioridad SAW, 50 % y 50 %).
/// Las incidencias abiertas se consolidan antes de mostrarse para no duplicar la intensidad.
/// </summary>
public class MapaService
{
    // Un reporte aún sin verificar pesa menos que uno verificado, pero no desaparece del mapa.
    private const double FactorRecibido = 0.6;
    private const double IntensidadMinima = 0.15;

    private readonly IReporteRepository _reportes;
    private readonly IZonaRepository _zonas;

    public MapaService(IReporteRepository reportes, IZonaRepository zonas)
    {
        _reportes = reportes;
        _zonas = zonas;
    }

    public async Task<MapaCalorVM> ObtenerAsync(double pesoPersonas = 0.5)
    {
        var todos = await _reportes.ListarTodosAsync();
        var usables = todos
            .Where(r => r.Latitud.HasValue && r.Longitud.HasValue
                        && r.FinCorte is null
                        && (r.Estado == EstadoReporte.Verificado || r.Estado == EstadoReporte.Recibido))
            .ToList();

        // El mapa muestra incidentes consolidados, no un punto por cada vecino.
        // Las coordenadas se aproximan a ~100 m para no publicar viviendas exactas.
        var incidentes = Incidentes.Agrupar(usables);
        var vm = new MapaCalorVM();
        foreach (var i in incidentes)
        {
            var baseIntensidad = pesoPersonas *
                Math.Clamp((double)i.Personas / PrioridadService.ReferenciaPersonas, 0, 1)
                + (1 - pesoPersonas) * Math.Clamp(i.Horas / PrioridadService.ReferenciaHoras, 0, 1);
            var factor = i.Estado == EstadoReporte.Verificado ? 1.0 : FactorRecibido;
            vm.Puntos.Add(new PuntoCalorVM
            {
                Lat = Math.Round(i.Lat, 3),
                Lng = Math.Round(i.Lng, 3),
                Intensidad = Math.Round(Math.Clamp(baseIntensidad * factor, IntensidadMinima, 1.0), 3),
                Direccion = i.ZonaNombre,
                Tipo = i.Tipo.ToString(),
                Personas = i.Personas > 0 ? i.Personas : null,
                Horas = Math.Round(i.Horas, 1),
                Estado = i.Situacion
            });
        }

        var zonas = await _zonas.ListarConReportesAsync();
        vm.Distritos = zonas.Select(z => new MapaVM
        {
            Nombre = z.Nombre,
            Total = z.ReporteZonas.Count,
            Verificados = z.ReporteZonas.Count(rz => rz.Reporte.Estado == EstadoReporte.Verificado),
            Recibidos = z.ReporteZonas.Count(rz => rz.Reporte.Estado == EstadoReporte.Recibido)
        }).ToList();
        return vm;
    }
}
