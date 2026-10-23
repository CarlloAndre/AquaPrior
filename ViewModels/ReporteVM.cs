using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AquaPrior.ViewModels;

public class ReporteVM
{
    public int Id { get; set; }
    public string? ReturnUrl { get; set; }

    [Required(ErrorMessage = "Elige un distrito.")]
    [Display(Name = "Distrito")]
    public int? ZonaId { get; set; }

    [Required(ErrorMessage = "Indica una dirección o referencia."), StringLength(80, MinimumLength = 4, ErrorMessage = "Usa entre 4 y 80 caracteres.")]
    [Display(Name = "Dirección o referencia")]
    public string Direccion { get; set; } = "";

    [Required(ErrorMessage = "Indica cuándo empezó el corte.")]
    [Display(Name = "Inicio del corte")]
    public DateTime? InicioCorte { get; set; }

    [Range(1, 1000000, ErrorMessage = "Las personas afectadas deben ser un número mayor a 0.")]
    [Display(Name = "Personas afectadas (si lo sabes)")]
    public int? PersonasAfectadas { get; set; }

    [ValidateNever]
    public IEnumerable<SelectListItem> Zonas { get; set; } = new List<SelectListItem>();
}
