using System;
using System.Linq;
using System.Collections.Generic;
using UtopiaBS.Data;
using UtopiaBS.Entities;

namespace UtopiaBS.Services
{
    public class ReporteUsuariosActivosService
    {
        public List<UsuarioActividad> ObtenerReporte(DateTime fechaInicio, DateTime fechaFin)
        {
            using (var db = new Context())
            {
                return db.UsuarioActividad
                    .Where(a => a.FechaInicio >= fechaInicio && a.FechaInicio <= fechaFin)
                    .OrderBy(a => a.FechaInicio)
                    .ToList();
            }
        }
    }
}
