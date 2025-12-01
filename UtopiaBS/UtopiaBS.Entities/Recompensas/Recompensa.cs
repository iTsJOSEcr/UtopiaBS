using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UtopiaBS.Entities.Recompensas
{
        [Table("Recompensas")]
        public class Recompensa
        {
            [Key]
            public int IdRecompensa { get; set; }

            [Required]
            [StringLength(100)]
            public string Nombre { get; set; }

            [Required]
            [StringLength(20)]
            public string Tipo { get; set; } // Cupón | Servicio | Producto

            [Required]
            public int PuntosNecesarios { get; set; }

            [Required]
            public decimal Valor { get; set; }

            [Required]
            public bool Activa { get; set; }
        }

    }