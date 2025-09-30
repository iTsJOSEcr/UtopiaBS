using System.Web.Mvc;
using UtopiaBS.Business;

namespace UtopiaBS.Web.Controllers
{
    public class CitaController : Controller
    {
        private readonly CitaService _citaService = new CitaService();
        private readonly EmpleadoService _empleadoService = new EmpleadoService();
        private readonly ServicioService _servicioService = new ServicioService();

        // GET: Cita/Listar 
        public ActionResult Listar(int? empleadoId, int? servicioId)
        {
            var citas = _citaService.ListarDisponibles(empleadoId, servicioId);
            ViewBag.Empleados = _empleadoService.ObtenerTodos();
            ViewBag.Servicios = new ServicioService().ObtenerTodos(); 
            ViewBag.EmpleadoSeleccionado = empleadoId;
            ViewBag.ServicioSeleccionado = servicioId;
            return View(citas);
        }


        // POST: Cita/Reservar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Reservar(int idCita, int idCliente, int? idEmpleado, int? idServicio)
        {
            var mensaje = _citaService.ReservarCita(idCita, idCliente, idEmpleado, idServicio);
            TempData["Mensaje"] = mensaje;
            return RedirectToAction("Listar");
        }
    }
}
