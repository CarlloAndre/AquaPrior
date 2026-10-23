using AquaPrior.Models;

namespace AquaPrior.ViewModels;

public class HomeVM
{
    public ResultadoPrioridad Resultado { get; set; } = new();
    public int Pendientes { get; set; }
}

public class PrioridadVM
{
    public string Clave { get; set; } = "saw";
    public int PesoPersonas { get; set; } = 50;
    public List<OpcionEstrategia> Estrategias { get; set; } = new();
    public ResultadoPrioridad Resultado { get; set; } = new();
}
