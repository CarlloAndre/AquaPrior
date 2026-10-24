namespace AquaPrior.Models;

/// <summary>Dato simple que la capa de presentación entrega al negocio: un distrito afectado y sus personas.</summary>
public record LineaZona(int ZonaId, int? Personas);
