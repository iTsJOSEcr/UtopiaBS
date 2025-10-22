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
                              .Where(c => c.IdEstadoCita == 4)
                              .AsQueryable();

                if (empleadoId.HasValue)
                    query = query.Where(c => c.IdEmpleado == empleadoId.Value);

                if (servicioId.HasValue)
                    query = query.Where(c => c.IdServicio == servicioId.Value);

                return query.OrderBy(c => c.FechaHora).ToList();
            }
        }

        public List<Cita> ListarPendientes(int? empleadoId = null, int? servicioId = null)
        {
            using (var db = new Context())
            {
                var query = db.Citas
                              .Include(c => c.Empleado)
                              .Include(c => c.Servicio)
                              .Where(c => c.IdEstadoCita == 1)
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


                    if (cita.IdEstadoCita != 4 && cita.IdEstadoCita != 1)
                        return "La cita ya no está disponible.";

                    cita.IdCliente = idCliente;
                    cita.IdEmpleado = idEmpleado;
                    cita.IdServicio = idServicio;
                    cita.IdEstadoCita = 1;

                    db.SaveChanges();
                    return "Cita reservada exitosamente. Pendiente de Confirmación";
                }
            }
            catch (Exception ex)
            {
                return $"Error al reservar la cita: {ex.Message}";
            }
        }
        public string AgregarCita(Cita nuevaCita)
        {
            try
            {
                using (var db = new Context())
                {
                    db.Citas.Add(nuevaCita);
                    db.SaveChanges();
                    return "Cita agregada correctamente.";
                }
            }
            catch (Exception ex)
            {
                return $"Error al agregar la cita: {ex.Message}";
            }
        }


        public string ConfirmarCita(int idCita)
        {
            using (var db = new Context())
            {
                var cita = db.Citas.Find(idCita);
                if (cita == null) return "Cita no encontrada.";

                cita.IdEstadoCita = 3;
                cita.FechaUltimoRecordatorio = DateTime.Now;
                db.SaveChanges();
                return "Cita confirmada correctamente.";
            }
        }

        public string CancelarCita(int idCita)
        {
            using (var db = new Context())
            {
                var cita = db.Citas.Find(idCita);
                if (cita == null) return "Cita no encontrada.";

                cita.IdEstadoCita = 4;
                cita.FechaUltimoRecordatorio = DateTime.Now;
                db.SaveChanges();
                return "Cita cancelada correctamente.";
            }
        }

        public List<Cita> ListarTodas(int? empleadoId = null, int? servicioId = null)
        {
            using (var db = new Context())
            {
                var query = db.Citas
                              .Include(c => c.Empleado)
                              .Include(c => c.Servicio)
                              .AsQueryable();

                if (empleadoId.HasValue)
                    query = query.Where(c => c.IdEmpleado == empleadoId.Value);

                if (servicioId.HasValue)
                    query = query.Where(c => c.IdServicio == servicioId.Value);

                return query.OrderBy(c => c.FechaHora).ToList();
            }
        }

        public string CambiarCita(int idCitaActual, int idNuevaCita)
        {
            using (var db = new Context())
            {
                var citaActual = db.Citas.Find(idCitaActual);
                var nuevaCita = db.Citas.Find(idNuevaCita);

                if (citaActual == null || nuevaCita == null)
                    return "No se encontraron las citas especificadas.";

                if (nuevaCita.IdEstadoCita != 4)
                    return "La nueva cita ya no está disponible.";


                int? clienteId = citaActual.IdCliente;


                citaActual.IdCliente = null;
                citaActual.IdEstadoCita = 4;


                nuevaCita.IdCliente = clienteId;
                nuevaCita.IdEstadoCita = 1;

                db.SaveChanges();
                return "Cita cambiada exitosamente.";
            }
        }

        public string EliminarCita(int idCita)
        {
            using (var db = new Context())
            {
                var cita = db.Citas.Find(idCita);
                if (cita == null) return "Cita no encontrada.";

                db.Citas.Remove(cita);
                db.SaveChanges();
                return "Cita eliminada correctamente.";
            }
        }

    }
}