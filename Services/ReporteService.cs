using AquaPrior.Models;
using AquaPrior.Repositories;

namespace AquaPrior.Services;

public class ReporteService
{
    private readonly IReporteRepository _reportes;
    private readonly IZonaRepository _zonas;

    public ReporteService(IReporteRepository reportes, IZonaRepository zonas)
    {
        _reportes = reportes;
        _zonas = zonas;
    }

    /// <summary>El administrador puede todo. El ciudadano solo sus reportes y mientras estén en estado Recibido.</summary>
    public static bool PuedeModificar(Reporte r, int usuarioId, bool esAdmin) =>
        esAdmin || (r.UsuarioId == usuarioId && r.Estado == EstadoReporte.Recibido);

    private async Task<string?> ValidarAsync(string direccion, string motivo, DateTime inicio, IReadOnlyList<LineaZona> lineas)
    {
        direccion = (direccion ?? "").Trim();
        if (direccion.Length < 4 || direccion.Length > 80) return "La dirección debe tener entre 4 y 80 caracteres.";
        motivo = (motivo ?? "").Trim();
        if (motivo.Length < 10 || motivo.Length > 500) return "El motivo debe tener entre 10 y 500 caracteres.";
        if (inicio > DateTime.Now) return "La fecha de inicio no puede estar en el futuro.";
        if (lineas is null || lineas.Count == 0) return "Agrega al menos un distrito afectado.";
        if (lineas.Select(l => l.ZonaId).Distinct().Count() != lineas.Count)
            return "Un distrito no puede repetirse en el mismo reporte.";
        foreach (var l in lineas)
        {
            if (l.Personas is <= 0) return "Las personas afectadas deben ser un número mayor a 0.";
            if (!await _zonas.ExisteAsync(l.ZonaId)) return "Uno de los distritos elegidos no existe.";
        }
        return null;
    }

    // C: crear el maestro con sus líneas de detalle
    public async Task<string?> RegistrarAsync(int usuarioId, string direccion, string motivo, DateTime inicio, IReadOnlyList<LineaZona> lineas)
    {
        var error = await ValidarAsync(direccion, motivo, inicio, lineas);
        if (error is not null) return error;

        await _reportes.AgregarAsync(new Reporte
        {
            UsuarioId = usuarioId,
            Direccion = direccion.Trim(),
            Motivo = motivo.Trim(),
            InicioCorte = inicio,
            Estado = EstadoReporte.Recibido,
            Zonas = lineas.Select(l => new ReporteZona { ZonaId = l.ZonaId, PersonasAfectadas = l.Personas }).ToList()
        });
        return null;
    }

    // U: actualizar el maestro y sincronizar sus líneas (quitar, modificar y agregar)
    public async Task<string?> ActualizarAsync(int id, int usuarioId, bool esAdmin, string direccion, string motivo, DateTime inicio, IReadOnlyList<LineaZona> lineas)
    {
        var r = await _reportes.ObtenerConDetalleAsync(id);
        if (r is null) return "El reporte no existe.";
        if (!PuedeModificar(r, usuarioId, esAdmin)) return "No puedes modificar este reporte.";

        var error = await ValidarAsync(direccion, motivo, inicio, lineas);
        if (error is not null) return error;

        var nuevas = lineas.ToDictionary(l => l.ZonaId);

        _reportes.QuitarLineas(r.Zonas.Where(rz => !nuevas.ContainsKey(rz.ZonaId)).ToList());
        foreach (var rz in r.Zonas.Where(rz => nuevas.ContainsKey(rz.ZonaId)))
            rz.PersonasAfectadas = nuevas[rz.ZonaId].Personas;

        var existentes = r.Zonas.Select(rz => rz.ZonaId).ToHashSet();
        foreach (var l in lineas.Where(l => !existentes.Contains(l.ZonaId)))
            r.Zonas.Add(new ReporteZona { ReporteId = r.Id, ZonaId = l.ZonaId, PersonasAfectadas = l.Personas });

        r.Direccion = direccion.Trim();
        r.Motivo = motivo.Trim();
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
