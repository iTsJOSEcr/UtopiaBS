using System;
using System.Collections.Generic;

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

        public List<LineaVentaViewModel> Lineas { get; set; }

        // Subtotal antes de descuentos
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

        // Cupón aplicado (código)
        public string CuponAplicado { get; set; }

        // Tipo de cupón ("Porcentaje" o "Monto")
        public string CuponTipo { get; set; }

        // Valor original del cupón (porcentaje o monto)
        public decimal CuponValor { get; set; }

        // Monto de descuento aplicado (valor monetario)
        public decimal Descuento { get; set; } = 0m;

        // Total después de descuento
        public decimal Total
        {
            get
            {
                var total = SubTotal - Descuento;
                if (total < 0) total = 0;
                return total;
            }
        }

        public DateTime FechaCreacion { get; set; }

        public int? IdCliente { get; set; }

        // Aplica cupón a este carrito (calcula Descuento a partir del SubTotal)
        public void AplicarCupon(string codigo, string tipo, decimal valor)
        {
            CuponAplicado = codigo;
            CuponTipo = tipo;
            CuponValor = valor;

            if (string.Equals(tipo, "Porcentaje", StringComparison.OrdinalIgnoreCase))
            {
                Descuento = Math.Round(SubTotal * (valor / 100m), 2);
            }
            else
            {
                Descuento = Math.Round(valor, 2);
                if (Descuento > SubTotal) Descuento = SubTotal;
            }
        }

        // Limpia cupón
        public void LimpiarCupon()
        {
            CuponAplicado = null;
            CuponTipo = null;
            CuponValor = 0m;
            Descuento = 0m;
        }
    }
}
