using System.ComponentModel.DataAnnotations;

namespace UtopiaBS.Web.ViewModels
{
    public class LineaVentaViewModel
    {
        public int IdProducto { get; set; }

        public string NombreProducto { get; set; }
        public bool EsServicio { get; set; } = false;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0")]
        public decimal PrecioUnitario { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser al menos 1")]
        public int Cantidad { get; set; }

        public decimal SubTotal => PrecioUnitario * Cantidad;
    }
}
