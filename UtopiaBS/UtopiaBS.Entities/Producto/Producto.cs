using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace UtopiaBS.Entities



{
    [Table("Producto")] 
    public class Producto
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdProducto { get; set; }

        [Required, MaxLength(150)]
        public string Nombre { get; set; }

        [MaxLength(500)]
        public string Descripcion { get; set; }


        [Required, MaxLength(150)]
        public string Proveedor { get; set; }

        [Required]
        public decimal PrecioUnitario { get; set; }

        [Required]
        public int CantidadStock { get; set; }

        [Required]
        public DateTime Fecha { get; set; }

        [Required]
        public int Threshold { get; set; } 

        public int IdEstado { get; set; } = 1;

        public string Tipo { get; set; }

        [NotMapped] 
        public decimal Precio => PrecioUnitario;
        public DateTime? FechaExpiracion { get; set; }
        public int? DiasAnticipacion { get; set; }

    }
}
