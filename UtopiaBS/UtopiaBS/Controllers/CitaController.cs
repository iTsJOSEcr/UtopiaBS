using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using UtopiaBS.Business;
using UtopiaBS.Data;
using UtopiaBS.Entities;

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
            try
            {
                if (ModelState.IsValid)
                {

                    cita.IdEstadoCita = 4;
                    _citaService.AgregarCita(cita);

                    TempData["Mensaje"] = "Cita agregada correctamente.";
                    return RedirectToAction("Administrar");
                }


                ViewBag.Empleados = new SelectList(_empleadoService.ObtenerTodos(), "IdEmpleado", "Nombre");
                ViewBag.Servicios = new SelectList(_servicioService.ObtenerTodos(), "IdServicio", "Nombre");
                return View(cita);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al agregar la cita: " + ex.Message;
                ViewBag.Empleados = new SelectList(_empleadoService.ObtenerTodos(), "IdEmpleado", "Nombre");
                ViewBag.Servicios = new SelectList(_servicioService.ObtenerTodos(), "IdServicio", "Nombre");
                return View(cita);
            }
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
    }
}