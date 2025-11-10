using System.Data.Entity;
using UtopiaBS.Entities;
using UtopiaBS.Entities.Clientes;
using UtopiaBS.Entities.Contabilidad;

namespace UtopiaBS.Data
{
    public class Context : DbContext
    {
        public Context() : base("name=Contexto")
        {

        }

        public DbSet<Cliente> Clientes { get; set; }

        public DbSet<Producto> Productos { get; set; }

        public DbSet<Cita> Citas { get; set; }

        public DbSet<Empleado> Empleados { get; set; }
        public DbSet<Servicio> Servicios { get; set; }
        public DbSet<Venta> Ventas { get; set; }
        public DbSet<DetalleVentaProducto> DetalleVentaProductos { get; set; }
        public DbSet<DetalleVentaServicio> DetalleVentaServicios { get; set; }

        public DbSet<UtopiaBS.Entities.Contabilidad.Ingreso> Ingresos { get; set; }
        public DbSet<UtopiaBS.Entities.Contabilidad.Egreso> Egresos { get; set; }
        public DbSet<UtopiaBS.Entities.Contabilidad.CierreSemanal> CierresSemanales { get; set; }
        public DbSet<CuponDescuento> CuponDescuento { get; set; }
    }
}
