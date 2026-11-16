using AquaPrior.Models;

namespace AquaPrior.Services;

/// <summary>Agrupa reportes en incidentes: cercanos en el espacio y con cortes que se solapan en el tiempo.</summary>
public static class Incidentes
{
    public const double RadioMetros = 120;

    public static List<Incidente> Agrupar(IEnumerable<Reporte> reportes)
    {
        var lista = reportes.ToList();
        // Enlace completo: cada reporte debe coincidir con TODOS los del grupo.
        // Evita unir A y C a través de B cuando A y C representan puntos muy separados.
        var grupos = new List<List<Reporte>>();
        foreach (var reporte in lista.OrderBy(r => r.Id))
        {
            var grupo = grupos.FirstOrDefault(g => g.All(r => Unibles(r, reporte)));
            if (grupo is null) grupos.Add(new List<Reporte> { reporte });
            else grupo.Add(reporte);
        }

        return grupos.Select(g => new Incidente { Reportes = g }).ToList();
    }

    public static Incidente? De(IEnumerable<Reporte> reportes, Reporte reporte) =>
        Agrupar(reportes).FirstOrDefault(i => i.Reportes.Contains(reporte));

    private static bool Vigente(Reporte r) => r.Estado == EstadoReporte.Recibido || r.Estado == EstadoReporte.Verificado;

    public static bool Unibles(Reporte a, Reporte b)
    {
        if (!Vigente(a) || !Vigente(b) || a.Tipo != b.Tipo) return false;
        // Dos reportes en distritos distintos no se fusionan automáticamente.
        if (a.Zonas.Count != 0 && b.Zonas.Count != 0 &&
            !a.Zonas.Any(za => b.Zonas.Any(zb => za.ZonaId == zb.ZonaId))) return false;
        if (a.Latitud is null || a.Longitud is null || b.Latitud is null || b.Longitud is null) return false;
        if (DistanciaMetros(a.Latitud.Value, a.Longitud.Value, b.Latitud.Value, b.Longitud.Value) > RadioMetros) return false;
        var ahora = DateTime.Now;
        return a.InicioCorte <= (b.FinCorte ?? ahora) && b.InicioCorte <= (a.FinCorte ?? ahora);
    }

    public static double DistanciaMetros(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 6371000;
        double ToRad(double g) => g * Math.PI / 180;
        var dLat = ToRad(lat2 - lat1); var dLng = ToRad(lng2 - lng1);
        var h = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return 2 * R * Math.Asin(Math.Min(1, Math.Sqrt(h)));
    }
}
