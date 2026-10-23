namespace AquaPrior.Models;

/// <summary>Datos consolidados por zona (solo reportes verificados). Los produce el negocio.</summary>
public record ZonaConsolidada(string Zona, int Reportes, int? Personas, double Horas);

public class ZonaPrioridad
{
    public string Zona { get; set; } = "";
    public int Reportes { get; set; }
    public int? Personas { get; set; }
    public double Horas { get; set; }
    public double Puntaje { get; set; }
    public string Nivel { get; set; } = "Baja";
}

public class ResultadoPrioridad
{
    public List<ZonaPrioridad> Ranking { get; set; } = new();
    public List<string> SinDatos { get; set; } = new();
}

public class OpcionEstrategia
{
    public string Clave { get; set; } = "";
    public string Nombre { get; set; } = "";
}

public class Conteo
{
    public string Etiqueta { get; set; } = "";
    public int Cantidad { get; set; }
}

public class Estadisticas
{
    public int Total { get; set; }
    public int Verificados { get; set; }
    public int PorcentajeVerificados { get; set; }
    public double PromedioHoras { get; set; }
    public List<Conteo> PorEstado { get; set; } = new();
    public List<Conteo> PorZona { get; set; } = new();
}
