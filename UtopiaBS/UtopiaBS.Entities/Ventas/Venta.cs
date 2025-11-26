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

        public int? IdCliente { get; set; }
        public string NombreCliente { get; set; }
        public string CedulaCliente { get; set; }
        public decimal MontoDescuento { get; set; }
    }
}
