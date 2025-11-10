using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UtopiaBS.Entities
{
    [Table("CuponDescuento")]
    public class CuponDescuento
    {
        [Key]
        public int CuponId { get; set; }

        public string Codigo { get; set; }

        public string Tipo { get; set; }

        public decimal Valor { get; set; }

        public bool Activo { get; set; }

        public DateTime FechaInicio { get; set; }

        public DateTime FechaFin { get; set; }

        public int? UsoMaximo { get; set; }

        public int? UsoActual { get; set; }
    }
}
