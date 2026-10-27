using AquaPrior.Services;
using Microsoft.AspNetCore.Mvc;

namespace AquaPrior.Controllers;

public class MapaController : Controller
{
    private readonly MapaService _mapa;
    public MapaController(MapaService mapa) => _mapa = mapa;

    public async Task<IActionResult> Index() => View(await _mapa.ObtenerAsync());
}
