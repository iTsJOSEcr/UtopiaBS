using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UtopiaBS.Entities.Clientes;


namespace UtopiaBS.Entities
{
    [Table("Citas")]
    public class Cita
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdCita { get; set; }

        [Required]
        public DateTime FechaHora { get; set; }

        public int? IdCliente { get; set; }
        public int? IdEmpleado { get; set; }
        public int? IdServicio { get; set; }

        [Required]
        public int IdEstadoCita { get; set; } 

        [MaxLength(500)]
        public string Observaciones { get; set; }

        [ForeignKey("IdCliente")]
        public virtual Cliente Cliente { get; set; }

        [ForeignKey("IdEmpleado")]
        public virtual Empleado Empleado { get; set; }

        [ForeignKey("IdServicio")]
        public virtual Servicio Servicio { get; set; }

        public DateTime? FechaUltimoRecordatorio { get; set; }

        [MaxLength(100)]
        public string RegistradoPor { get; set; }
        public string MotivoCancelacion { get; set; }
        public string CanceladaPor { get; set; }
        public DateTime? FechaCancelacion { get; set; }
    }
}
