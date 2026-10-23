using AquaPrior.Services;
using AquaPrior.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AquaPrior.Controllers;

/// <summary>Solo reportes de análisis: priorización y estadísticas. Los CRUD tienen su propio controlador.</summary>
[Authorize(Roles = "Administrador")]
public class AdminController : Controller
{
    private readonly ReporteService _reportes;
    private readonly PrioridadService _prioridad;

    public AdminController(ReporteService reportes, PrioridadService prioridad)
    {
        _reportes = reportes;
        _prioridad = prioridad;
    }

    // CUS-05 y CUS-06: información consolidada y priorización
    public async Task<IActionResult> Prioridad(string clave = "saw", int peso = 50)
    {
        peso = Math.Clamp(peso, 0, 100);
        return View(new PrioridadVM
        {
            Clave = clave,
            PesoPersonas = peso,
            Estrategias = _prioridad.Opciones(),
            Resultado = await _prioridad.CalcularAsync(clave, peso / 100.0)
        });
    }

    // CUS-07: consultar estadísticas
    public async Task<IActionResult> Estadisticas() => View(await _reportes.EstadisticasAsync());
}
