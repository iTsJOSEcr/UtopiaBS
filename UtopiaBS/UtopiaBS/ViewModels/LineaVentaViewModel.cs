namespace UtopiaBS.ViewModels
{
    public class LineaVentaViewModel
    {
        public int IdProducto { get; set; }
        public string NombreProducto { get; set; }
        public decimal PrecioUnitario { get; set; }
        public int Cantidad { get; set; }
        public bool EsServicio { get; set; }

        public int CantidadStock { get; set; }

        public decimal SubTotal => PrecioUnitario * Cantidad;
    }
}