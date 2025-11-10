using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UtopiaBS.Entities.Contabilidad
{
    [Table("Ingresos")]
    public class Ingreso
    {
        [Key]
        public int IdIngreso { get; set; }

        [Required]
        public decimal Monto { get; set; }

        [Required]
        public DateTime Fecha { get; set; }

        [Required, MaxLength(150)]
        public string Categoria { get; set; }

        [Required, MaxLength(500)]
        public string Descripcion { get; set; }

        public string UsuarioId { get; set; }

        public DateTime FechaCreacion { get; set; }
    }
}