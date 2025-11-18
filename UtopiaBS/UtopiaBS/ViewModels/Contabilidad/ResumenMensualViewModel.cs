using System;
using System.Collections.Generic;

namespace UtopiaBS.Entities.Contabilidad
{
    public class ResumenMensualDto
    {
        // ya existentes
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal TotalIngresos { get; set; }     // Ingresos manuales
        public decimal TotalEgresos { get; set; }
        public decimal Balance => TotalIngresos - TotalEgresos;

        // ... tus listas: IngresosPorDia, EgresosPorDia, etc.

        // NUEVO: ventas por tipo
        public decimal TotalVentasProductos { get; set; }
        public decimal TotalVentasServicios { get; set; }
        public decimal TotalVentas => TotalVentasProductos + TotalVentasServicios;

        // NUEVO: qué filtro se está usando ("todo"|"productos"|"servicios")
        public string Filtro { get; set; } = "todo";

        // NUEVO: totales mostrados según filtro (para la UI)
        public decimal TotalIngresosMostrados
        {
            get
            {
                switch ((Filtro ?? "todo").ToLower())
                {
                    case "productos": return TotalVentasProductos;
                    case "servicios": return TotalVentasServicios;
                    case "todo": default: return TotalVentas; // ventas totales
                }
            }
        }
        public decimal BalanceMostrado => TotalIngresosMostrados - TotalEgresos;
    }
}
