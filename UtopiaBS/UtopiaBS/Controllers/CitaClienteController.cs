using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using UtopiaBS.Business;
using UtopiaBS.Data;

namespace UtopiaBS.Controllers
{
    [Authorize(Roles = "Cliente")]
    public class CitaClienteController : Controller
    {

        private readonly CitaService _citaService = new CitaService();
        private readonly EmpleadoService _empleadoService = new EmpleadoService();
        private readonly ServicioService _servicioService = new ServicioService();
        private readonly Context _context = new Context();

        // GET: CitaCliente/Listar 
        public ActionResult Listar(int? empleadoId, int? servicioId)
        {
            var citas = _citaService.ListarDisponibles(empleadoId, servicioId);
            ViewBag.Empleados = _empleadoService.ObtenerTodos();
            ViewBag.Servicios = new ServicioService().ObtenerTodos();
            ViewBag.EmpleadoSeleccionado = empleadoId;
            ViewBag.ServicioSeleccionado = servicioId;
            return View(citas);
        }

        // POST: CitaCliente/Reservar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Reservar(int idCita, int idCliente, int? idEmpleado, int? idServicio)
        {
            var resultado = _citaService.ReservarCita(idCita, idCliente, idEmpleado, idServicio);
            TempData["Mensaje"] = resultado;
            return RedirectToAction("Listar");
        }

        public ActionResult MisCitas(int? idCliente)
        {

            var citasPendientes = _context.Citas
                .Include("Empleado")
                .Include("Servicio")
                .Where(c => c.IdEstadoCita == 1)
                .ToList();


            var citasDisponibles = _context.Citas
                .Include("Empleado")
                .Include("Servicio")
                .Where(c => c.IdEstadoCita == 4 || c.IdCliente == null)
                .ToList();


            ViewBag.CitasDisponibles = citasDisponibles;
            ViewBag.IdCliente = idCliente ?? 0;


            return View(citasPendientes);
        }

        // POST: CitaCliente/CambiarCita
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CambiarCita(int idCitaActual, int idNuevaCita, int idCliente)
        {
            var resultado = _citaService.CambiarCita(idCitaActual, idNuevaCita);
            TempData["Mensaje"] = resultado;

            return RedirectToAction("MisCitas", new { idCliente });
        }
    }
}
