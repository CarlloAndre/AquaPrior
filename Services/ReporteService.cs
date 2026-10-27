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

    private static string? Validar(string direccion, string motivo, double latitud, double longitud, DateTime inicio, int? personas)
    {
        direccion = (direccion ?? "").Trim();
        if (direccion.Length < 4 || direccion.Length > 80) return "La dirección debe tener entre 4 y 80 caracteres.";
        motivo = (motivo ?? "").Trim();
        if (motivo.Length < 10 || motivo.Length > 500) return "El motivo debe tener entre 10 y 500 caracteres.";
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
    public async Task<string?> RegistrarAsync(int usuarioId, string direccion, string motivo, double latitud, double longitud, DateTime inicio, int? personas)
    {
        var error = Validar(direccion, motivo, latitud, longitud, inicio, personas);
        if (error is not null) return error;
        var (zonaId, errZona) = await ResolverZonaAsync(latitud, longitud);
        if (errZona is not null) return errZona;

        var reporte = new Reporte
        {
            UsuarioId = usuarioId,
            Direccion = direccion.Trim(),
            Motivo = motivo.Trim(),
            Latitud = latitud,
            Longitud = longitud,
            InicioCorte = inicio,
            Estado = EstadoReporte.Recibido,
            Zonas = new List<ReporteZona> { new() { ZonaId = zonaId!.Value, PersonasAfectadas = personas } }
        };
        await _reportes.AgregarAsync(reporte);
        await AplicarReglasAutomaticasAsync(reporte.Id);
        return null;
    }

    // U: actualizar; si el punto cambió de distrito, la línea de detalle se recalcula
    public async Task<string?> ActualizarAsync(int id, int usuarioId, bool esAdmin, string direccion, string motivo, double latitud, double longitud, DateTime inicio, int? personas)
    {
        var r = await _reportes.ObtenerConDetalleAsync(id);
        if (r is null) return "El reporte no existe.";
        if (!PuedeModificar(r, usuarioId, esAdmin)) return "No puedes modificar este reporte.";

        var error = Validar(direccion, motivo, latitud, longitud, inicio, personas);
        if (error is not null) return error;
        var (zonaId, errZona) = await ResolverZonaAsync(latitud, longitud);
        if (errZona is not null) return errZona;

        _reportes.QuitarLineas(r.Zonas.Where(rz => rz.ZonaId != zonaId).ToList());
        var linea = r.Zonas.FirstOrDefault(rz => rz.ZonaId == zonaId);
        if (linea is not null) linea.PersonasAfectadas = personas;
        else r.Zonas.Add(new ReporteZona { ReporteId = r.Id, ZonaId = zonaId!.Value, PersonasAfectadas = personas });

        r.Direccion = direccion.Trim();
        r.Motivo = motivo.Trim();
        r.Latitud = latitud;
        r.Longitud = longitud;
        r.InicioCorte = inicio;
        await _reportes.GuardarAsync();
        if (r.Estado == EstadoReporte.Recibido) await AplicarReglasAutomaticasAsync(r.Id);
        return null;
    }

    // ---- Reglas automáticas (el administrador solo interviene en lo dudoso) ----

    /// <summary>Con tantos vecinos distintos reportando el mismo corte, el incidente se verifica solo.</summary>
    public const int VecinosParaVerificarSolo = 3;
    /// <summary>Si al menos esta fracción de los reportes de un incidente dice que volvió el agua, se cierra todo el incidente.</summary>
    public const double FraccionParaCerrarSolo = 0.5;

    /// <summary>
    /// 1) El mismo usuario reportando otra vez el mismo punto: se marca Duplicado.
    /// 2) Reporte de otro vecino cerca de uno existente: no se descarta, cuenta como confirmación.
    /// 3) Incidente con varios vecinos distintos, o que ya tenía un reporte verificado: pasa a Verificado.
    /// </summary>
    private async Task AplicarReglasAutomaticasAsync(int reporteId)
    {
        var todos = await _reportes.ListarTodosAsync();
        var r = todos.FirstOrDefault(x => x.Id == reporteId);
        if (r is null || r.Estado != EstadoReporte.Recibido) return;

        var propio = todos.FirstOrDefault(o => o.Id != r.Id && o.UsuarioId == r.UsuarioId && Incidentes.Unibles(o, r));
        if (propio is not null && propio.Id < r.Id)
        {
            r.Estado = EstadoReporte.Duplicado;
            r.MotivoEstado = "Duplicado automático: ya enviaste un reporte de este mismo punto.";
            await _reportes.GuardarAsync();
            return;
        }

        var inc = Incidentes.De(todos, r);
        if (inc is null) return;
        var yaVerificado = inc.Reportes.Any(x => x.Estado == EstadoReporte.Verificado);
        if (!yaVerificado && inc.Usuarios < VecinosParaVerificarSolo) return;

        var motivo = yaVerificado
            ? "Verificado automáticamente: se suma a un incidente ya verificado."
            : $"Verificado automáticamente: {inc.Usuarios} vecinos reportaron el mismo corte.";
        foreach (var x in inc.Reportes.Where(x => x.Estado == EstadoReporte.Recibido))
        {
            x.Estado = EstadoReporte.Verificado;
            x.MotivoEstado = motivo;
        }
        await _reportes.GuardarAsync();
    }

    /// <summary>Un vecino indica que volvió el agua (su reporte); el administrador puede cerrar el incidente completo.</summary>
    public async Task<string?> ResolverAsync(int id, int usuarioId, bool esAdmin, bool incidente)
    {
        var todos = await _reportes.ListarTodosAsync();
        var r = todos.FirstOrDefault(x => x.Id == id);
        if (r is null) return "El reporte no existe.";
        if (r.FinCorte is not null) return "Este corte ya figura como resuelto.";
        if (r.Estado is EstadoReporte.Rechazado or EstadoReporte.Duplicado) return "Un reporte rechazado o duplicado no se puede resolver.";
        if (!esAdmin && r.UsuarioId != usuarioId) return "No puedes modificar este reporte.";
        if (incidente && !esAdmin) return "Solo el administrador puede cerrar un incidente completo.";

        var ahora = DateTime.Now;
        var inc = Incidentes.De(todos, r)!;
        void Cerrar(Reporte x, string? motivo)
        {
            if (x.FinCorte is not null) return;
            x.FinCorte = ahora < x.InicioCorte ? x.InicioCorte : ahora;
            if (motivo is not null && string.IsNullOrEmpty(x.MotivoEstado)) x.MotivoEstado = motivo;
        }

        if (incidente)
        {
            foreach (var x in inc.Reportes.Where(x => x.Estado is EstadoReporte.Recibido or EstadoReporte.Verificado)) Cerrar(x, null);
        }
        else
        {
            Cerrar(r, null);
            var vigentes = inc.Reportes.Where(x => x.Estado is EstadoReporte.Recibido or EstadoReporte.Verificado).ToList();
            if (vigentes.Count >= 2 && vigentes.Count(x => x.FinCorte is not null) >= vigentes.Count * FraccionParaCerrarSolo)
                foreach (var x in vigentes) Cerrar(x, "Cerrado automáticamente: la mayoría de los vecinos indicó que volvió el agua.");
        }
        await _reportes.GuardarAsync();
        return null;
    }

    /// <summary>Cola de trabajo del administrador: incidentes filtrados y ordenados por urgencia.</summary>
    public async Task<ColaAdmin> ColaAdminAsync(string filtro, int? zonaId)
    {
        var todos = await _reportes.ListarTodosAsync();
        var incidentes = Incidentes.Agrupar(todos);
        if (zonaId is int z) incidentes = incidentes.Where(i => i.Reportes.Any(r => r.Zonas.Any(l => l.ZonaId == z))).ToList();

        bool Pend(Incidente i) => i.Abierto && i.Estado == EstadoReporte.Recibido;
        bool Verif(Incidente i) => i.Abierto && i.Estado == EstadoReporte.Verificado;
        var cola = new ColaAdmin
        {
            Conteos = new Dictionary<string, int>
            {
                ["pendientes"] = incidentes.Count(Pend),
                ["verificados"] = incidentes.Count(Verif),
                ["resueltos"] = incidentes.Count(i => i.Resuelto),
                ["descartados"] = incidentes.Count(i => i.Descartado),
                ["todos"] = incidentes.Count
            }
        };

        var elegidos = (filtro ?? "pendientes") switch
        {
            "verificados" => incidentes.Where(Verif),
            "resueltos" => incidentes.Where(i => i.Resuelto),
            "descartados" => incidentes.Where(i => i.Descartado),
            "todos" => incidentes,
            _ => incidentes.Where(Pend)
        };

        var abiertos = incidentes.Where(i => i.Abierto).ToList();
        double maxP = Math.Max(1.0, abiertos.Select(i => (double)i.Personas).DefaultIfEmpty(0).Max());
        double maxH = Math.Max(1.0, abiertos.Select(i => i.Horas).DefaultIfEmpty(0).Max());
        double Urgencia(Incidente i) => 0.5 * i.Personas / maxP + 0.5 * i.Horas / maxH;

        cola.Incidentes = elegidos.OrderByDescending(i => i.Abierto).ThenByDescending(Urgencia).ThenByDescending(i => i.Inicio).ToList();
        return cola;
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

    /// <summary>Cambia el estado de un reporte o, con <paramref name="incidente"/>, de todos los reportes de su incidente.</summary>
    public async Task<bool> CambiarEstadoAsync(int id, EstadoReporte estado, string? motivo = null, bool incidente = false)
    {
        var todos = await _reportes.ListarTodosAsync();
        var r = todos.FirstOrDefault(x => x.Id == id);
        if (r is null) return false;

        var afectados = incidente ? Incidentes.De(todos, r)!.Reportes : new List<Reporte> { r };
        motivo = (motivo ?? "").Trim();
        if (motivo.Length > 160) motivo = motivo[..160];
        foreach (var x in afectados)
        {
            x.Estado = estado;
            x.MotivoEstado = estado is EstadoReporte.Rechazado or EstadoReporte.Duplicado
                ? (motivo.Length > 0 ? motivo : "Sin motivo indicado.")
                : null;
        }
        await _reportes.GuardarAsync();
        return true;
    }

    /// <summary>Ids de reportes que confirman a otro más antiguo del mismo incidente (mismo lugar y mismo corte).</summary>
    public HashSet<int> Duplicados(IReadOnlyCollection<Reporte> lista) =>
        Incidentes.Agrupar(lista).Where(i => i.Reportes.Count > 1)
            .SelectMany(i => i.Reportes.OrderBy(r => r.Id).Skip(1)).Select(r => r.Id).ToHashSet();

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
