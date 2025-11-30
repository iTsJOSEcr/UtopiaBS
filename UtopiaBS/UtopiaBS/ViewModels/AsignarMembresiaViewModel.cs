using System;
using System.ComponentModel.DataAnnotations;

namespace UtopiaBS.ViewModels
{
    public class AsignarMembresiaViewModel
    {
        [Required]
        public int IdCliente { get; set; }

        [Required]
        public int IdTipoMembresia { get; set; }

        [Required]
        public DateTime FechaInicio { get; set; }

        public DateTime? FechaFin { get; set; }
    }
}
