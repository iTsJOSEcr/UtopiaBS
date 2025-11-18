using Microsoft.AspNet.Identity;
using System;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using UtopiaBS.Business;
using UtopiaBS.Data;
using UtopiaBS.Entities;
using UtopiaBS.Models;

namespace UtopiaBS.Web.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class CitaController : Controller
    {
        private readonly CitaService _citaService = new CitaService();
        private readonly EmpleadoService _empleadoService = new EmpleadoService();
        private readonly ServicioService _servicioService = new ServicioService();
        private readonly Context _context = new Context();


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
        public ActionResult ListarAgendadas(int? empleadoId, int? servicioId)
        {
            var citas = _citaService.ListarPendientes(empleadoId, servicioId);
            ViewBag.Empleados = new SelectList(_empleadoService.ObtenerTodos(), "IdEmpleado", "Nombre");
            ViewBag.Servicios = new SelectList(_servicioService.ObtenerTodos(), "IdServicio", "Nombre");
            ViewBag.EmpleadoSeleccionado = empleadoId;
            ViewBag.ServicioSeleccionado = servicioId;
            return View(citas);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmarCita(int id)
        {
            using (var db = new Context())
            {
                var cita = db.Citas.Find(id);
                if (cita != null) cita.IdEstadoCita = 2;
                db.SaveChanges();
            }
            TempData["Mensaje"] = "Cita confirmada.";
            return RedirectToAction("ListarAgendadas");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CancelarCita(int id)
        {
            using (var db = new Context())
            {
                var cita = db.Citas.Find(id);
                if (cita != null) cita.IdEstadoCita = 4;
                db.SaveChanges();
            }
            TempData["Mensaje"] = "Cita cancelada.";
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

        public ActionResult Reportes()
        {
            return View();
        }

        public ActionResult DescargarReporteCitas(DateTime inicio, DateTime fin, string formato, string profesionalNombre = null)
        {
            try
            {
                formato = string.IsNullOrWhiteSpace(formato) ? "pdf" : formato.Trim().ToLower();

                // 👇 ahora el servicio recibe también el profesional si se filtró
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
        public ActionResult Cancelar(int idCita, string motivo)
        {
            var userId = User.Identity.GetUserId();

            using (var db = new Context())
            {
                var clienteId = db.Clientes
                    .Where(c => c.IdUsuario == userId)
                    .Select(c => (int?)c.IdCliente)
                    .FirstOrDefault();

                if (clienteId == null)
                {
                    TempData["Error"] = "No se encontró el perfil del cliente.";
                    return RedirectToAction("MisCitas");
                }

                var resultado = _citaService.CancelarPorCliente(idCita, clienteId.Value, motivo);

                TempData[resultado.StartsWith("Cita cancelada") ? "Mensaje" : "Error"] = resultado;

                return RedirectToAction("MisCitas");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CancelarPorAdmin(int idCita, string motivo)
        {
            var resultado = _citaService.CancelarPorAdmin(idCita, motivo);
            TempData["Mensaje"] = resultado;

            return RedirectToAction("Administrar");
        }


    }
}