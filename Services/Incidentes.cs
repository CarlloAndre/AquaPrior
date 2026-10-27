using AquaPrior.Models;

namespace AquaPrior.Services;

/// <summary>Agrupa reportes en incidentes: cercanos en el espacio y con cortes que se solapan en el tiempo.</summary>
public static class Incidentes
{
    public const double RadioMetros = 120;

    public static List<Incidente> Agrupar(IEnumerable<Reporte> reportes)
    {
        var lista = reportes.ToList();
        var padre = Enumerable.Range(0, lista.Count).ToArray();
        int Raiz(int x) { while (padre[x] != x) { padre[x] = padre[padre[x]]; x = padre[x]; } return x; }

        for (int i = 0; i < lista.Count; i++)
            for (int j = i + 1; j < lista.Count; j++)
                if (Unibles(lista[i], lista[j])) padre[Raiz(i)] = Raiz(j);

        return lista.Select((r, i) => (r, raiz: Raiz(i)))
            .GroupBy(x => x.raiz)
            .Select(g => new Incidente { Reportes = g.Select(x => x.r).OrderBy(r => r.Id).ToList() })
            .ToList();
    }

    public static Incidente? De(IEnumerable<Reporte> reportes, Reporte reporte) =>
        Agrupar(reportes).FirstOrDefault(i => i.Reportes.Contains(reporte));

    private static bool Vigente(Reporte r) => r.Estado == EstadoReporte.Recibido || r.Estado == EstadoReporte.Verificado;

    public static bool Unibles(Reporte a, Reporte b)
    {
        if (!Vigente(a) || !Vigente(b)) return false;
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
