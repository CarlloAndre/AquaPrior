using System.Globalization;
using System.Text;
using System.Xml.Linq;

namespace AquaPrior.Services;

/// <summary>
/// Dado un punto (latitud, longitud) devuelve el nombre del distrito que lo contiene,
/// usando los polígonos del KML (wwwroot/mapas/trujillo-distritos.kml). Se carga una sola vez.
/// </summary>
public class DistritoLocator
{
    private record Poligono(string Nombre, List<(double Lng, double Lat)> Anillo);
    private readonly List<Poligono> _poligonos = new();

    public DistritoLocator(IWebHostEnvironment env)
    {
        var ruta = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "mapas", "trujillo-distritos.kml");
        if (!File.Exists(ruta)) return;

        var doc = XDocument.Load(ruta);
        foreach (var pm in doc.Descendants().Where(e => e.Name.LocalName == "Placemark"))
        {
            var nombre = pm.Elements().FirstOrDefault(e => e.Name.LocalName == "name")?.Value?.Trim();
            if (string.IsNullOrEmpty(nombre)) continue;

            foreach (var poly in pm.Descendants().Where(e => e.Name.LocalName == "Polygon"))
            {
                var outer = poly.Descendants().FirstOrDefault(e => e.Name.LocalName == "outerBoundaryIs");
                var coords = outer?.Descendants().FirstOrDefault(e => e.Name.LocalName == "coordinates")?.Value;
                if (coords is null) continue;
                var anillo = coords.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Split(','))
                    .Where(p => p.Length >= 2)
                    .Select(p => (Lng: double.Parse(p[0], CultureInfo.InvariantCulture), Lat: double.Parse(p[1], CultureInfo.InvariantCulture)))
                    .ToList();
                if (anillo.Count >= 3) _poligonos.Add(new Poligono(Normalizar(nombre), anillo));
            }
        }
    }

    /// <summary>Nombre normalizado (sin tildes, minúsculas) del distrito que contiene el punto, o null si cae fuera.</summary>
    public string? NombreNormalizadoEn(double lat, double lng) =>
        _poligonos.FirstOrDefault(p => Contiene(p.Anillo, lat, lng))?.Nombre;

    public bool HayPoligonos => _poligonos.Count > 0;

    public static string Normalizar(string s)
    {
        var d = s.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in d)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    // Ray casting
    private static bool Contiene(List<(double Lng, double Lat)> a, double lat, double lng)
    {
        bool dentro = false;
        for (int i = 0, j = a.Count - 1; i < a.Count; j = i++)
        {
            if ((a[i].Lat > lat) != (a[j].Lat > lat) &&
                lng < (a[j].Lng - a[i].Lng) * (lat - a[i].Lat) / (a[j].Lat - a[i].Lat) + a[i].Lng)
                dentro = !dentro;
        }
        return dentro;
    }
}
