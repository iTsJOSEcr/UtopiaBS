using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UtopiaBS.Entities
{
    [Table("DetalleVentaProducto")]
    public class DetalleVentaProducto
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdDetalleProducto { get; set; }

        public int IdVenta { get; set; }

        public int IdProducto { get; set; }

        public int Cantidad { get; set; }

        public decimal PrecioUnitario { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public decimal SubTotal { get; private set; }
    }
}
