using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace UtopiaBS.Entities
{
    [Table("Ventas")]
    public class Venta
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdVenta { get; set; }

        public DateTime FechaVenta { get; set; }

        [Required]
        [MaxLength(128)]
        public string IdUsuario { get; set; }

        public decimal Total { get; set; }

        public int? CuponId { get; set; }

        // Monto monetario que se descontó en esta venta (si aplica)
        // No use "decimal(18,2)" en Column; la precisión se define en OnModelCreating
        public decimal MontoDescuento { get; set; }
    }
}
