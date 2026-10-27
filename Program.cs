using System.Globalization;
using AquaPrior.Data;
using AquaPrior.Repositories;
using AquaPrior.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AquaPriorContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("AquaPrior")));

// Capa de acceso a datos (patrón Repository)
builder.Services.AddScoped<IReporteRepository, ReporteRepository>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IZonaRepository, ZonaRepository>();

// Capa de negocio (patrón Strategy para la prioridad)
builder.Services.AddScoped<IPrioridadStrategy, PrioridadSaw>();
builder.Services.AddScoped<IPrioridadStrategy, PrioridadDuracion>();
builder.Services.AddScoped<IPrioridadStrategy, PrioridadPersonas>();
builder.Services.AddScoped<PrioridadService>();
builder.Services.AddSingleton<DistritoLocator>();
builder.Services.AddScoped<ReporteService>();
builder.Services.AddScoped<MapaService>();
builder.Services.AddScoped<ZonaService>();
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddScoped<AuthService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Cuenta/Login";
        o.AccessDeniedPath = "/Cuenta/AccesoDenegado";
        o.ExpireTimeSpan = TimeSpan.FromHours(8);
        o.Cookie.HttpOnly = true;
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AquaPriorContext>();
    db.Database.Migrate();   // aplica las migraciones pendientes (code-first con migraciones)
    DbSeeder.Sembrar(db);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Cultura fija con punto decimal: las coordenadas del mapa viajan como "-8.0912345" y, con una cultura
// que use coma decimal (es-ES), el punto se leería como separador de miles y fallaría la validación.
var cultura = (CultureInfo)CultureInfo.GetCultureInfo("es-PE").Clone();
cultura.NumberFormat.NumberDecimalSeparator = ".";
cultura.NumberFormat.NumberGroupSeparator = ",";
var localizacion = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(cultura),
    SupportedCultures = new List<CultureInfo> { cultura },
    SupportedUICultures = new List<CultureInfo> { cultura }
};
localizacion.RequestCultureProviders.Clear();
app.UseRequestLocalization(localizacion);

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();