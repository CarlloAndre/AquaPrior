namespace AquaPrior.Models;

/// <summary>
/// Un incidente es un conjunto de reportes cercanos (unos 120 m) cuyos cortes coinciden en el tiempo:
/// los vecinos que reportan el mismo corte. Se calcula, no se guarda. Lo arma <c>Services/Incidentes.cs</c>.
/// </summary>
public class Incidente
{
    public List<Reporte> Reportes { get; init; } = new();

    public bool TieneUbicacion => Reportes.Any(r => r.Latitud.HasValue && r.Longitud.HasValue);
    public double Lat => Reportes.Where(r => r.Latitud.HasValue).Select(r => r.Latitud!.Value).DefaultIfEmpty(0).Average();
    public double Lng => Reportes.Where(r => r.Longitud.HasValue).Select(r => r.Longitud!.Value).DefaultIfEmpty(0).Average();

    public int Usuarios => Reportes.Select(r => r.UsuarioId).Distinct().Count();
    /// <summary>Los vecinos del mismo incidente viven en el mismo entorno: se toma el mayor estimado, no la suma.</summary>
    public int Personas => Reportes.Select(r => r.Personas).DefaultIfEmpty(0).Max();
    public double Horas => Reportes.Select(r => r.HorasSinServicio).DefaultIfEmpty(0).Max();
    public DateTime Inicio => Reportes.Min(r => r.InicioCorte);

    private static bool Vigente(Reporte r) => r.Estado == EstadoReporte.Recibido || r.Estado == EstadoReporte.Verificado;

    /// <summary>Tiene al menos un reporte que no fue rechazado ni marcado como duplicado.</summary>
    public bool Activo => Reportes.Any(Vigente);
    public bool Abierto => Reportes.Any(r => Vigente(r) && r.FinCorte is null);
    public bool Resuelto => Activo && !Abierto;
    public bool Descartado => !Activo;

    public EstadoReporte Estado =>
        Reportes.Any(r => r.Estado == EstadoReporte.Verificado) ? EstadoReporte.Verificado
        : Reportes.Any(r => r.Estado == EstadoReporte.Recibido) ? EstadoReporte.Recibido
        : Reportes[0].Estado;

    public string ZonaNombre => Reportes.SelectMany(r => r.Zonas).Select(z => z.Zona.Nombre).FirstOrDefault() ?? "Sin distrito";
    public int ZonaId => Reportes.SelectMany(r => r.Zonas).Select(z => z.ZonaId).FirstOrDefault();
    public string Direccion => Reportes.OrderBy(r => r.Id).First().Direccion;
    public int ReporteBaseId => Reportes.OrderBy(r => r.Id).First().Id;
}

/// <summary>Resultado de la cola de trabajo del administrador.</summary>
public class ColaAdmin
{
    public List<Incidente> Incidentes { get; set; } = new();
    public Dictionary<string, int> Conteos { get; set; } = new();
}
