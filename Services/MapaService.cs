using AquaPrior.Models;
using AquaPrior.Repositories;
using AquaPrior.ViewModels;

namespace AquaPrior.Services;

/// <summary>
/// Prepara el mapa de calor: cada reporte aporta un punto en su ubicación exacta con una intensidad
/// que mezcla personas afectadas y horas sin servicio (misma idea que la prioridad SAW, 50 % y 50 %).
/// Donde se juntan varios reportes los puntos se suman y la zona se ve más roja.
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
                        && (r.Estado == EstadoReporte.Verificado || r.Estado == EstadoReporte.Recibido))
            .ToList();

        double Personas(Reporte r) => r.Zonas.Select(z => z.PersonasAfectadas ?? 0).DefaultIfEmpty(0).Max();

        double maxP = Math.Max(1.0, usables.Select(Personas).DefaultIfEmpty(0).Max());
        double maxH = Math.Max(1.0, usables.Select(r => r.HorasSinServicio).DefaultIfEmpty(0).Max());

        var vm = new MapaCalorVM();
        foreach (var r in usables)
        {
            var baseIntensidad = pesoPersonas * (Personas(r) / maxP) + (1 - pesoPersonas) * (r.HorasSinServicio / maxH);
            var factor = r.Estado == EstadoReporte.Verificado ? 1.0 : FactorRecibido;
            vm.Puntos.Add(new PuntoCalorVM
            {
                Lat = r.Latitud!.Value,
                Lng = r.Longitud!.Value,
                Intensidad = Math.Round(Math.Clamp(baseIntensidad * factor, IntensidadMinima, 1.0), 3),
                Direccion = r.Direccion,
                Personas = Personas(r) > 0 ? (int)Personas(r) : null,
                Horas = Math.Round(r.HorasSinServicio, 1),
                Estado = r.Estado.ToString()
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
