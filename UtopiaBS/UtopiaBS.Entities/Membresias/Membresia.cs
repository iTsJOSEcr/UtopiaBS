using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UtopiaBS.Entities
{
    [Table("Membresias")]
    public class Membresia
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdMembresia { get; set; }  

        public int IdCliente { get; set; }
        public int IdTipoMembresia { get; set; }

        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }

        public int PuntosAcumulados { get; set; }
    }
}
