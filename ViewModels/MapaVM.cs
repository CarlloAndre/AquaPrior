namespace AquaPrior.ViewModels
{
    public class MapaVM
    {
        public string Nombre { get; set; } = "";
        public int Total { get; set; }
        public int Verificados { get; set; }
        public int Recibidos { get; set; }
        public double? Lat { get; set; }
        public double? Lng { get; set; }
    }
}
