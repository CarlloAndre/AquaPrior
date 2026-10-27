using System.Security.Claims;
using AquaPrior.Models;
using AquaPrior.Repositories;
using AquaPrior.Services;
using AquaPrior.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AquaPrior.Controllers;

/// <summary>CRUD independiente de Reportes (maestro) con sus distritos afectados (detalle).</summary>
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

    private static DateTime SinSegundos(DateTime d) => new(d.Ticks - d.Ticks % TimeSpan.TicksPerMinute);

    private IActionResult Volver(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return LocalRedirect(returnUrl);
        return EsAdmin ? RedirectToAction(nameof(Index)) : RedirectToAction(nameof(Mis));
    }

    // R: cola de trabajo del administrador. Incidentes (reportes cercanos) ordenados por urgencia.
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Index(string filtro = "pendientes", int? zonaId = null)
    {
        var cola = await _servicio.ColaAdminAsync(filtro, zonaId);
        return View(new ColaAdminVM
        {
            Filtro = filtro,
            ZonaId = zonaId,
            Cola = cola,
            Zonas = (await _zonas.ListarAsync()).Select(z => new SelectListItem(z.Nombre, z.Id.ToString(), z.Id == zonaId)).ToList()
        });
    }

    // R: historial propio (ciudadano)
    public async Task<IActionResult> Mis() => View(await _reportes.ListarPorUsuarioAsync(UsuarioId));

    // C: crear (CUS-01). El distrito se deduce de la ubicación exacta marcada en el mapa.
    [HttpGet]
    public IActionResult Crear(string? returnUrl) =>
        View(new ReporteVM { InicioCorte = SinSegundos(DateTime.Now.AddHours(-1)), ReturnUrl = returnUrl });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(ReporteVM vm)
    {
        if (ModelState.IsValid)
        {
            var error = await _servicio.RegistrarAsync(UsuarioId, vm.Direccion, vm.Motivo, vm.Latitud!.Value, vm.Longitud!.Value, vm.InicioCorte!.Value, vm.PersonasAfectadas);
            if (error is null)
            {
                TempData["Mensaje"] = "Reporte enviado. Queda pendiente de revisión por el administrador.";
                return Volver(vm.ReturnUrl);
            }
            ModelState.AddModelError(string.Empty, error);
        }
        return View(vm);
    }

    // U: editar
    [HttpGet]
    public async Task<IActionResult> Editar(int id, string? returnUrl)
    {
        var r = await _reportes.ObtenerConDetalleAsync(id);
        if (r is null) return NotFound();
        if (!ReporteService.PuedeModificar(r, UsuarioId, EsAdmin)) return Forbid();

        var vm = new ReporteVM
        {
            Id = r.Id,
            Direccion = r.Direccion,
            Motivo = r.Motivo,
            Latitud = r.Latitud,
            Longitud = r.Longitud,
            InicioCorte = SinSegundos(r.InicioCorte),
            ReturnUrl = returnUrl,
            PersonasAfectadas = r.Zonas.Select(z => z.PersonasAfectadas).Max()
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(ReporteVM vm)
    {
        if (ModelState.IsValid)
        {
            var error = await _servicio.ActualizarAsync(vm.Id, UsuarioId, EsAdmin, vm.Direccion, vm.Motivo, vm.Latitud!.Value, vm.Longitud!.Value, vm.InicioCorte!.Value, vm.PersonasAfectadas);
            if (error is null)
            {
                TempData["Mensaje"] = "Reporte actualizado.";
                return Volver(vm.ReturnUrl);
            }
            ModelState.AddModelError(string.Empty, error);
        }
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

    // CUS-04: verificar / rechazar reporte (o todo su incidente)
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Administrador")]
    public async Task<IActionResult> CambiarEstado(int id, EstadoReporte estado, string? motivo, bool incidente, string? returnUrl)
    {
        TempData["Mensaje"] = await _servicio.CambiarEstadoAsync(id, estado, motivo, incidente) ? "Estado actualizado." : "No se encontró el reporte.";
        return Volver(returnUrl);
    }

    // Ya volvió el agua: el ciudadano cierra su reporte; el administrador puede cerrar todo el incidente.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Resolver(int id, bool incidente, string? returnUrl)
    {
        var error = await _servicio.ResolverAsync(id, UsuarioId, EsAdmin, incidente);
        TempData["Mensaje"] = error ?? (incidente ? "Incidente marcado como resuelto." : "Gracias: registramos que el servicio volvió.");
        return Volver(returnUrl);
    }
}
