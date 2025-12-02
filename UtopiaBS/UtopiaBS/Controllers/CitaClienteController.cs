using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using UtopiaBS.Business;
using UtopiaBS.Business.Services;
using UtopiaBS.Data;
using UtopiaBS.Entities;

namespace UtopiaBS.Controllers
{
    [Authorize(Roles = "Cliente")]
    public class CitaClienteController : Controller
    {
        private readonly CitaService _citaService = new CitaService();
        private readonly EmpleadoService _empleadoService = new EmpleadoService();
        private readonly ServicioService _servicioService = new ServicioService();

        // GET: CitaCliente/Listar 
        [HttpGet]
        public ActionResult Listar(int? empleadoId, int? servicioId)
        {
            var citas = _citaService.ListarDisponibles(empleadoId, servicioId);
            ViewBag.Empleados = _empleadoService.ObtenerTodos();
            ViewBag.Servicios = _servicioService.ObtenerTodos();
            ViewBag.EmpleadoSeleccionado = empleadoId;
            ViewBag.ServicioSeleccionado = servicioId;
            return View(citas);
        }

        // POST: CitaCliente/Reservar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Reservar(int idCita, int? idEmpleado, int? idServicio)
        {
            try
            {
                var userId = User.Identity.GetUserId(); // AspNetUsers.Id

                int? clienteId;
                using (var db = new Context())
                {
                    clienteId = db.Clientes
                                  .Where(c => c.IdUsuario == userId)
                                  .Select(c => (int?)c.IdCliente)
                                  .FirstOrDefault();
                }

                if (clienteId == null)
                {
                    TempData["Error"] = "No se encontró el perfil de cliente asociado a tu cuenta.";
                    return RedirectToAction("Listar");
                }

                var resultado = _citaService.ReservarCita(idCita, clienteId.Value, idEmpleado, idServicio);
                TempData["Mensaje"] = resultado;
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al reservar la cita: " + ex.Message;
            }

            return RedirectToAction("Listar");
        }

        [HttpGet]
        public ActionResult MisCitas()
        {
            try
            {
                var userId = User.Identity.GetUserId();
                int? clienteId;

                using (var db = new Context())
                {
                    clienteId = db.Clientes
                                  .Where(c => c.IdUsuario == userId)
                                  .Select(c => (int?)c.IdCliente)
                                  .FirstOrDefault();

                    if (clienteId == null)
                    {
                        TempData["Error"] = "No se encontró el perfil de cliente.";
                        return RedirectToAction("Listar");
                    }
                    var citasPendientes = db.Citas
                        .Include("Empleado")
                        .Include("Servicio")
                        .Where(c =>
                            (c.IdEstadoCita == 1 || c.IdEstadoCita == 2) &&  
                            c.IdCliente == clienteId.Value
                        )
                        .OrderBy(c => c.FechaHora)
                        .ToList();

                    var citasDisponibles = db.Citas
                        .Include("Empleado")
                        .Include("Servicio")
                        .Where(c => c.IdEstadoCita == 4 || c.IdCliente == null)
                        .ToList();

                    ViewBag.CitasDisponibles = citasDisponibles;
                    ViewBag.IdCliente = clienteId.Value;

                    return View(citasPendientes);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar tus citas: " + ex.Message;
                return RedirectToAction("Listar");
            }
        }

        // POST: CitaCliente/CambiarCita
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CambiarCita(int idCitaActual, int idNuevaCita)
        {
            try
            {
                var resultado = _citaService.CambiarCita(idCitaActual, idNuevaCita);
                TempData["Mensaje"] = resultado;
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cambiar la cita: " + ex.Message;
            }

            return RedirectToAction("MisCitas");
        }

        [HttpGet]
        public ActionResult Historial()
        {
            try
            {
                var userId = User.Identity.GetUserId();

                using (var db = new Context())
                {
                    var clienteId = db.Clientes
                        .Where(c => c.IdUsuario.Trim().ToLower() == userId.Trim().ToLower())
                        .Select(c => (int?)c.IdCliente)
                        .FirstOrDefault();

                    if (clienteId == null)
                    {
                        TempData["Error"] = "No se encontró el perfil de cliente.";
                        return RedirectToAction("Listar");
                    }

                    var historial = db.Citas
                        .Include("Empleado")
                        .Include("Servicio")
                        .Where(c => c.IdCliente == clienteId.Value)
                        .OrderByDescending(c => c.FechaHora)
                        .ToList();

                    if (!historial.Any())
                    {
                        TempData["Info"] = "No hay registros disponibles en tu historial de citas.";
                        return View(new List<Cita>());
                    }

                    return View(historial);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar historial: " + ex.Message;
                return RedirectToAction("Listar");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CancelarCliente(int idCita, string motivo)
        {
            var userId = User.Identity.GetUserId();

            using (var db = new Context())
            using (var identityDb = new ApplicationDbContext())
            {
                var cliente = db.Clientes.FirstOrDefault(c => c.IdUsuario == userId);
                if (cliente == null)
                {
                    TempData["Error"] = "No se encontró el perfil del cliente.";
                    return RedirectToAction("MisCitas");
                }

                // Cancelamos con la lógica de negocio
                var service = new CitaService();
                var resultado = service.CancelarPorCliente(idCita, cliente.IdCliente, motivo);
                TempData["Mensaje"] = resultado;

                // Buscamos la cita para el correo
                var cita = db.Citas
                    .Include("Servicio")
                    .Include("Empleado")
                    .FirstOrDefault(c => c.IdCita == idCita);

                //  Buscamos el usuario del cliente en Identity
                var usuario = identityDb.Users.FirstOrDefault(u => u.Id == userId);

                // Envío de correo
                if (cita != null && usuario != null)
                {
                    string mensaje = $@"
                        <h2> Cita Cancelada</h2>
                        <p>Hola <strong>{usuario.UserName}</strong>,</p>

                        <p>Tu cita ha sido cancelada con el siguiente motivo:</p>
                        <blockquote>{motivo}</blockquote>

                        <ul>
                            <li><strong>Servicio:</strong> {cita.Servicio?.Nombre}</li>
                            <li><strong>Profesional:</strong> {cita.Empleado?.Nombre}</li>
                            <li><strong>Fecha:</strong> {cita.FechaHora:dd/MM/yyyy}</li>
                            <li><strong>Hora:</strong> {cita.FechaHora:hh\\:mm tt}</li>
                        </ul>

                        <p><strong>Utopía Beauty Salon</strong></p>
                        ";

                    await EmailService.EnviarCorreoAsync(
                        usuario.Email,
                        "❌ Cita Cancelada - Utopía Beauty Salon",
                        mensaje
                    );
                }
            }

            return RedirectToAction("MisCitas");
        }

    }
}
