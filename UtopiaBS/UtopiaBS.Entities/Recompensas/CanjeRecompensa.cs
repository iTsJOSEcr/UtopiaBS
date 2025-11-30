using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UtopiaBS.Entities.Recompensas
{
    [Table("CanjeRecompensa")]
    public class CanjeRecompensa
    {
        [Key]
        public int IdCanje { get; set; }

        [Required]
        public int IdCliente { get; set; }

        [Required]
        public int IdRecompensa { get; set; }

        public int? CuponId { get; set; }

        [Required]
        public DateTime FechaCanje { get; set; }

        [Required]
        public int PuntosUtilizados { get; set; }
    }
}
