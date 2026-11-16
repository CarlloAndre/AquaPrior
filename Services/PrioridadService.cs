using AquaPrior.Models;
using AquaPrior.Repositories;

namespace AquaPrior.Services;

public class PrioridadService
{
    // Umbrales legibles en una escala de 0 a 100.
    public const double UmbralAlta = 0.70;
    public const double UmbralMedia = 0.40;

    private readonly IReporteRepository _reportes;
    private readonly IEnumerable<IPrioridadStrategy> _estrategias;

    public PrioridadService(IReporteRepository reportes, IEnumerable<IPrioridadStrategy> estrategias)
    {
        _reportes = reportes;
        _estrategias = estrategias;
    }

    public List<OpcionEstrategia> Opciones() =>
        _estrategias.Select(e => new OpcionEstrategia { Clave = e.Clave, Nombre = e.Nombre }).ToList();

    public async Task<ResultadoPrioridad> CalcularAsync(string clave = "saw", double pesoPersonas = 0.5)
    {
        pesoPersonas = Math.Clamp(pesoPersonas, 0, 1);
        var estrategia = _estrategias.FirstOrDefault(e => e.Clave == clave) ?? _estrategias.First();

        // Se consideran cortes totales abiertos y verificados. Varios reportes cercanos forman un incidente.
        var verificados = (await _reportes.ListarPorEstadoAsync(EstadoReporte.Verificado))
            .Where(r => r.FinCorte is null && r.Tipo == TipoProblema.CorteTotal);
        var incidentes = Incidentes.Agrupar(verificados);
        var zonas = new List<ZonaConsolidada>();
        foreach (var g in incidentes.GroupBy(i => i.ZonaNombre))
        {
            var personas = g.Select(i => i.Personas).Where(p => p > 0).ToList();
            zonas.Add(new ZonaConsolidada(g.Key, g.Sum(i => i.Reportes.Count),
                personas.Count > 0 ? personas.Sum() : (int?)null,
                g.Max(i => i.Horas)));
        }

        var aptas = zonas.Where(z => !estrategia.RequierePersonas || z.Personas.HasValue).ToList();
        var maxP = Math.Max(1, aptas.Select(z => z.Personas ?? 0).DefaultIfEmpty().Max());
        var maxH = Math.Max(1, aptas.Select(z => z.Horas).DefaultIfEmpty().Max());

        var resultado = new ResultadoPrioridad();
        foreach (var z in aptas)
        {
            var puntaje = estrategia.Calcular(z, maxP, maxH, pesoPersonas);
            resultado.Ranking.Add(new ZonaPrioridad
            {
                Zona = z.Zona, Reportes = z.Reportes, Personas = z.Personas, Horas = z.Horas,
                Puntaje = puntaje,
                Nivel = puntaje >= UmbralAlta ? "Alta" : puntaje >= UmbralMedia ? "Media" : "Baja"
            });
        }
        resultado.Ranking = resultado.Ranking.OrderByDescending(r => r.Puntaje).ToList();
        // Sin datos suficientes no se asigna prioridad (CUS-06, flujo alternativo).
        resultado.SinDatos = zonas.Where(z => !aptas.Contains(z)).Select(z => z.Zona).ToList();
        return resultado;
    }
}
