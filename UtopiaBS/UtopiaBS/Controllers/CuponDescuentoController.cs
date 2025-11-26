using System;
using System.Linq;
using System.Web.Mvc;
using UtopiaBS.Business;
using UtopiaBS.Data;
using UtopiaBS.Entities;

namespace UtopiaBS.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class CuponDescuentoController : Controller
    {
        private readonly CuponDescuentoService _service = new CuponDescuentoService();

        public ActionResult Index()
        {
            using (var db = new Context())
            {
                var cupones = db.CuponDescuento.ToList();
                return View(cupones);
            }
        }

        // GET: Agregar
        public ActionResult Agregar()
        {
            return View(new CuponDescuento());
        }

        // POST: Agregar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Agregar(CuponDescuento cupon)
        {
            var mensaje = _service.AgregarCupon(cupon);
            TempData["Mensaje"] = mensaje;
            return RedirectToAction("Listar", "Producto");
        }

        // GET: Editar
        public ActionResult Editar(int id)
        {
            using (var db = new Context())
            {
                var cupon = db.CuponDescuento.FirstOrDefault(c => c.CuponId == id);
                if (cupon == null)
                    return HttpNotFound();

                return View(cupon);
            }
        }

        // POST: Editar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Editar(CuponDescuento cupon)
        {
            var mensaje = _service.EditarCupon(cupon);
            TempData["Mensaje"] = mensaje;
            return RedirectToAction("Listar", "Producto");
        }


        // POST: Eliminar
        [HttpPost]
        public ActionResult Eliminar(int id)
        {
            var mensaje = _service.EliminarCupon(id);
            TempData["Mensaje"] = mensaje;
            return RedirectToAction("Listar", "Producto");
        }

        // POST: Cambiar estado
        [HttpPost]
        public ActionResult CambiarEstado(int id)
        {
            var mensaje = _service.CambiarEstado(id);
            TempData["Mensaje"] = mensaje;
            return RedirectToAction("Listar", "Producto");
        }
    }
}
