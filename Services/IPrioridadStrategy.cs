using AquaPrior.Models;

namespace AquaPrior.Services;

/// <summary>Patrón Strategy: cada regla de cálculo de prioridad es intercambiable.</summary>
public interface IPrioridadStrategy
{
    string Clave { get; }
    string Nombre { get; }
    bool RequierePersonas { get; }
    double Calcular(ZonaConsolidada zona, double maxPersonas, double maxHoras, double pesoPersonas);
}
