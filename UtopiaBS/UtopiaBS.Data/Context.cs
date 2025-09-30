using System.Data.Entity;
using UtopiaBS.Entities;


namespace UtopiaBS.Data
{
    public class Context : DbContext
    {
        public Context() : base("name=Contexto")
        {

        }

        public DbSet<Producto> Productos { get; set; }

        public DbSet<Cita> Citas { get; set; }

        public DbSet<Empleado> Empleados { get; set; }
        public DbSet<Servicio> Servicios { get; set; }
        public DbSet<Venta> Ventas { get; set; }
        public DbSet<DetalleVentaProducto> DetalleVentaProductos { get; set; }
        public DbSet<DetalleVentaServicio> DetalleVentaServicios { get; set; }

    }
}
