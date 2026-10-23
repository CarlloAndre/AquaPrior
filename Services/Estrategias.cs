using AquaPrior.Models;

namespace AquaPrior.Services;

/// <summary>Simple Additive Weighting: S = w1 · (P / Pmax) + w2 · (H / Hmax), con w1 + w2 = 1.</summary>
public class PrioridadSaw : IPrioridadStrategy
{
    public string Clave => "saw";
    public string Nombre => "Combinada (SAW: personas y duración)";
    public bool RequierePersonas => true;

    public double Calcular(ZonaConsolidada z, double maxPersonas, double maxHoras, double pesoPersonas) =>
        pesoPersonas * ((z.Personas ?? 0) / maxPersonas) + (1 - pesoPersonas) * (z.Horas / maxHoras);
}

public class PrioridadDuracion : IPrioridadStrategy
{
    public string Clave => "duracion";
    public string Nombre => "Solo duración de la interrupción";
    public bool RequierePersonas => false;

    public double Calcular(ZonaConsolidada z, double maxPersonas, double maxHoras, double pesoPersonas) =>
        z.Horas / maxHoras;
}

public class PrioridadPersonas : IPrioridadStrategy
{
    public string Clave => "personas";
    public string Nombre => "Solo personas afectadas";
    public bool RequierePersonas => true;

    public double Calcular(ZonaConsolidada z, double maxPersonas, double maxHoras, double pesoPersonas) =>
        (z.Personas ?? 0) / maxPersonas;
}
