using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UtopiaBS.Entities
{
    [Table("PuntosCliente")]
    public class PuntosCliente
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdPuntosCliente { get; set; }  

        public int IdCliente { get; set; }
        public int IdVenta { get; set; }

        public int Puntos { get; set; }
        public DateTime FechaRegistro { get; set; }
    }
}
