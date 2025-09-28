using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UtopiaBS.Entities
{
    [Table("Empleados")]
    public class Empleado
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdEmpleado { get; set; }

        [Required, MaxLength(100)]
        public string Nombre { get; set; }

        [MaxLength(100)]
        public string Puesto { get; set; }

        [MaxLength(150)]
        public string Especialidad { get; set; }

        public virtual ICollection<Cita> Citas { get; set; }
    }
}
