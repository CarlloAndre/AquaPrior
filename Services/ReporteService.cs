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

    private async Task<string?> ValidarAsync(int zonaId, string direccion, DateTime inicio, int? personas)
    {
        direccion = (direccion ?? "").Trim();
        if (direccion.Length < 4 || direccion.Length > 80) return "La dirección debe tener entre 4 y 80 caracteres.";
        if (inicio > DateTime.Now) return "La fecha de inicio no puede estar en el futuro.";
        if (personas is <= 0) return "Las personas afectadas deben ser un número mayor a 0.";
        if (!await _zonas.ExisteAsync(zonaId)) return "El distrito elegido no existe.";
        return null;
    }

    // C: crear
    public async Task<string?> RegistrarAsync(int usuarioId, int zonaId, string direccion, DateTime inicio, int? personas)
    {
        var error = await ValidarAsync(zonaId, direccion, inicio, personas);
        if (error is not null) return error;

        await _reportes.AgregarAsync(new Reporte
        {
            UsuarioId = usuarioId, ZonaId = zonaId, Direccion = direccion.Trim(),
            InicioCorte = inicio, PersonasAfectadas = personas, Estado = EstadoReporte.Recibido
        });
        return null;
    }

    // U: actualizar
    public async Task<string?> ActualizarAsync(int id, int usuarioId, bool esAdmin, int zonaId, string direccion, DateTime inicio, int? personas)
    {
        var r = await _reportes.ObtenerAsync(id);
        if (r is null) return "El reporte no existe.";
        if (!PuedeModificar(r, usuarioId, esAdmin)) return "No puedes modificar este reporte.";

        var error = await ValidarAsync(zonaId, direccion, inicio, personas);
        if (error is not null) return error;

        r.ZonaId = zonaId;
        r.Direccion = direccion.Trim();
        r.InicioCorte = inicio;
        r.PersonasAfectadas = personas;
        await _reportes.GuardarAsync();
        return null;
    }

    // D: eliminar
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

    private static string Clave(Reporte r) =>
        r.ZonaId + "|" + string.Join(' ', r.Direccion.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));

    /// <summary>Ids de reportes que repiten zona y dirección de otro reporte anterior no rechazado.</summary>
    public HashSet<int> Duplicados(IReadOnlyCollection<Reporte> lista) =>
        lista.Where(r => lista.Any(o => o.Id < r.Id && o.Estado != EstadoReporte.Rechazado && Clave(o) == Clave(r)))
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
            PorZona = todos.GroupBy(r => r.Zona.Nombre)
                .Select(g => new Conteo { Etiqueta = g.Key, Cantidad = g.Count() })
                .OrderByDescending(c => c.Cantidad).ToList()
        };
    }
}
