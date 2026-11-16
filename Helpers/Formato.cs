using AquaPrior.Models;

namespace AquaPrior.Helpers;

public static class Formato
{
    public static string Horas(double horas) =>
        horas < 1 ? "menos de 1 h" : $"{Math.Round(horas)} h";

    public static string Tipo(TipoProblema tipo) => tipo switch
    {
        TipoProblema.CorteTotal => "Corte total",
        TipoProblema.BajaPresion => "Baja presión",
        TipoProblema.CalidadAgua => "Problema de calidad del agua",
        _ => "Otro problema"
    };

    public static string Duracion(Reporte r) =>
        r.Tipo == TipoProblema.CorteTotal
            ? Horas(r.HorasSinServicio) + (r.FinCorte is null ? " sin servicio" : " de interrupción")
            : Horas(r.HorasAfectacion) + (r.FinCorte is null ? " con el problema" : " de afectación");

    public static string ClaseNivel(string nivel) => nivel switch
    {
        "Alta" => "crit",
        "Media" => "mid",
        _ => "low"
    };

    /// <summary>Distritos del reporte, con las personas afectadas de cada uno cuando se conocen.</summary>
    public static string Zonas(Reporte r) =>
        r.Zonas.Count == 0
            ? "Sin distrito"
            : string.Join(", ", r.Zonas.OrderBy(z => z.Zona.Nombre)
                .Select(z => z.PersonasAfectadas is int p ? $"{z.Zona.Nombre} ({p} pers.)" : z.Zona.Nombre));
}
