using System.Security.Claims;
using AquaPrior.Services;
using AquaPrior.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AquaPrior.Controllers;

/// <summary>CRUD independiente de Usuarios.</summary>
[Authorize(Roles = "Administrador")]
public class UsuariosController : Controller
{
    private readonly UsuarioService _usuarios;
    public UsuariosController(UsuarioService usuarios) => _usuarios = usuarios;

    private int ActualId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task<IActionResult> Index() => View(await _usuarios.ListarAsync());

    [HttpGet]
    public IActionResult Crear() => View(new UsuarioVM());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(UsuarioVM vm)
    {
        if (string.IsNullOrEmpty(vm.Clave))
            ModelState.AddModelError(nameof(vm.Clave), "Escribe una contraseña de al menos 8 caracteres.");
        if (ModelState.IsValid)
        {
            var (usuario, error) = await _usuarios.CrearAsync(vm.Nombre, vm.Correo, vm.Clave!, vm.Rol);
            if (usuario is not null)
            {
                TempData["Mensaje"] = "Usuario creado.";
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError(string.Empty, error!);
        }
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var u = await _usuarios.ObtenerAsync(id);
        return u is null ? NotFound() : View(new UsuarioVM { Id = u.Id, Nombre = u.Nombre, Correo = u.Correo, Rol = u.Rol });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(UsuarioVM vm)
    {
        if (ModelState.IsValid)
        {
            var error = await _usuarios.ActualizarAsync(vm.Id, vm.Nombre, vm.Correo, vm.Rol, vm.Clave);
            if (error is null)
            {
                TempData["Mensaje"] = "Usuario actualizado.";
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError(string.Empty, error);
        }
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Eliminar(int id)
    {
        var u = await _usuarios.ObtenerAsync(id);
        return u is null ? NotFound() : View(u);
    }

    [HttpPost, ActionName("Eliminar"), ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmarEliminar(int id)
    {
        TempData["Mensaje"] = await _usuarios.EliminarAsync(id, ActualId) ?? "Usuario eliminado.";
        return RedirectToAction(nameof(Index));
    }
}
