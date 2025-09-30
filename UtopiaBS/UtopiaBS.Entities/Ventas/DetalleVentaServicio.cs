using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UtopiaBS.Entities
{
    [Table("DetalleVentaServicio")]
    public class DetalleVentaServicio
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdDetalleServicio { get; set; }

        [Required]
        public int IdVenta { get; set; }

        [Required]
        public int IdServicio { get; set; }

        [Required]
        public int Cantidad { get; set; }

        public decimal PrecioUnitario { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public decimal SubTotal { get; private set; }
    }
}
