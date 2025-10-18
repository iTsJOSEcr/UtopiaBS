using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UtopiaBS.Entities.Contabilidad
{
    public class ResumenDiarioViewModel
    {
        public DateTime Fecha { get; set; }
        public decimal TotalIngresos { get; set; }
        public decimal TotalEgresos { get; set; }
        public decimal Balance { get; set; }
    }

}
