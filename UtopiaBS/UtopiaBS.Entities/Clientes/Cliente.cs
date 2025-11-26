using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UtopiaBS.Entities.Clientes
{
    [Table("Clientes")]
    public class Cliente
    {
        [Key]
        public int IdCliente { get; set; }

        [Required, StringLength(150)]
        public string Nombre { get; set; }

        public int? IdTipoMembresia { get; set; }

        [Required, StringLength(128)]
        public string IdUsuario { get; set; }
        public string Cedula { get; set; }
    }
}
