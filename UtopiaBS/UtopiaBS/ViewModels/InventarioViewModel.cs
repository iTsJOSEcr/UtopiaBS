using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UtopiaBS.ViewModels
{
    public class InventarioViewModel
    {
        public List<UtopiaBS.Entities.Producto> Productos { get; set; }
        public List<UtopiaBS.Entities.CuponDescuento> Cupones { get; set; }
    }
}