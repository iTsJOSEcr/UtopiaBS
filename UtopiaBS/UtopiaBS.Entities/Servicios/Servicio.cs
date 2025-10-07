using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UtopiaBS.Entities
{
    [Table("Servicios")]
    public class Servicio
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdServicio { get; set; }

        [Required, MaxLength(150)]
        public string Nombre { get; set; }

        [MaxLength(300)]
        public string Descripcion { get; set; }

        public decimal Precio { get; set; }

        public int IdEstado { get; set; }


        // Navegación
        public virtual ICollection<Cita> Citas { get; set; }
    }
}