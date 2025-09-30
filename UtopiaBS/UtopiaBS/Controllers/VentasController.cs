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

        // Buscar servicios por nombre o Id
        public JsonResult BuscarServicio(string termino)
        {
            using (var db = new Context())
            {
                var servicios = db.Servicios
                    .Where(s => s.Nombre.Contains(termino) || SqlFunctions.StringConvert((double)s.IdServicio).Contains(termino))
                    .Select(s => new
                    {
                        IdServicio = s.IdServicio,
                        s.Nombre,
                        PrecioUnitario = s.Precio, // mapeamos Precio -> PrecioUnitario para consistencia con la UI
                        s.Descripcion
                    })
                    .ToList();

                return Json(servicios, JsonRequestBehavior.AllowGet);
            }
        }

        // Agregar producto al carrito
        [HttpPost]
        public ActionResult AgregarAlCarrito(int idProducto, string nombre, decimal precioUnitario)
        {
            var carrito = Session["Carrito"] as VentaViewModel ?? new VentaViewModel();

            var linea = carrito.Lineas.FirstOrDefault(x => x.IdProducto == idProducto && !x.EsServicio);
            if (linea != null)
                linea.Cantidad++;
            else
                carrito.Lineas.Add(new LineaVentaViewModel
                {
                    IdProducto = idProducto,
                    NombreProducto = nombre,
                    PrecioUnitario = precioUnitario,
                    Cantidad = 1,
                    EsServicio = false
                });

            Session["Carrito"] = carrito;

            return Json(new { success = true });
        }

        // Agregar servicio al carrito
        [HttpPost]
        public ActionResult AgregarServicioAlCarrito(int idServicio, string nombre, decimal precioUnitario)
        {
            var carrito = Session["Carrito"] as VentaViewModel ?? new VentaViewModel();

            // Buscar si ya existe la misma línea de servicio (mismo Id y EsServicio = true)
            var linea = carrito.Lineas.FirstOrDefault(x => x.EsServicio && x.IdProducto == idServicio);
            if (linea != null)
            {
                linea.Cantidad++; // normalmente 1 por servicio, pero soportamos sumar si ya existe
            }
            else
            {
                carrito.Lineas.Add(new LineaVentaViewModel
                {
                    IdProducto = idServicio,
                    NombreProducto = nombre,
                    EsServicio = true,
                    PrecioUnitario = precioUnitario,
                    Cantidad = 1
                });
            }

            Session["Carrito"] = carrito;
            return Json(new { success = true });
        }

        // Actualizar cantidad o eliminar 
        [HttpPost]
        public ActionResult ActualizarLinea(int idProducto, int cantidad, bool esServicio)
        {
            var carrito = Session["Carrito"] as VentaViewModel;
            if (carrito == null) return new HttpStatusCodeResult(400);

            var linea = carrito.Lineas.FirstOrDefault(x => x.IdProducto == idProducto && x.EsServicio == esServicio);
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
        public ActionResult EliminarLinea(int idProducto, bool esServicio)
        {
            var carrito = Session["Carrito"] as VentaViewModel;
            if (carrito == null) return new HttpStatusCodeResult(400);

            var linea = carrito.Lineas.FirstOrDefault(x => x.IdProducto == idProducto && x.EsServicio == esServicio);
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

            int idUsuario = 1; // reemplazar por usuario actual

            using (var db = new Context())
            {
                // Calcular total real del carrito
                decimal totalVenta = carrito.Lineas.Sum(l => l.PrecioUnitario * l.Cantidad);

                var venta = new Venta
                {
                    IdUsuario = idUsuario,
                    FechaVenta = DateTime.Now,
                    Total = totalVenta
                };
                db.Ventas.Add(venta);
                db.SaveChanges();

                foreach (var linea in carrito.Lineas)
                {
                    var subtotalLinea = linea.PrecioUnitario * linea.Cantidad;

                    if (linea.EsServicio)
                    {
                        var detalleServicio = new DetalleVentaServicio
                        {
                            IdVenta = venta.IdVenta,
                            IdServicio = linea.IdProducto,
                            Cantidad = linea.Cantidad,
                            PrecioUnitario = linea.PrecioUnitario
                        };

                        // Asignamos subtotal mediante EF usando propiedad privada + BD
                        db.Entry(detalleServicio).Property("SubTotal").CurrentValue = subtotalLinea;

                        db.DetalleVentaServicios.Add(detalleServicio);
                    }
                    else
                    {
                        var detalleProducto = new DetalleVentaProducto
                        {
                            IdVenta = venta.IdVenta,
                            IdProducto = linea.IdProducto,
                            Cantidad = linea.Cantidad,
                            PrecioUnitario = linea.PrecioUnitario
                        };

                        // Asignamos subtotal mediante EF usando propiedad privada + BD
                        db.Entry(detalleProducto).Property("SubTotal").CurrentValue = subtotalLinea;

                        db.DetalleVentaProductos.Add(detalleProducto);
                    }
                }

                db.SaveChanges();
            }

            // Limpiar carrito
            Session["Carrito"] = new VentaViewModel();
            return Json(new { success = true, mensaje = "Venta registrada correctamente." });
        }
    }
}
