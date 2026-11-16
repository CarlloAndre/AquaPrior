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

    private static string? Validar(string direccion, string motivo, double latitud, double longitud, DateTime inicio, int? personas, TipoProblema tipo)
    {
        direccion = (direccion ?? "").Trim();
        if (direccion.Length < 4 || direccion.Length > 80) return "La dirección debe tener entre 4 y 80 caracteres.";
        motivo = (motivo ?? "").Trim();
        if (motivo.Length < 10 || motivo.Length > 500) return "El motivo debe tener entre 10 y 500 caracteres.";
        if (!double.IsFinite(latitud) || !double.IsFinite(longitud) || latitud is < -90 or > 90 || longitud is < -180 or > 180)
            return "Marca una ubicación válida en el mapa.";
        if (inicio > DateTime.Now) return "La fecha de inicio no puede estar en el futuro.";
        if (personas is <= 0 or > 1000000) return "El estimado de personas debe ser positivo; puedes dejarlo vacío si no lo sabes.";
        if (!Enum.IsDefined(tipo)) return "Selecciona un tipo de problema válido.";
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
            return (null, "El distrito geográfico aún no está habilitado. Solicita al administrador actualizar el catálogo.");
        return (zona.Id, null);
    }

    // C: crear el reporte; el sistema genera su línea de distrito a partir de la ubicación
    public async Task<string?> RegistrarAsync(int usuarioId, string direccion, string motivo, double latitud, double longitud, DateTime inicio, int? personas, TipoProblema tipo)
    {
        var error = Validar(direccion, motivo, latitud, longitud, inicio, personas, tipo);
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
            Tipo = tipo,
            Estado = EstadoReporte.Recibido,
            Zonas = new List<ReporteZona> { new() { ZonaId = zonaId!.Value, PersonasAfectadas = personas } }
        };
        await _reportes.AgregarAsync(reporte);
        await AplicarReglasAutomaticasAsync(reporte.Id);
        return null;
    }

    // U: actualizar; si el punto cambió de distrito, la línea de detalle se recalcula
    public async Task<string?> ActualizarAsync(int id, int usuarioId, bool esAdmin, string direccion, string motivo, double latitud, double longitud, DateTime inicio, int? personas, TipoProblema tipo)
    {
        var r = await _reportes.ObtenerConDetalleAsync(id);
        if (r is null) return "El reporte no existe.";
        if (!PuedeModificar(r, usuarioId, esAdmin)) return "No puedes modificar este reporte.";

        var error = Validar(direccion, motivo, latitud, longitud, inicio, personas, tipo);
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
        r.Tipo = tipo;
        await _reportes.GuardarAsync();
        if (r.Estado == EstadoReporte.Recibido) await AplicarReglasAutomaticasAsync(r.Id);
        return null;
    }

    // ---- Reglas automáticas (el administrador solo interviene en lo dudoso) ----


    /// <summary>
    /// Solo detecta duplicados del mismo ciudadano. Múltiples vecinos aportan corroboración,
    /// pero NUNCA constituyen verificación oficial por sí solos.
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

        // La verificación requiere una acción explícita del administrador.
    }

    /// <summary>El ciudadano confirma su propio suministro; el administrador puede cerrar todos los reportes tras verificar el restablecimiento.</summary>
    public async Task<string?> ResolverAsync(int id, int usuarioId, bool esAdmin, bool incidente)
    {
        var todos = await _reportes.ListarTodosAsync();
        var r = todos.FirstOrDefault(x => x.Id == id);
        if (r is null) return "El reporte no existe.";
        if (!incidente && r.FinCorte is not null) return "Este problema ya figura como resuelto.";
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
            // El restablecimiento de una vivienda no significa que todo el sector se recuperó.
            Cerrar(r, null);
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

        double Urgencia(Incidente i) => 0.5 * Math.Clamp((double)i.Personas / PrioridadService.ReferenciaPersonas, 0, 1)
            + 0.5 * Math.Clamp(i.Horas / PrioridadService.ReferenciaHoras, 0, 1);

        cola.Incidentes = elegidos.OrderByDescending(i => i.Abierto).ThenByDescending(Urgencia).ThenByDescending(i => i.Inicio).ToList();
        return cola;
    }

    /// <summary>Solo un administrador puede reabrir reportes cerrados por error.</summary>
    public async Task<string?> ReabrirAsync(int id, bool incidente)
    {
        var todos = await _reportes.ListarTodosAsync();
        var r = todos.FirstOrDefault(x => x.Id == id);
        if (r is null) return "El reporte no existe.";
        var afectados = incidente ? Incidentes.De(todos, r)?.Reportes ?? new List<Reporte> { r } : new List<Reporte> { r };
        if (!afectados.Any(x => x.FinCorte is not null)) return "No hay reportes resueltos para reabrir.";
        foreach (var x in afectados.Where(x => x.FinCorte is not null &&
                                                  x.Estado is EstadoReporte.Recibido or EstadoReporte.Verificado))
        {
            x.FinCorte = null;
            x.MotivoEstado = "Reabierto por el administrador. El suministro requiere nueva comprobación.";
        }
        await _reportes.GuardarAsync();
        return null;
    }

    /// <summary>Acción administrativa explícita de atención operativa.</summary>
    public async Task<string?> CambiarAtencionAsync(int id, bool incidente, EstadoAtencion atencion)
    {
        if (!Enum.IsDefined(atencion)) return "Estado de atención inválido.";
        var todos = await _reportes.ListarTodosAsync();
        var r = todos.FirstOrDefault(x => x.Id == id);
        if (r is null) return "El reporte no existe.";
        var afectados = incidente ? Incidentes.De(todos, r)?.Reportes ?? new List<Reporte> { r } : new List<Reporte> { r };
        foreach (var x in afectados.Where(x => x.FinCorte is null && x.Estado == EstadoReporte.Verificado))
            x.Atencion = atencion;
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

    /// <summary>Cambia el estado de un reporte o, con <paramref name="incidente"/>, de todos los reportes de su incidente.</summary>
    public async Task<bool> CambiarEstadoAsync(int id, EstadoReporte estado, string? motivo = null, bool incidente = false)
    {
        var todos = await _reportes.ListarTodosAsync();
        var r = todos.FirstOrDefault(x => x.Id == id);
        if (r is null) return false;

        if (!Enum.IsDefined(estado)) return false;
        var afectados = incidente ? Incidentes.De(todos, r)!.Reportes : new List<Reporte> { r };
        motivo = (motivo ?? "").Trim();
        if (motivo.Length > 160) motivo = motivo[..160];
        foreach (var x in afectados)
        {
            x.Estado = estado;
            if (estado == EstadoReporte.Recibido) x.Atencion = EstadoAtencion.Pendiente;
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
        var resueltos = verificados.Where(r => r.FinCorte is not null).ToList();
        var incidentes = Incidentes.Agrupar(todos);
        return new Estadisticas
        {
            Total = todos.Count,
            Verificados = verificados.Count,
            PorcentajeVerificados = todos.Count == 0 ? 0 : (int)Math.Round(100.0 * verificados.Count / todos.Count),
            PromedioHoras = resueltos.Count == 0 ? 0 : resueltos.Average(r => r.HorasAfectacion),
            IncidentesActivos = incidentes.Count(i => i.Abierto),
            IncidentesResueltos = incidentes.Count(i => i.Resuelto),
            PorEstado = Enum.GetValues<EstadoReporte>()
                .Select(e => new Conteo { Etiqueta = e.ToString(), Cantidad = todos.Count(r => r.Estado == e) }).ToList(),
            PorZona = todos.SelectMany(r => r.Zonas).GroupBy(rz => rz.Zona.Nombre)
                .Select(g => new Conteo { Etiqueta = g.Key, Cantidad = g.Count() })
                .OrderByDescending(c => c.Cantidad).ToList()
        };
    }
}
