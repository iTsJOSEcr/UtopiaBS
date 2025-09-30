using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using UtopiaBS.Entities;
using UtopiaBS.Data;

namespace UtopiaBS.Business
{
    public class CitaService
    {
        public List<Cita> ListarDisponibles(int? empleadoId = null, int? servicioId = null)
        {
            using (var db = new Context())
            {
                var query = db.Citas
                              .Include(c => c.Empleado)
                              .Include(c => c.Servicio)
                              .Where(c => c.IdEstadoCita == 1) // disponibles
                              .AsQueryable();

                if (empleadoId.HasValue)
                    query = query.Where(c => c.IdEmpleado == empleadoId.Value);

                if (servicioId.HasValue)
                    query = query.Where(c => c.IdServicio == servicioId.Value);

                return query.OrderBy(c => c.FechaHora).ToList();
            }
        }
        public string ReservarCita(int idCita, int idCliente, int? idEmpleado, int? idServicio)
        {
            try
            {
                using (var db = new Context())
                {
                    var cita = db.Citas.Find(idCita);
                    if (cita == null)
                        return "No se encontró la cita.";

                    if (cita.IdEstadoCita != 1)
                        return "La cita ya no está disponible.";

                    cita.IdCliente = idCliente;
                    cita.IdEmpleado = idEmpleado;
                    cita.IdServicio = idServicio;
                    cita.IdEstadoCita = 2; 

                    db.SaveChanges();
                    return "Cita reservada exitosamente.";
                }
            }
            catch (Exception ex)
            {
                return $"Error al reservar la cita: {ex.Message}";
            }
        }
    }
}
