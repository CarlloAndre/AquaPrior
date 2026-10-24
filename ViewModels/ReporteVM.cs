using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AquaPrior.ViewModels;

/// <summary>Una línea del formulario: un distrito afectado y, opcionalmente, sus personas.</summary>
public class LineaZonaVM
{
    [Required(ErrorMessage = "Elige un distrito.")]
    public int? ZonaId { get; set; }

    [Range(1, 1000000, ErrorMessage = "Las personas afectadas deben ser un número mayor a 0.")]
    public int? PersonasAfectadas { get; set; }
}

/// <summary>Formulario maestro-detalle: cabecera del reporte y varias líneas de distritos.</summary>
public class ReporteVM
{
    public int Id { get; set; }
    public string? ReturnUrl { get; set; }

    [Required(ErrorMessage = "Indica una dirección o referencia."), StringLength(80, MinimumLength = 4, ErrorMessage = "Usa entre 4 y 80 caracteres.")]
    [Display(Name = "Dirección o referencia")]
    public string Direccion { get; set; } = "";

    [Required(ErrorMessage = "Describe el motivo del reporte."), StringLength(500, MinimumLength = 10, ErrorMessage = "El motivo debe tener entre 10 y 500 caracteres.")]
    [Display(Name = "Motivo del reporte")]
    public string Motivo { get; set; } = "";

    [Required(ErrorMessage = "Indica cuándo empezó el corte.")]
    [Display(Name = "Inicio del corte")]
    public DateTime? InicioCorte { get; set; }

    public List<LineaZonaVM> Lineas { get; set; } = new();

    [ValidateNever]
    public IEnumerable<SelectListItem> Zonas { get; set; } = new List<SelectListItem>();
}
