using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UtopiaBS.Entities; 


namespace UtopiaBS.Data
{
    public class Context : DbContext
    {
        // El nombre "Contexto" debe coincidir con el connectionString en App.config
        public Context() : base("name=Contexto")
        {
        }

        public DbSet<Producto> Productos { get; set; }

        // Si después agregas más entidades, aquí vas creando más DbSet<Entidad>
    }
}
