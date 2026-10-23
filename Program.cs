using AquaPrior.Data;
using AquaPrior.Repositories;
using AquaPrior.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
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
builder.Services.AddScoped<ReporteService>();
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
    db.Database.EnsureCreated();
    DbSeeder.Sembrar(db);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();
