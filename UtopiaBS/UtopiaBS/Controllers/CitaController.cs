using Microsoft.AspNet.Identity;
using System;
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
    [Authorize(Roles = "Administrador,Empleado")]
    public class CitaController : Controller
    {
        private readonly CitaService _citaService = new CitaService();
        private readonly EmpleadoService _empleadoService = new EmpleadoService();
        private readonly ServicioService _servicioService = new ServicioService();

        // GET: Cita/Agregar
        public ActionResult Agregar()
        {
            ViewBag.Empleados = new SelectList(_empleadoService.ObtenerTodos(), "IdEmpleado", "Nombre");
            ViewBag.Servicios = new SelectList(_servicioService.ObtenerTodos(), "IdServicio", "Nombre");
            return View();
        }

        // POST: Cita/Agregar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Agregar(Cita cita)
        {
            var resultado = _citaService.AgendarCita(cita);

            if (resultado != "Cita agendada correctamente.")
            {
                TempData["Error"] = resultado;

                ViewBag.Empleados = new SelectList(_empleadoService.ObtenerTodos(), "IdEmpleado", "Nombre");
                ViewBag.Servicios = new SelectList(_servicioService.ObtenerTodos(), "IdServicio", "Nombre");

                return View(cita);
            }

            TempData["Mensaje"] = resultado;
            return RedirectToAction("Administrar");
        }

        // LISTAR CITAS PENDIENTES (para confirmar / cancelar)
        public ActionResult ListarAgendadas(int? empleadoId, int? servicioId)
        {
            var citas = _citaService.ListarPendientes(empleadoId, servicioId);

            ViewBag.Empleados = new SelectList(_empleadoService.ObtenerTodos(), "IdEmpleado", "Nombre");
            ViewBag.Servicios = new SelectList(_servicioService.ObtenerTodos(), "IdServicio", "Nombre");
            ViewBag.EmpleadoSeleccionado = empleadoId;
            ViewBag.ServicioSeleccionado = servicioId;

            return View(citas);
        }

        // ✅ CONFIRMAR CITA (ADMIN / EMPLEADO) + CORREO AL CLIENTE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ConfirmarCita(int id)
        {
            using (var db = new Context())
            using (var identityDb = new ApplicationDbContext())
            {
                var cita = db.Citas
                    .Include("Cliente")
                    .Include("Servicio")
                    .Include("Empleado")
                    .FirstOrDefault(c => c.IdCita == id);

                if (cita == null)
                {
                    TempData["Error"] = "La cita no existe.";
                    return RedirectToAction("ListarAgendadas");
                }

                cita.IdEstadoCita = 2; // CONFIRMADA
                db.SaveChanges();

                if (cita.Cliente != null && !string.IsNullOrEmpty(cita.Cliente.IdUsuario))
                {
                    var usuario = identityDb.Users.FirstOrDefault(u => u.Id == cita.Cliente.IdUsuario);

                    if (usuario != null)
                    {
                        string mensaje = $@"
                    <h2>✅ Cita Confirmada</h2>
                    <p>Hola <strong>{usuario.UserName}</strong>,</p>
                    <p>Tu cita ha sido confirmada con éxito.</p>
                    <ul>
                        <li><strong>Servicio:</strong> {cita.Servicio?.Nombre}</li>
                        <li><strong>Profesional:</strong> {cita.Empleado?.Nombre}</li>
                        <li><strong>Fecha:</strong> {cita.FechaHora:dd/MM/yyyy}</li>
                        <li><strong>Hora:</strong> {cita.FechaHora:hh\\:mm tt}</li>
                    </ul>
                    <p>Te esperamos en <strong>Utopía Beauty Salon</strong>.</p>";

                        await EmailService.EnviarCorreoAsync(
                            usuario.Email,
                            "✅ Cita Confirmada - Utopía Beauty Salon",
                            mensaje
                        );
                    }
                }
            }

            TempData["Mensaje"] = "Cita confirmada y correo enviado al cliente.";
            return RedirectToAction("ListarAgendadas");
        }

        //  CANCELAR CITA (ADMIN / EMPLEADO) + CORREO AL CLIENTE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CancelarCita(int id)
        {
            using (var db = new Context())
            using (var identityDb = new ApplicationDbContext())
            {
                var cita = db.Citas
                    .Include("Cliente")
                    .Include("Servicio")
                    .Include("Empleado")
                    .FirstOrDefault(c => c.IdCita == id);

                if (cita == null)
                {
                    TempData["Error"] = "La cita no existe.";
                    return RedirectToAction("ListarAgendadas");
                }

                cita.IdEstadoCita = 4; // CANCELADA
                db.SaveChanges();

                if (cita.Cliente != null && !string.IsNullOrEmpty(cita.Cliente.IdUsuario))
                {
                    var usuario = identityDb.Users.FirstOrDefault(u => u.Id == cita.Cliente.IdUsuario);

                    if (usuario != null)
                    {
                        string mensaje = $@"
                        <h2>❌ Cita Cancelada</h2>
                        <p>Hola <strong>{usuario.UserName}</strong>,</p>
                        <p>Tu cita ha sido cancelada por el salón.</p>
                        <ul>
                            <li><strong>Servicio:</strong> {cita.Servicio?.Nombre}</li>
                            <li><strong>Profesional:</strong> {cita.Empleado?.Nombre}</li>
                            <li><strong>Fecha:</strong> {cita.FechaHora:dd/MM/yyyy}</li>
                            <li><strong>Hora:</strong> {cita.FechaHora:hh\\:mm tt}</li>
                        </ul>
                        <p>Si deseas reagendar, puedes hacerlo desde tu cuenta en el sistema.</p>
                        <p><strong>Utopía Beauty Salon</strong></p>";

                            await EmailService.EnviarCorreoAsync(
                            usuario.Email,
                            "Cita Cancelada - Utopía Beauty Salon",
                            mensaje
                        );
                    }
                }
            }

            TempData["Mensaje"] = "Cita cancelada y correo enviado al cliente.";
            return RedirectToAction("ListarAgendadas");
        }

        // GET: Cita/Administrar
        public ActionResult Administrar()
        {
            var citas = _citaService.ListarTodas();
            return View(citas);
        }

        // GET: Cita/Editar/5
        public ActionResult Editar(int id)
        {
            var cita = _citaService.ListarTodas().FirstOrDefault(c => c.IdCita == id);
            if (cita == null) return HttpNotFound();

            ViewBag.Empleados = new SelectList(_empleadoService.ObtenerTodos(), "IdEmpleado", "Nombre", cita.IdEmpleado);
            ViewBag.Servicios = new SelectList(_servicioService.ObtenerTodos(), "IdServicio", "Nombre", cita.IdServicio);
            return View(cita);
        }

        // POST: Cita/Editar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Editar(Cita cita)
        {
            if (ModelState.IsValid)
            {
                using (var db = new Context())
                {
                    var existente = db.Citas.Find(cita.IdCita);
                    if (existente != null)
                    {
                        existente.FechaHora = cita.FechaHora;
                        existente.IdEmpleado = cita.IdEmpleado;
                        existente.IdServicio = cita.IdServicio;
                        existente.IdEstadoCita = cita.IdEstadoCita;
                        existente.Observaciones = cita.Observaciones;
                        db.SaveChanges();
                    }
                }
                TempData["Mensaje"] = "Cita actualizada correctamente.";
                return RedirectToAction("Administrar");
            }

            ViewBag.Empleados = new SelectList(_empleadoService.ObtenerTodos(), "IdEmpleado", "Nombre", cita.IdEmpleado);
            ViewBag.Servicios = new SelectList(_servicioService.ObtenerTodos(), "IdServicio", "Nombre", cita.IdServicio);
            return View(cita);
        }

        // POST: Cita/Eliminar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Eliminar(int id)
        {
            _citaService.EliminarCita(id);
            TempData["Mensaje"] = "Cita eliminada correctamente.";
            return RedirectToAction("Administrar");
        }

        public ActionResult Menu()
        {
            return View();
        }

        public ActionResult MenuEmpleado()
        {
            return View();
        }

        public ActionResult Reportes()
        {
            return View();
        }

        public ActionResult DescargarReporteCitas(DateTime inicio, DateTime fin, string formato, string profesionalNombre = null)
        {
            try
            {
                formato = string.IsNullOrWhiteSpace(formato) ? "pdf" : formato.Trim().ToLower();

                var archivo = _citaService.DescargarReporteCitas(inicio, fin, formato, profesionalNombre);

                if (archivo == null || archivo.Length == 0)
                {
                    TempData["Mensaje"] = "No se encontraron registros.";
                    return RedirectToAction("Reportes");
                }

                string nombreArchivo = (profesionalNombre != null ? $"{profesionalNombre.Replace(" ", "_")}_" : "")
                    + $"Reporte_Citas_{inicio:yyyyMMdd}_{fin:yyyyMMdd}.{(formato == "excel" ? "xlsx" : "pdf")}";

                string mimeType = formato == "excel"
                    ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                    : "application/pdf";

                return File(archivo, mimeType, nombreArchivo);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al generar reporte: " + ex.Message;
                return RedirectToAction("Reportes");
            }
        }

        [HttpGet]
        public ActionResult Estadisticas(DateTime inicio, DateTime fin, string profesionalNombre = null)
        {
            try
            {
                var datos = _citaService.ObtenerEstadisticas(inicio, fin, profesionalNombre);
                return Json(datos, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = "Error al generar estadísticas: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Cliente")]
        public async Task<ActionResult> Cancelar(int idCita, string motivo)
        {
            var userId = User.Identity.GetUserId();

            using (var db = new Context())
            using (var identityDb = new ApplicationDbContext())
            {
                var clienteId = db.Clientes
                    .Where(c => c.IdUsuario == userId)
                    .Select(c => (int?)c.IdCliente)
                    .FirstOrDefault();

                if (clienteId == null)
                {
                    TempData["Error"] = "No se encontró el perfil del cliente.";
                    return RedirectToAction("MisCitas", "CitaCliente");
                }

                // Primero aplicamos la lógica de negocio
                var resultado = _citaService.CancelarPorCliente(idCita, clienteId.Value, motivo);
                TempData[resultado.StartsWith("Cita cancelada") ? "Mensaje" : "Error"] = resultado;

                // Luego buscamos la cita y el usuario para el correo
                var cita = db.Citas
                    .Include("Servicio")
                    .Include("Empleado")
                    .FirstOrDefault(c => c.IdCita == idCita);

                var usuario = identityDb.Users.FirstOrDefault(u => u.Id == userId);

                if (cita != null && usuario != null)
                {
                    string mensaje = $@"
                    <h2>❌ Cita Cancelada</h2>
                    <p>Hola <strong>{usuario.UserName}</strong>,</p>
                    <p>Tu cita ha sido cancelada con el siguiente motivo:</p>
                    <blockquote>{motivo}</blockquote>
                    <ul>
                        <li><strong>Servicio:</strong> {cita.Servicio?.Nombre}</li>
                        <li><strong>Profesional:</strong> {cita.Empleado?.Nombre}</li>
                        <li><strong>Fecha:</strong> {cita.FechaHora:dd/MM/yyyy}</li>
                        <li><strong>Hora:</strong> {cita.FechaHora:hh\\:mm tt}</li>
                    </ul>
                    <p><strong>Utopía Beauty Salon</strong></p>";

                    await EmailService.EnviarCorreoAsync(
                        usuario.Email,
                        "❌ Cita Cancelada - Utopía Beauty Salon",
                        mensaje
                    );
                }
            }

            return RedirectToAction("MisCitas", "CitaCliente");
        }
    }
}
