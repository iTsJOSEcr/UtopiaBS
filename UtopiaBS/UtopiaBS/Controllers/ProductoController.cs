  using System;
using System.Linq;
using System.Web.Mvc;
using UtopiaBS.Business;
using UtopiaBS.Entities;
using UtopiaBS.Data;

namespace UtopiaBS.Web.Controllers
{
    public class ProductoController : Controller
    {
        private readonly ProductoService service = new ProductoService();

        // GET: Producto/Agregar
        public ActionResult Agregar()
        { 
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Agregar(Producto nuevo)
        {
            if (ModelState.IsValid)
            {
                nuevo.Fecha = DateTime.Now;

                nuevo.Threshold = (nuevo.CantidadStock > 0) ? 0 : 1; 

                string resultado = service.AgregarProducto(nuevo);
                ViewBag.Mensaje = resultado;

                if (resultado.Contains("exitosamente"))
                {
                    ModelState.Clear();
                    return View();
                }
            }
            return View(nuevo);
        }


        // GET: Producto/Listar
        public ActionResult Listar()
        {
            using (var db = new Context())
            {
                var productos = db.Productos.ToList();
                return View(productos);
            }
        }


        // GET: Producto/Editar/5
        public ActionResult Editar(int id)
        {
            using (var db = new Context())
            {
                var producto = db.Productos.Find(id);
                if (producto == null)
                    return RedirectToAction("Listar"); 

                return View(producto);
            }
        }

        // POST: Producto/Editar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Editar(Producto producto)
        {
            if (ModelState.IsValid)
            {
                string resultado = service.EditarProducto(producto);
                ViewBag.Mensaje = resultado;

                if (resultado.Contains("exitosamente"))
                    return RedirectToAction("Listar"); 
            }
            return View(producto);
        }

        // GET: Producto/Eliminar/5
        public ActionResult Eliminar(int id)
        {
            string resultado = service.EliminarProducto(id);
            TempData["Mensaje"] = resultado; 
            return RedirectToAction("Listar");
        }


    }
}
