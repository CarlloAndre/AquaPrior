using System.Globalization;
using AquaPrior.Models;
using AquaPrior.Repositories;

namespace AquaPrior.Services;

public class ReporteService
{
    private readonly IReporteRepository _reportes;
    private readonly IZonaRepository _zonas;
    private readonly DistritoLocator _locator;

    public ReporteService(IReporteRepository reportes, IZonaRepository zonas, DistritoLocator locator)
    {
        _reportes = reportes;
        _zonas = zonas;
        _locator = locator;
    }

    /// <summary>El administrador puede todo. El ciudadano solo sus reportes y mientras estén en estado Recibido.</summary>
    public static bool PuedeModificar(Reporte r, int usuarioId, bool esAdmin) =>
        esAdmin || (r.UsuarioId == usuarioId && r.Estado == EstadoReporte.Recibido);

    private static string? Validar(string direccion, double latitud, double longitud, DateTime inicio, int? personas)
    {
        direccion = (direccion ?? "").Trim();
        if (direccion.Length < 4 || direccion.Length > 80) return "La dirección debe tener entre 4 y 80 caracteres.";
        if (!double.IsFinite(latitud) || !double.IsFinite(longitud) || latitud is < -90 or > 90 || longitud is < -180 or > 180)
            return "Marca una ubicación válida en el mapa.";
        if (inicio > DateTime.Now) return "La fecha de inicio no puede estar en el futuro.";
        if (personas is null or <= 0) return "Indica cuántas personas podrían estar afectadas (mayor a 0).";
        return null;
    }

    /// <summary>
    /// El distrito ya no lo elige el usuario: se deduce de las coordenadas exactas del reporte
    /// (punto dentro de polígono sobre el KML). Devuelve el Id de la zona o un error.
    /// </summary>
    private async Task<(int? ZonaId, string? Error)> ResolverZonaAsync(double lat, double lng)
    {
        var normalizado = _locator.NombreNormalizadoEn(lat, lng);
        if (normalizado is null)
            return (null, "La ubicación marcada está fuera de los distritos de Trujillo. Marca un punto dentro del mapa de distritos.");

        var zonas = await _zonas.ListarAsync();
        var zona = zonas.FirstOrDefault(z => DistritoLocator.Normalizar(z.Nombre) == normalizado);
        if (zona is null)
        {
            // El KML trae un distrito que aún no existe en la base (p. ej. Alto Trujillo): se crea.
            var nombre = CultureInfo.GetCultureInfo("es-PE").TextInfo.ToTitleCase(normalizado);
            zona = new Zona { Nombre = nombre };
            await _zonas.AgregarAsync(zona);
        }
        return (zona.Id, null);
    }

    // C: crear el reporte; el sistema genera su línea de distrito a partir de la ubicación
    public async Task<string?> RegistrarAsync(int usuarioId, string direccion, double latitud, double longitud, DateTime inicio, int? personas)
    {
        var error = Validar(direccion, latitud, longitud, inicio, personas);
        if (error is not null) return error;
        var (zonaId, errZona) = await ResolverZonaAsync(latitud, longitud);
        if (errZona is not null) return errZona;

        await _reportes.AgregarAsync(new Reporte
        {
            UsuarioId = usuarioId,
            Direccion = direccion.Trim(),
            Latitud = latitud,
            Longitud = longitud,
            InicioCorte = inicio,
            Estado = EstadoReporte.Recibido,
            Zonas = new List<ReporteZona> { new() { ZonaId = zonaId!.Value, PersonasAfectadas = personas } }
        });
        return null;
    }

    // U: actualizar; si el punto cambió de distrito, la línea de detalle se recalcula
    public async Task<string?> ActualizarAsync(int id, int usuarioId, bool esAdmin, string direccion, double latitud, double longitud, DateTime inicio, int? personas)
    {
        var r = await _reportes.ObtenerConDetalleAsync(id);
        if (r is null) return "El reporte no existe.";
        if (!PuedeModificar(r, usuarioId, esAdmin)) return "No puedes modificar este reporte.";

        var error = Validar(direccion, latitud, longitud, inicio, personas);
        if (error is not null) return error;
        var (zonaId, errZona) = await ResolverZonaAsync(latitud, longitud);
        if (errZona is not null) return errZona;

        _reportes.QuitarLineas(r.Zonas.Where(rz => rz.ZonaId != zonaId).ToList());
        var linea = r.Zonas.FirstOrDefault(rz => rz.ZonaId == zonaId);
        if (linea is not null) linea.PersonasAfectadas = personas;
        else r.Zonas.Add(new ReporteZona { ReporteId = r.Id, ZonaId = zonaId!.Value, PersonasAfectadas = personas });

        r.Direccion = direccion.Trim();
        r.Latitud = latitud;
        r.Longitud = longitud;
        r.InicioCorte = inicio;
        await _reportes.GuardarAsync();
        return null;
    }

    // D: eliminar (las líneas se borran en cascada)
    public async Task<string?> EliminarAsync(int id, int usuarioId, bool esAdmin)
    {
        var r = await _reportes.ObtenerAsync(id);
        if (r is null) return "El reporte no existe.";
        if (!PuedeModificar(r, usuarioId, esAdmin)) return "No puedes eliminar este reporte.";
        await _reportes.EliminarAsync(r);
        return null;
    }

    public async Task<bool> CambiarEstadoAsync(int id, EstadoReporte estado)
    {
        var r = await _reportes.ObtenerAsync(id);
        if (r is null) return false;
        r.Estado = estado;
        await _reportes.GuardarAsync();
        return true;
    }

    private static string Direccion(Reporte r) =>
        string.Join(' ', r.Direccion.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));

    /// <summary>Ids de reportes con la misma dirección y al menos un distrito en común con un reporte anterior no rechazado.</summary>
    public HashSet<int> Duplicados(IReadOnlyCollection<Reporte> lista) =>
        lista.Where(r => lista.Any(o => o.Id < r.Id && o.Estado != EstadoReporte.Rechazado
                    && Direccion(o) == Direccion(r)
                    && o.Zonas.Select(z => z.ZonaId).Intersect(r.Zonas.Select(z => z.ZonaId)).Any()))
             .Select(r => r.Id).ToHashSet();

    public async Task<Estadisticas> EstadisticasAsync()
    {
        var todos = await _reportes.ListarTodosAsync();
        var verificados = todos.Where(r => r.Estado == EstadoReporte.Verificado).ToList();
        return new Estadisticas
        {
            Total = todos.Count,
            Verificados = verificados.Count,
            PorcentajeVerificados = todos.Count == 0 ? 0 : (int)Math.Round(100.0 * verificados.Count / todos.Count),
            PromedioHoras = verificados.Count == 0 ? 0 : verificados.Average(r => r.HorasSinServicio),
            PorEstado = Enum.GetValues<EstadoReporte>()
                .Select(e => new Conteo { Etiqueta = e.ToString(), Cantidad = todos.Count(r => r.Estado == e) }).ToList(),
            PorZona = todos.SelectMany(r => r.Zonas).GroupBy(rz => rz.Zona.Nombre)
                .Select(g => new Conteo { Etiqueta = g.Key, Cantidad = g.Count() })
                .OrderByDescending(c => c.Cantidad).ToList()
        };
    }
}
