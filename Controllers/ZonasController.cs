using AquaPrior.Models;
using AquaPrior.Services;
using AquaPrior.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AquaPrior.Controllers;

/// <summary>CRUD independiente de Zonas (distritos) y vista de los reportes que afectan a cada una.</summary>
[Authorize(Roles = "Administrador")]
public class ZonasController : Controller
{
    private readonly ZonaService _zonas;
    private readonly ReporteService _reportes;

    public ZonasController(ZonaService zonas, ReporteService reportes)
    {
        _zonas = zonas;
        _reportes = reportes;
    }

    public async Task<IActionResult> Index() => View(await _zonas.ListarAsync());

    // El distrito (maestro) con los reportes que lo afectan (detalle), según la tabla intermedia ReporteZona
    public async Task<IActionResult> Detalle(int id)
    {
        var zona = await _zonas.ObtenerAsync(id);
        if (zona is null) return NotFound();

        var lineas = zona.ReporteZonas.OrderByDescending(rz => rz.ReporteId).ToList();
        var reportes = lineas.Select(rz => rz.Reporte).ToList();
        var duplicados = _reportes.Duplicados(reportes);
        return View(new ZonaDetalleVM
        {
            Zona = zona,
            Reportes = lineas.Select(rz => new ReporteFilaVM
            {
                Reporte = rz.Reporte,
                EsPosibleDuplicado = duplicados.Contains(rz.ReporteId),
                PersonasEnZona = rz.PersonasAfectadas
            }).ToList(),
            Verificados = reportes.Count(r => r.Estado == EstadoReporte.Verificado)
        });
    }

    [HttpGet]
    public IActionResult Crear() => View(new ZonaVM());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(ZonaVM vm)
    {
        if (ModelState.IsValid)
        {
            var error = await _zonas.CrearAsync(vm.Nombre);
            if (error is null)
            {
                TempData["Mensaje"] = "Distrito creado.";
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError(nameof(vm.Nombre), error);
        }
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var z = await _zonas.ObtenerAsync(id);
        return z is null ? NotFound() : View(new ZonaVM { Id = z.Id, Nombre = z.Nombre });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(ZonaVM vm)
    {
        if (ModelState.IsValid)
        {
            var error = await _zonas.ActualizarAsync(vm.Id, vm.Nombre);
            if (error is null)
            {
                TempData["Mensaje"] = "Distrito actualizado.";
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError(nameof(vm.Nombre), error);
        }
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Eliminar(int id)
    {
        var z = await _zonas.ObtenerAsync(id);
        return z is null ? NotFound() : View(z);
    }

    [HttpPost, ActionName("Eliminar"), ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmarEliminar(int id)
    {
        TempData["Mensaje"] = await _zonas.EliminarAsync(id) ?? "Distrito eliminado.";
        return RedirectToAction(nameof(Index));
    }
}
