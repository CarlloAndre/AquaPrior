using AquaPrior.Models;
using AquaPrior.Repositories;
using AquaPrior.Services;
using AquaPrior.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AquaPrior.Controllers;

public class HomeController : Controller
{
    private readonly PrioridadService _prioridad;
    private readonly IReporteRepository _reportes;

    public HomeController(PrioridadService prioridad, IReporteRepository reportes)
    {
        _prioridad = prioridad;
        _reportes = reportes;
    }

    // CUS-02: consultar zonas afectadas (visitante, ciudadano o administrador)
    public async Task<IActionResult> Index() => View(new HomeVM
    {
        Resultado = await _prioridad.CalcularAsync(),
        Pendientes = await _reportes.ContarPorEstadoAsync(EstadoReporte.Recibido)
    });

    public IActionResult Error() => View();
}
