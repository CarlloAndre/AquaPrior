using System.Security.Claims;
using AquaPrior.Models;
using AquaPrior.Repositories;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;

namespace AquaPrior.Services;

public class AuthService
{
    private readonly IUsuarioRepository _usuarios;
    private readonly UsuarioService _usuarioService;
    private readonly PasswordHasher<Usuario> _hasher = new();

    public AuthService(IUsuarioRepository usuarios, UsuarioService usuarioService)
    {
        _usuarios = usuarios;
        _usuarioService = usuarioService;
    }

    public async Task<Usuario?> ValidarAsync(string correo, string clave)
    {
        var u = await _usuarios.PorCorreoAsync(UsuarioService.Normalizar(correo));
        if (u is null) return null;
        var r = _hasher.VerifyHashedPassword(u, u.ClaveHash, clave);
        return r == PasswordVerificationResult.Failed ? null : u;
    }

    /// <summary>Autorregistro: siempre crea un ciudadano.</summary>
    public Task<(Usuario? Usuario, string? Error)> RegistrarAsync(string nombre, string correo, string clave) =>
        _usuarioService.CrearAsync(nombre, correo, clave, Rol.Ciudadano);

    public ClaimsPrincipal CrearPrincipal(Usuario u)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, u.Id.ToString()),
            new(ClaimTypes.Name, u.Nombre),
            new(ClaimTypes.Role, u.Rol.ToString())
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }
}
