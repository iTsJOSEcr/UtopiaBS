using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UtopiaBS.Entities
{
    [Table("TipoMembresia")]
    public class TipoMembresia
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdTipoMembresia { get; set; }

        [Required]
        [StringLength(50)]
        public string NombreTipo { get; set; }

        [StringLength(200)]
        public string Descripcion { get; set; }

        public decimal? Descuento { get; set; }

        [Required]
        public decimal Costo { get; set; }
    }
}
