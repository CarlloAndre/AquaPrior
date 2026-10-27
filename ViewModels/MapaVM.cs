namespace AquaPrior.ViewModels;

/// <summary>Resumen por distrito (se muestra al hacer clic en el polígono).</summary>
public class MapaVM
{
    public string Nombre { get; set; } = "";
    public int Total { get; set; }
    public int Verificados { get; set; }
    public int Recibidos { get; set; }
}

/// <summary>Un reporte con su ubicación exacta y su intensidad (0 a 1) para el mapa de calor.</summary>
public class PuntoCalorVM
{
    public double Lat { get; set; }
    public double Lng { get; set; }
    public double Intensidad { get; set; }
    public string Direccion { get; set; } = "";
    public int? Personas { get; set; }
    public double Horas { get; set; }
    public string Estado { get; set; } = "";
}

public class MapaCalorVM
{
    public List<PuntoCalorVM> Puntos { get; set; } = new();
    public List<MapaVM> Distritos { get; set; } = new();
}
