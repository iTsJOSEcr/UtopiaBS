using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace UtopiaBS.Entities


{
    [Table("Producto")] // Nombre real de la tabla en SQL
    public class Producto
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdProducto { get; set; }

        [Required, MaxLength(150)]
        public string Nombre { get; set; }

        [NotMapped]
        public string Tipo { get; set; }

        [MaxLength(500)]
        public string Descripcion { get; set; }


        [Required, MaxLength(150)]
        public string Proveedor { get; set; }

        [Required]
        public decimal PrecioUnitario { get; set; }

        [Required]
        public int CantidadStock { get; set; }

        [NotMapped]
        public DateTime Fecha { get; set; }

        [NotMapped]
        public int Threshold { get; set; } // Mínimo de unidades permitidas

        [NotMapped] // no se guarda en la BD
        public decimal Precio => PrecioUnitario;


    }
}
