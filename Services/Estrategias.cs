using AquaPrior.Models;

namespace AquaPrior.Services;

/// <summary>Simple Additive Weighting: S = w1 · (P / Pmax) + w2 · (H / Hmax), con w1 + w2 = 1.</summary>
public class PrioridadSaw : IPrioridadStrategy
{
    public string Clave => "saw";
    public string Nombre => "Combinada (SAW: personas y duración)";
    // Si no existe población estimada, se prioriza por duración sin inventar un cero.
    public bool RequierePersonas => false;

    public double Calcular(ZonaConsolidada z, double maxPersonas, double maxHoras, double pesoPersonas) =>
        z.Personas.HasValue
            ? pesoPersonas * Math.Clamp((double)z.Personas.Value / maxPersonas, 0, 1) + (1 - pesoPersonas) * Math.Clamp(z.Horas / maxHoras, 0, 1)
            : Math.Clamp(z.Horas / maxHoras, 0, 1);
}

public class PrioridadDuracion : IPrioridadStrategy
{
    public string Clave => "duracion";
    public string Nombre => "Solo duración de la interrupción";
    public bool RequierePersonas => false;

    public double Calcular(ZonaConsolidada z, double maxPersonas, double maxHoras, double pesoPersonas) =>
        Math.Clamp(z.Horas / maxHoras, 0, 1);
}

public class PrioridadPersonas : IPrioridadStrategy
{
    public string Clave => "personas";
    public string Nombre => "Solo personas afectadas";
    public bool RequierePersonas => true;

    public double Calcular(ZonaConsolidada z, double maxPersonas, double maxHoras, double pesoPersonas) =>
        Math.Clamp((double)(z.Personas ?? 0) / maxPersonas, 0, 1);
}
