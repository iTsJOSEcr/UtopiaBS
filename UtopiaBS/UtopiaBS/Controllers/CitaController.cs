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

        // CONFIRMAR CITA (ADMIN / EMPLEADO) + CORREO AL CLIENTE
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
                        <div style='font-family: Arial, sans-serif; background-color:#f9f9f9; padding:20px;'>
                            <div style='max-width:600px; margin:auto; background-color:#ffffff; border-radius:8px; overflow:hidden; box-shadow:0 0 10px rgba(0,0,0,0.1);'>
        
                                <div style='background-color:#d4a5a5; color:white; padding:20px; text-align:center;'>
                                    <h2 style='margin:0;'>✨ Cita Confirmada ✨</h2>
                                </div>

                                <div style='padding:25px; color:#333;'>
                                    <p style='font-size:16px;'>Hola <strong>{usuario.UserName}</strong>,</p>

                                    <p style='font-size:15px;'>
                                        ¡Tenemos buenas noticias! 💖 Tu cita ha sido <strong>confirmada con éxito</strong>.
                                    </p>

                                    <div style='background-color:#f3f3f3; border-radius:6px; padding:15px; margin:20px 0;'>
                                        <p style='margin:6px 0;'><strong>💇 Servicio:</strong> {cita.Servicio?.Nombre}</p>
                                        <p style='margin:6px 0;'><strong>👩‍🎨 Profesional:</strong> {cita.Empleado?.Nombre}</p>
                                        <p style='margin:6px 0;'><strong>📅 Fecha:</strong> {cita.FechaHora:dd/MM/yyyy}</p>
                                        <p style='margin:6px 0;'><strong>⏰ Hora:</strong> {cita.FechaHora:hh\\:mm tt}</p>
                                    </div>

                                    <p style='font-size:15px;'>
                                        Te esperamos con mucho gusto para brindarte una experiencia única y especial. 🌸
                                    </p>

                                    <p style='margin-top:30px;'>
                                        Con cariño,<br>
                                        <strong>Utopía Beauty Salon</strong>
                                    </p>
                                </div>

                                <div style='background-color:#eeeeee; text-align:center; padding:10px; font-size:12px; color:#777;'>
                                    © {DateTime.Now.Year} Utopía Beauty Salon · Todos los derechos reservados
                                </div>
                            </div>
                        </div>";
                        await EmailService.EnviarCorreoAsync(
                            usuario.Email,
                            "Cita Confirmada - Utopía Beauty Salon",
                            mensaje
                        );
                    }
                }
            }

            TempData["Mensaje"] = "Cita confirmada y correo enviado al cliente.";
            return RedirectToAction("ListarAgendadas");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CancelarCita(int id)
        {
            try
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

                    // Estado 3 = CANCELADA
                    cita.IdEstadoCita = 3;
                    db.SaveChanges();

                    if (cita.Cliente != null && !string.IsNullOrEmpty(cita.Cliente.IdUsuario))
                    {
                        var usuario = identityDb.Users
                            .FirstOrDefault(u => u.Id == cita.Cliente.IdUsuario);

                        if (usuario != null && !string.IsNullOrEmpty(usuario.Email))
                        {
                            string mensaje = $@"
                            <div style='font-family: Arial, sans-serif; background-color:#f9f9f9; padding:20px;'>
                                <div style='max-width:600px; margin:auto; background-color:#ffffff; border-radius:8px; overflow:hidden; box-shadow:0 0 10px rgba(0,0,0,0.1);'>
        
                                    <div style='background-color:#c97a7a; color:white; padding:20px; text-align:center;'>
                                        <h2 style='margin:0;'>❌ Cita Cancelada</h2>
                                    </div>

                                    <div style='padding:25px; color:#333;'>
                                        <p style='font-size:16px;'>Hola <strong>{usuario.UserName}</strong>,</p>

                                        <p style='font-size:15px;'>
                                            Queremos informarte que tu cita ha sido <strong>cancelada</strong>.
                                        </p>

                                        <div style='background-color:#f3f3f3; border-radius:6px; padding:15px; margin:20px 0;'>
                                            <p style='margin:6px 0;'><strong>💇 Servicio:</strong> {cita.Servicio?.Nombre}</p>
                                            <p style='margin:6px 0;'><strong>👩‍🎨 Profesional:</strong> {cita.Empleado?.Nombre}</p>
                                            <p style='margin:6px 0;'><strong>📅 Fecha:</strong> {cita.FechaHora:dd/MM/yyyy}</p>
                                            <p style='margin:6px 0;'><strong>⏰ Hora:</strong> {cita.FechaHora:hh\:mm tt}</p>
                                        </div>

                                        <p style='font-size:15px;'>
                                            Si deseas reprogramar tu cita o tienes alguna consulta, estaremos encantados de ayudarte. 💖
                                        </p>

                                        <p style='margin-top:30px;'>
                                            Con cariño,<br>
                                            <strong>Utopía Beauty Salon</strong>
                                        </p>
                                    </div>

                                    <div style='background-color:#eeeeee; text-align:center; padding:10px; font-size:12px; color:#777;'>
                                        © {DateTime.Now.Year} Utopía Beauty Salon · Todos los derechos reservados
                                    </div>
                                </div>
                            </div>";

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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                TempData["Error"] = "Error al cancelar la cita.";
                return RedirectToAction("ListarAgendadas");
            }
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

                string nombreArchivo = (profesionalNombre != null ? $"{profesionalNombre.Replace(" ", "")}" : "")
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
    }
}