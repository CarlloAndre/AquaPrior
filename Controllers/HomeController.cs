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
    public async Task<IActionResult> Index()
    {
        var reportes = await _reportes.ListarTodosAsync();
        var pendientes = Incidentes.Agrupar(reportes.Where(r => r.Estado == EstadoReporte.Recibido));
        return View(new HomeVM
        {
            Resultado = await _prioridad.CalcularAsync(),
            Pendientes = pendientes.Sum(i => i.Reportes.Select(r => r.UsuarioId).Distinct().Count())
        });
    }

    public IActionResult Error() => View();
}
