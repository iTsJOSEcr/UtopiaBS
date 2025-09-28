using System.Collections.Generic;
using System.Linq;
using UtopiaBS.Data;
using UtopiaBS.Entities;

namespace UtopiaBS.Business
{
    public class EmpleadoService
    {
        public List<Empleado> ObtenerTodos()
        {
            using (var db = new Context())
            {
                return db.Empleados.OrderBy(e => e.Nombre).ToList();
            }
        }
    }
}