using System;
using System.Collections.Generic;

namespace UtopiaBS.Entities.Contabilidad
{
    public class ResumenMensualDto
    {
        public int Year { get; set; }
        public int Month { get; set; }

        // Ingresos/Egresos manuales
        public decimal TotalIngresos { get; set; }
        public decimal TotalEgresos { get; set; }
        public decimal Balance => TotalIngresos - TotalEgresos;

        public List<ItemDiaDto> IngresosPorDia { get; set; } = new List<ItemDiaDto>();
        public List<ItemDiaDto> EgresosPorDia { get; set; } = new List<ItemDiaDto>();

        public List<ItemCategoriaDto> IngresosPorCategoria { get; set; } = new List<ItemCategoriaDto>();
        public List<ItemCategoriaDto> EgresosPorTipo { get; set; } = new List<ItemCategoriaDto>();

        // Ventas por tipo (para filtro)
        public decimal TotalVentasProductos { get; set; }
        public decimal TotalVentasServicios { get; set; }

        // "todo" | "productos" | "servicios"
        public string Filtro { get; set; } = "todo";
    }

    public class ItemDiaDto
    {
        public DateTime Dia { get; set; }
        public decimal Monto { get; set; }
    }

    public class ItemCategoriaDto
    {
        public string Nombre { get; set; }
        public decimal Monto { get; set; }
    }
}
