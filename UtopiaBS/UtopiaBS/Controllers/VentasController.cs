using System;
using System.Data.Entity.SqlServer;
using System.Linq;
using System.Web.Mvc;
using UtopiaBS.Data;
using UtopiaBS.Entities;
using UtopiaBS.Web.ViewModels;

namespace UtopiaBS.Web.Controllers
{
    public class VentasController : Controller
    {
        // GET: Ventas/PuntoVenta
        public ActionResult PuntoVenta()
        {
            var carrito = Session["Carrito"] as VentaViewModel ?? new VentaViewModel();
            return View(carrito);
        }

        // Buscar productos por nombre o Id
        public JsonResult BuscarProducto(string termino)
        {
            using (var db = new Context())
            {
                var productos = db.Productos
                    .Where(p => p.Nombre.Contains(termino) || SqlFunctions.StringConvert((double)p.IdProducto).Contains(termino))
                    .Select(p => new
                    {
                        p.IdProducto,
                        p.Nombre,
                        p.PrecioUnitario,
                        p.CantidadStock
                    })
                    .ToList();

                return Json(productos, JsonRequestBehavior.AllowGet);
            }
        }

        // Agregar producto al carrito
        [HttpPost]
        public ActionResult AgregarAlCarrito(int idProducto, string nombre, decimal precioUnitario)
        {
            var carrito = Session["Carrito"] as VentaViewModel ?? new VentaViewModel();

            var linea = carrito.Lineas.FirstOrDefault(x => x.IdProducto == idProducto);
            if (linea != null)
                linea.Cantidad++;
            else
                carrito.Lineas.Add(new LineaVentaViewModel
                {
                    IdProducto = idProducto,
                    NombreProducto = nombre,
                    PrecioUnitario = precioUnitario,
                    Cantidad = 1
                });

            Session["Carrito"] = carrito;

            return Json(new { success = true });
        }

        // Actualizar cantidad o eliminar 
        [HttpPost]
        public ActionResult ActualizarLinea(int idProducto, int cantidad)
        {
            var carrito = Session["Carrito"] as VentaViewModel;
            if (carrito == null) return new HttpStatusCodeResult(400);

            var linea = carrito.Lineas.FirstOrDefault(x => x.IdProducto == idProducto);
            if (linea != null)
            {
                if (cantidad <= 0)
                    carrito.Lineas.Remove(linea);
                else
                    linea.Cantidad = cantidad;
            }

            Session["Carrito"] = carrito;
            return Json(new { success = true });
        }

        // Eliminar línea
        [HttpPost]
        public ActionResult EliminarLinea(int idProducto)
        {
            var carrito = Session["Carrito"] as VentaViewModel;
            if (carrito == null) return new HttpStatusCodeResult(400);

            var linea = carrito.Lineas.FirstOrDefault(x => x.IdProducto == idProducto);
            if (linea != null)
                carrito.Lineas.Remove(linea);

            Session["Carrito"] = carrito;
            return Json(new { success = true });
        }

        // Finalizar venta
        [HttpPost]
        public ActionResult FinalizarVenta()
        {
            var carrito = Session["Carrito"] as VentaViewModel;
            if (carrito == null || carrito.Lineas.Count == 0)
                return new HttpStatusCodeResult(400, "Carrito vacío");

            int idUsuario = 1;

            using (var db = new Context())
            {
                var venta = new Venta
                {
                    IdUsuario = idUsuario,
                    FechaVenta = DateTime.Now,
                    Total = carrito.Total
                };
                db.Ventas.Add(venta);
                db.SaveChanges();

                foreach (var linea in carrito.Lineas)
                {
                    var detalle = new DetalleVentaProducto
                    {
                        IdVenta = venta.IdVenta,
                        IdProducto = linea.IdProducto,
                        Cantidad = linea.Cantidad,
                        PrecioUnitario = linea.PrecioUnitario
                    };
                    db.DetalleVentaProductos.Add(detalle);
                }

                db.SaveChanges();
            }

            Session["Carrito"] = new VentaViewModel();
            return Json(new { success = true, mensaje = "Venta registrada correctamente." });
        }
    }
}
