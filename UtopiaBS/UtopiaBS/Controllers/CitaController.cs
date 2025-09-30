using System.Web.Mvc;
using UtopiaBS.Business;

namespace UtopiaBS.Web.Controllers
{
    public class CitaController : Controller
    {
        private readonly CitaService _citaService = new CitaService();
        private readonly EmpleadoService _empleadoService = new EmpleadoService();

        // GET: Cita/Listar 
        public ActionResult Listar(int? empleadoId)
        {
            var citas = _citaService.ListarDisponibles(empleadoId);
            ViewBag.Empleados = _empleadoService.ObtenerTodos();
            ViewBag.EmpleadoSeleccionado = empleadoId;
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
