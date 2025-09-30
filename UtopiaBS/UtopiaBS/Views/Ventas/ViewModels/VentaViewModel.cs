using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace UtopiaBS.Web.ViewModels
{
    // Representa el carrito/venta en proceso
    public class VentaViewModel
    {
        public VentaViewModel()
        {
            Lineas = new List<LineaVentaViewModel>();
            FechaCreacion = DateTime.Now;
        }

        // Lista de líneas que conforman la venta (temporal, en memoria / session)
        public List<LineaVentaViewModel> Lineas { get; set; }

        // Subtotal calculado: suma de SubTotal de cada linea
        public decimal SubTotal
        {
            get
            {
                decimal s = 0;
                foreach (var l in Lineas)
                    s += l.SubTotal;
                return s;
            }
        }

        // Aquí podrías agregar propiedades para descuentos, impuestos, etc.
        // Por ahora dejamos Total == SubTotal (ya que no pediste impuestos ni movimientos)
        public decimal Total => SubTotal;

        // Fecha de creación del carrito (solo informativa)
        public DateTime FechaCreacion { get; set; }

        // Si en UI vamos a asociar cliente, aquí podríamos tener IdCliente (opcional)
        public int? IdCliente { get; set; }
    }
}
