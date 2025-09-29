using System.ComponentModel.DataAnnotations;

namespace UtopiaBS.Web.ViewModels
{
    // Representa una línea del carrito: 1 producto + su cantidad
    public class LineaVentaViewModel
    {
        // Id del producto en la BD (Producto.IdProducto)
        public int IdProducto { get; set; }

        // Nombre útil para mostrar en la UI (Producto.Nombre)
        public string NombreProducto { get; set; }

        // Precio unitario actual del producto
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0")]
        public decimal PrecioUnitario { get; set; }

        // Cantidad que se quiere vender (input editable por el usuario)
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser al menos 1")]
        public int Cantidad { get; set; }

        // Subtotal calculado en la UI o en servidor: Cantidad * PrecioUnitario
        public decimal SubTotal => PrecioUnitario * Cantidad;
    }
}
