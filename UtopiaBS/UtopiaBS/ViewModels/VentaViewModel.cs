using System.Collections.Generic;
using System.Linq;

namespace UtopiaBS.ViewModels
{
    public class VentaViewModel
    {
        public List<LineaVentaViewModel> Lineas { get; set; }

        public decimal SubTotal => Lineas.Sum(x => x.SubTotal);
        public decimal Descuento { get; set; }
        public decimal Total => SubTotal - Descuento;

        public string CuponAplicado { get; set; }

        // CLIENTE
        public int? IdCliente { get; set; }
        public string NombreCliente { get; set; }
        public string CedulaCliente { get; set; }

        public VentaViewModel()
        {
            Lineas = new List<LineaVentaViewModel>();
        }

        public void AplicarCupon(string codigo, string tipo, decimal valor)
        {
            CuponAplicado = codigo;

            if (tipo == "Porcentaje")
                Descuento = SubTotal * (valor / 100m);
            else
                Descuento = valor;
        }

        public void LimpiarCupon()
        {
            CuponAplicado = null;
            Descuento = 0;
        }
    }
}