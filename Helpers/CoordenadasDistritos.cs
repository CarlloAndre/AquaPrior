namespace AquaPrior.Helpers
{
    public static class CoordenadasDistritos
    {
        // Centros aproximados: verificar antes de la entrega.
        public static readonly Dictionary<string, (double Lat, double Lng)> Centros = new()
        {
            ["Trujillo"] = (-8.1116, -79.0288),
            ["El Porvenir"] = (-8.0030, -79.0000),
            ["Florencia de Mora"] = (-8.0870, -79.0030),
            ["Huanchaco"] = (-8.0783, -79.1222),
            ["La Esperanza"] = (-8.0830, -79.0500),
            ["Laredo"] = (-8.0800, -78.9600),
            ["Moche"] = (-8.1700, -79.0100),
            ["Poroto"] = (-8.0400, -78.8900),
            ["Salaverry"] = (-8.2253, -78.9808),
            ["Simbal"] = (-7.9900, -78.8200),
            ["Víctor Larco Herrera"] = (-8.1333, -79.0500),
        };
    }
}
