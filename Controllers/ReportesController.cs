using System.Security.Claims;
using AquaPrior.Models;
using AquaPrior.Repositories;
using AquaPrior.Services;
using AquaPrior.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AquaPrior.Controllers;

/// <summary>CRUD independiente de Reportes.</summary>
[Authorize]
public class ReportesController : Controller
{
    private readonly ReporteService _servicio;
    private readonly IReporteRepository _reportes;
    private readonly IZonaRepository _zonas;

    public ReportesController(ReporteService servicio, IReporteRepository reportes, IZonaRepository zonas)
    {
        _servicio = servicio;
        _reportes = reportes;
        _zonas = zonas;
    }

    private int UsuarioId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool EsAdmin => User.IsInRole("Administrador");

    private async Task CargarZonasAsync(ReporteVM vm) =>
        vm.Zonas = (await _zonas.ListarAsync()).Select(z => new SelectListItem(z.Nombre, z.Id.ToString()));

    private static DateTime SinSegundos(DateTime d) => new(d.Ticks - d.Ticks % TimeSpan.TicksPerMinute);

    private IActionResult Volver(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return LocalRedirect(returnUrl);
        return EsAdmin ? RedirectToAction(nameof(Index)) : RedirectToAction(nameof(Mis));
    }

    // R: listado general (administrador)
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Index()
    {
        var todos = await _reportes.ListarTodosAsync();
        var duplicados = _servicio.Duplicados(todos);
        return View(todos.Select(r => new ReporteFilaVM { Reporte = r, EsPosibleDuplicado = duplicados.Contains(r.Id) }).ToList());
    }

    // R: historial propio (ciudadano)
    public async Task<IActionResult> Mis() => View(await _reportes.ListarPorUsuarioAsync(UsuarioId));

    // C: crear (CUS-01)
    [HttpGet]
    public async Task<IActionResult> Crear(int? zonaId, string? returnUrl)
    {
        var vm = new ReporteVM { ZonaId = zonaId, InicioCorte = SinSegundos(DateTime.Now.AddHours(-1)), ReturnUrl = returnUrl };
        await CargarZonasAsync(vm);
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(ReporteVM vm)
    {
        if (ModelState.IsValid)
        {
            var error = await _servicio.RegistrarAsync(UsuarioId, vm.ZonaId!.Value, vm.Direccion, vm.InicioCorte!.Value, vm.PersonasAfectadas);
            if (error is null)
            {
                TempData["Mensaje"] = "Reporte enviado. Queda pendiente de revisión por el administrador.";
                return Volver(vm.ReturnUrl);
            }
            ModelState.AddModelError(string.Empty, error);
        }
        await CargarZonasAsync(vm);
        return View(vm);
    }

    // U: editar
    [HttpGet]
    public async Task<IActionResult> Editar(int id, string? returnUrl)
    {
        var r = await _reportes.ObtenerAsync(id);
        if (r is null) return NotFound();
        if (!ReporteService.PuedeModificar(r, UsuarioId, EsAdmin)) return Forbid();

        var vm = new ReporteVM
        {
            Id = r.Id, ZonaId = r.ZonaId, Direccion = r.Direccion, InicioCorte = SinSegundos(r.InicioCorte),
            PersonasAfectadas = r.PersonasAfectadas, ReturnUrl = returnUrl
        };
        await CargarZonasAsync(vm);
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(ReporteVM vm)
    {
        if (ModelState.IsValid)
        {
            var error = await _servicio.ActualizarAsync(vm.Id, UsuarioId, EsAdmin, vm.ZonaId!.Value, vm.Direccion, vm.InicioCorte!.Value, vm.PersonasAfectadas);
            if (error is null)
            {
                TempData["Mensaje"] = "Reporte actualizado.";
                return Volver(vm.ReturnUrl);
            }
            ModelState.AddModelError(string.Empty, error);
        }
        await CargarZonasAsync(vm);
        return View(vm);
    }

    // D: eliminar (confirmación y borrado)
    [HttpGet]
    public async Task<IActionResult> Eliminar(int id, string? returnUrl)
    {
        var r = await _reportes.ObtenerConDetalleAsync(id);
        if (r is null) return NotFound();
        if (!ReporteService.PuedeModificar(r, UsuarioId, EsAdmin)) return Forbid();
        ViewData["ReturnUrl"] = returnUrl;
        return View(r);
    }

    [HttpPost, ActionName("Eliminar"), ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmarEliminar(int id, string? returnUrl)
    {
        var error = await _servicio.EliminarAsync(id, UsuarioId, EsAdmin);
        TempData["Mensaje"] = error ?? "Reporte eliminado.";
        return Volver(returnUrl);
    }

    // CUS-04: verificar reporte
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Administrador")]
    public async Task<IActionResult> CambiarEstado(int id, EstadoReporte estado, string? returnUrl)
    {
        TempData["Mensaje"] = await _servicio.CambiarEstadoAsync(id, estado) ? "Estado actualizado." : "No se encontró el reporte.";
        return Volver(returnUrl);
    }
}
