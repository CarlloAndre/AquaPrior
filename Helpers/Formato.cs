namespace AquaPrior.Helpers;

public static class Formato
{
    public static string Horas(double horas) =>
        horas < 1 ? "menos de 1 h" : $"{Math.Round(horas)} h";

    public static string ClaseNivel(string nivel) => nivel switch
    {
        "Alta" => "crit",
        "Media" => "mid",
        _ => "low"
    };
}
