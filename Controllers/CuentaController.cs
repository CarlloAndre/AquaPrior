using AquaPrior.Services;
using AquaPrior.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace AquaPrior.Controllers;

public class CuentaController : Controller
{
    private readonly AuthService _auth;
    public CuentaController(AuthService auth) => _auth = auth;

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginVM());
    }

    // CUS-08: iniciar sesión
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVM vm, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid) return View(vm);

        var usuario = await _auth.ValidarAsync(vm.Correo, vm.Clave);
        if (usuario is null)
        {
            ModelState.AddModelError(string.Empty, "Correo o contraseña incorrectos.");
            return View(vm);
        }

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, _auth.CrearPrincipal(usuario));
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return LocalRedirect(returnUrl);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Registro() => View(new RegistroVM());

    // CUS-09: registrarse como ciudadano
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Registro(RegistroVM vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var (usuario, error) = await _auth.RegistrarAsync(vm.Nombre, vm.Correo, vm.Clave);
        if (usuario is null)
        {
            ModelState.AddModelError(nameof(vm.Correo), error!);
            return View(vm);
        }

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, _auth.CrearPrincipal(usuario));
        TempData["Mensaje"] = "Cuenta creada. Ya puedes reportar una interrupción.";
        return RedirectToAction("Crear", "Reportes");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Salir()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    public IActionResult AccesoDenegado() => View();
}
