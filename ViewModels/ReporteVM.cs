using System.ComponentModel.DataAnnotations;

namespace AquaPrior.ViewModels;

/// <summary>Formulario del reporte: dirección, ubicación exacta, inicio del corte y posibles personas afectadas. El distrito se deduce de las coordenadas.</summary>
public class ReporteVM
{
    public int Id { get; set; }
    public string? ReturnUrl { get; set; }

    [Required(ErrorMessage = "Indica una dirección o referencia."), StringLength(80, MinimumLength = 4, ErrorMessage = "Usa entre 4 y 80 caracteres.")]
    [Display(Name = "Dirección o referencia")]
    public string Direccion { get; set; } = "";

    [Required(ErrorMessage = "Marca la ubicación exacta en el mapa.")]
    [Range(-90, 90, ErrorMessage = "La latitud no es válida.")]
    public double? Latitud { get; set; }

    [Required(ErrorMessage = "Marca la ubicación exacta en el mapa.")]
    [Range(-180, 180, ErrorMessage = "La longitud no es válida.")]
    public double? Longitud { get; set; }

    [Required(ErrorMessage = "Describe el motivo del reporte."), StringLength(500, MinimumLength = 10, ErrorMessage = "El motivo debe tener entre 10 y 500 caracteres.")]
    [Display(Name = "Motivo del reporte")]
    public string Motivo { get; set; } = "";

    [Required(ErrorMessage = "Indica cuándo empezó el corte.")]
    [Display(Name = "Inicio del corte")]
    public DateTime? InicioCorte { get; set; }

    [Required(ErrorMessage = "Indica cuántas personas podrían estar afectadas.")]
    [Range(1, 1000000, ErrorMessage = "Las personas afectadas deben ser un número mayor a 0.")]
    [Display(Name = "Posibles personas afectadas")]
    public int? PersonasAfectadas { get; set; }
}
