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

        // Devuelve solo el partial del carrito (para refrescar UI desde JS)
        public PartialViewResult CarritoParcial()
        {
            var carrito = Session["Carrito"] as VentaViewModel ?? new VentaViewModel();

            using (var db = new Context())
            {
                // Rellenar CantidadStock para productos
                var productoIds = carrito.Lineas.Where(l => !l.EsServicio).Select(l => l.IdProducto).Distinct().ToList();
                if (productoIds.Any())
                {
                    var stocks = db.Productos
                                   .Where(p => productoIds.Contains(p.IdProducto))
                                   .Select(p => new { p.IdProducto, p.CantidadStock })
                                   .ToList();

                    foreach (var linea in carrito.Lineas.Where(l => !l.EsServicio))
                    {
                        var prod = stocks.FirstOrDefault(s => s.IdProducto == linea.IdProducto);
                        linea.CantidadStock = prod?.CantidadStock ?? 0;
                    }
                }

                // Recalcular cupón si aplica
                if (!string.IsNullOrEmpty(carrito.CuponAplicado))
                {
                    var cupon = db.CuponDescuento.FirstOrDefault(c => c.Codigo.ToUpper() == carrito.CuponAplicado.ToUpper());
                    if (cupon != null)
                        carrito.AplicarCupon(cupon.Codigo, cupon.Tipo ?? "Monto", cupon.Valor);
                    else
                        carrito.LimpiarCupon();
                }
            }

            return PartialView("_CarritoParcial", carrito);
        }

        // Buscar productos por nombre o ID
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

        // Buscar servicios por nombre o ID
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
                        PrecioUnitario = s.Precio,
                        s.Descripcion
                    })
                    .ToList();

                return Json(servicios, JsonRequestBehavior.AllowGet);
            }
        }

        // Agregar producto al carrito
        [HttpPost]
        public JsonResult AgregarAlCarrito(int idProducto, string nombre, decimal precioUnitario)
        {
            var carrito = Session["Carrito"] as VentaViewModel ?? new VentaViewModel();

            using (var db = new Context())
            {
                var producto = db.Productos.SingleOrDefault(p => p.IdProducto == idProducto);
                if (producto == null)
                    return Json(new { success = false, mensaje = "Producto no encontrado." });

                var linea = carrito.Lineas.FirstOrDefault(x => x.IdProducto == idProducto && !x.EsServicio);
                int nuevaCantidad = linea != null ? linea.Cantidad + 1 : 1;

                if (nuevaCantidad > producto.CantidadStock)
                    return Json(new { success = false, mensaje = $"Stock insuficiente. Disponible: {producto.CantidadStock}" });

                if (linea != null)
                    linea.Cantidad = nuevaCantidad;
                else
                    carrito.Lineas.Add(new LineaVentaViewModel
                    {
                        IdProducto = idProducto,
                        NombreProducto = producto.Nombre,
                        PrecioUnitario = producto.PrecioUnitario,
                        Cantidad = 1,
                        EsServicio = false,
                        CantidadStock = producto.CantidadStock
                    });

                // Recalcular cupón si aplica
                if (!string.IsNullOrEmpty(carrito.CuponAplicado))
                {
                    var cupon = db.CuponDescuento.FirstOrDefault(c => c.Codigo.ToUpper() == carrito.CuponAplicado.ToUpper());
                    if (cupon != null)
                        carrito.AplicarCupon(cupon.Codigo, cupon.Tipo ?? "Monto", cupon.Valor);
                    else
                        carrito.LimpiarCupon();
                }

                Session["Carrito"] = carrito;
            }

            return Json(new { success = true });
        }

        // Agregar servicio al carrito
        [HttpPost]
        public JsonResult AgregarServicioAlCarrito(int idServicio, string nombre, decimal precioUnitario)
        {
            var carrito = Session["Carrito"] as VentaViewModel ?? new VentaViewModel();

            var linea = carrito.Lineas.FirstOrDefault(x => x.EsServicio && x.IdProducto == idServicio);
            if (linea != null)
                linea.Cantidad++;
            else
                carrito.Lineas.Add(new LineaVentaViewModel
                {
                    IdProducto = idServicio,
                    NombreProducto = nombre,
                    PrecioUnitario = precioUnitario,
                    Cantidad = 1,
                    EsServicio = true
                });

            // Recalcular cupón si aplica
            if (!string.IsNullOrEmpty(carrito.CuponAplicado))
            {
                using (var db = new Context())
                {
                    var cupon = db.CuponDescuento.FirstOrDefault(c => c.Codigo.ToUpper() == carrito.CuponAplicado.ToUpper());
                    if (cupon != null)
                        carrito.AplicarCupon(cupon.Codigo, cupon.Tipo ?? "Monto", cupon.Valor);
                    else
                        carrito.LimpiarCupon();
                }
            }

            Session["Carrito"] = carrito;
            return Json(new { success = true });
        }

        // Actualizar cantidad de línea
        [HttpPost]
        public JsonResult ActualizarLinea(int idProducto, int cantidad, bool esServicio)
        {
            var carrito = Session["Carrito"] as VentaViewModel ?? new VentaViewModel();
            var linea = carrito.Lineas.FirstOrDefault(x => x.IdProducto == idProducto && x.EsServicio == esServicio);
            if (linea != null)
            {
                if (!esServicio)
                {
                    using (var db = new Context())
                    {
                        var producto = db.Productos.SingleOrDefault(p => p.IdProducto == idProducto);
                        if (producto == null)
                            return Json(new { success = false, mensaje = "Producto no encontrado." });

                        if (cantidad > producto.CantidadStock)
                            return Json(new { success = false, mensaje = $"Stock insuficiente. Disponible: {producto.CantidadStock}" });

                        linea.CantidadStock = producto.CantidadStock;
                    }
                }

                if (cantidad <= 0)
                    carrito.Lineas.Remove(linea);
                else
                    linea.Cantidad = cantidad;
            }

            // Recalcular cupón
            if (!string.IsNullOrEmpty(carrito.CuponAplicado))
            {
                using (var db = new Context())
                {
                    var cupon = db.CuponDescuento.FirstOrDefault(c => c.Codigo.ToUpper() == carrito.CuponAplicado.ToUpper());
                    if (cupon != null)
                        carrito.AplicarCupon(cupon.Codigo, cupon.Tipo ?? "Monto", cupon.Valor);
                    else
                        carrito.LimpiarCupon();
                }
            }

            Session["Carrito"] = carrito;
            return Json(new { success = true, subtotal = carrito.SubTotal, descuento = carrito.Descuento, total = carrito.Total });
        }

        // Eliminar línea
        [HttpPost]
        public JsonResult EliminarLinea(int idProducto, bool esServicio)
        {
            var carrito = Session["Carrito"] as VentaViewModel ?? new VentaViewModel();
            var linea = carrito.Lineas.FirstOrDefault(x => x.IdProducto == idProducto && x.EsServicio == esServicio);
            if (linea != null)
                carrito.Lineas.Remove(linea);

            // Recalcular cupón
            if (!string.IsNullOrEmpty(carrito.CuponAplicado))
            {
                using (var db = new Context())
                {
                    var cupon = db.CuponDescuento.FirstOrDefault(c => c.Codigo.ToUpper() == carrito.CuponAplicado.ToUpper());
                    if (cupon != null)
                        carrito.AplicarCupon(cupon.Codigo, cupon.Tipo ?? "Monto", cupon.Valor);
                    else
                        carrito.LimpiarCupon();
                }
            }

            Session["Carrito"] = carrito;
            return Json(new { success = true, subtotal = carrito.SubTotal, descuento = carrito.Descuento, total = carrito.Total });
        }

        // Aplicar cupón
        [HttpPost]
        public JsonResult AplicarCupon(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
                return Json(new { success = false, mensaje = "Ingrese un código de cupón." });

            var carrito = Session["Carrito"] as VentaViewModel ?? new VentaViewModel();

            using (var db = new Context())
            {
                var now = DateTime.Now;
                var cupon = db.CuponDescuento.FirstOrDefault(c => c.Codigo.ToUpper() == codigo.ToUpper());

                if (cupon == null)
                    return Json(new { success = false, mensaje = "Cupón no encontrado." });

                if (!cupon.Activo)
                    return Json(new { success = false, mensaje = "Cupón no activo." });

                if (cupon.FechaInicio > now || cupon.FechaFin < now)
                    return Json(new { success = false, mensaje = "Cupón fuera de vigencia." });

                if (cupon.UsoMaximo.HasValue && cupon.UsoMaximo.Value > 0)
                {
                    var usoActual = cupon.UsoActual.GetValueOrDefault(0);
                    if (usoActual >= cupon.UsoMaximo.Value)
                        return Json(new { success = false, mensaje = "Cupón ya alcanzó su uso máximo." });
                }

                // Calcular descuento
                decimal descuentoCalculado = 0m;
                string tipo = string.IsNullOrWhiteSpace(cupon.Tipo) ? "Monto" : cupon.Tipo;
                decimal valor = cupon.Valor;

                if (string.Equals(tipo, "Porcentaje", StringComparison.OrdinalIgnoreCase))
                    descuentoCalculado = Math.Round(carrito.SubTotal * (valor / 100m), 2);
                else
                {
                    descuentoCalculado = Math.Min(Math.Round(valor, 2), carrito.SubTotal);
                }

                carrito.AplicarCupon(cupon.Codigo, tipo, cupon.Valor);
                Session["Carrito"] = carrito;

                return Json(new
                {
                    success = true,
                    mensaje = string.Equals(tipo, "Porcentaje", StringComparison.OrdinalIgnoreCase) ? $"{valor}% de descuento aplicado." : $"Descuento de {descuentoCalculado:C} aplicado.",
                    codigo = cupon.Codigo,
                    tipo = tipo,
                    valor = valor,
                    descuento = descuentoCalculado,
                    subtotal = carrito.SubTotal,
                    totalConDescuento = carrito.Total
                });
            }
        }

        // Quitar cupón
        [HttpPost]
        public JsonResult QuitarCupon()
        {
            var carrito = Session["Carrito"] as VentaViewModel ?? new VentaViewModel();
            carrito.LimpiarCupon();
            Session["Carrito"] = carrito;

            return Json(new
            {
                success = true,
                mensaje = "Cupón quitado.",
                subtotal = carrito.SubTotal,
                descuento = carrito.Descuento,
                total = carrito.Total
            });
        }

        [HttpPost]
        public JsonResult FinalizarVenta()
        {
            var carrito = Session["Carrito"] as VentaViewModel;
            if (carrito == null || carrito.Lineas.Count == 0)
                return Json(new { success = false, mensaje = "Carrito vacío" });

            int idUsuario = 1; // remplazar por usuario real si aplica

            using (var db = new Context())
            {
                // Validar stock para productos
                foreach (var linea in carrito.Lineas.Where(l => !l.EsServicio))
                {
                    var producto = db.Productos.FirstOrDefault(p => p.IdProducto == linea.IdProducto);
                    if (producto == null)
                        return Json(new { success = false, mensaje = $"El producto {linea.NombreProducto} no existe" });

                    if (producto.CantidadStock < linea.Cantidad)
                        return Json(new { success = false, mensaje = $"No hay stock suficiente para {producto.Nombre}. Disponible: {producto.CantidadStock}, solicitado: {linea.Cantidad}" });
                }

                // Crear la venta: Total ya debe considerar el descuento aplicado en el carrito (carrito.Total)
                var venta = new Venta
                {
                    IdUsuario = idUsuario,
                    FechaVenta = DateTime.Now,
                    Total = carrito.Total,
                    CuponId = null,
                    MontoDescuento = 0m
                };

                // Si el carrito tiene cupón aplicado, tratar de obtener su id y monto descontado
                if (!string.IsNullOrEmpty(carrito.CuponAplicado))
                {
                    var cupon = db.CuponDescuento.FirstOrDefault(c => c.Codigo.ToUpper() == carrito.CuponAplicado.ToUpper());
                    if (cupon != null)
                    {
                        venta.CuponId = cupon.CuponId;

                        // Preferimos tomar el valor calculado por el carrito (carrito.Descuento) si existe.
                        // Si no existe, calculamos aquí para mayor seguridad.
                        decimal montoDescuento = carrito.Descuento;
                        if (montoDescuento <= 0m)
                        {
                            // calcular según tipo
                            string tipo = string.IsNullOrWhiteSpace(cupon.Tipo) ? "Monto" : cupon.Tipo;
                            decimal valor = cupon.Valor;
                            if (string.Equals(tipo, "Porcentaje", StringComparison.OrdinalIgnoreCase))
                            {
                                montoDescuento = Math.Round(carrito.SubTotal * (valor / 100m), 2);
                            }
                            else
                            {
                                montoDescuento = Math.Round(valor, 2);
                                if (montoDescuento > carrito.SubTotal) montoDescuento = carrito.SubTotal;
                            }
                        }
                        venta.MontoDescuento = montoDescuento;
                    }
                    else
                    {
                        // Cupón aplicado en carrito ya no existe en BD -> limpiamos info del cupón
                        venta.CuponId = null;
                        venta.MontoDescuento = 0m;
                        carrito.LimpiarCupon();
                    }
                }

                // Agregar venta a BD para obtener IdVenta antes de agregar detalles
                db.Ventas.Add(venta);
                db.SaveChanges();

                // Guardar detalles y ajustar stock
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
                        db.Entry(detalleProducto).Property("SubTotal").CurrentValue = subtotalLinea;
                        db.DetalleVentaProductos.Add(detalleProducto);

                        var producto = db.Productos.First(p => p.IdProducto == linea.IdProducto);
                        producto.CantidadStock -= linea.Cantidad;
                    }
                }

                // Registrar uso del cupón (incrementar UsoActual) si aplica
                if (!string.IsNullOrEmpty(carrito.CuponAplicado))
                {
                    var cupon = db.CuponDescuento.FirstOrDefault(c => c.Codigo.ToUpper() == carrito.CuponAplicado.ToUpper());
                    if (cupon != null)
                    {
                        cupon.UsoActual = cupon.UsoActual.GetValueOrDefault(0) + 1;
                    }
                }

                db.SaveChanges();
            }

            // Limpiar carrito en sesión
            Session["Carrito"] = new VentaViewModel();
            return Json(new { success = true, mensaje = "Venta registrada correctamente." });
        }

    }
}
