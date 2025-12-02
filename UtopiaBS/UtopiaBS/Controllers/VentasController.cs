using Microsoft.AspNet.Identity;
using System;
using System.Data.Entity.SqlServer;
using System.Linq;
using System.Web.Mvc;
using UtopiaBS.Business.Puntos;
using UtopiaBS.Data;
using UtopiaBS.Entities;
using UtopiaBS.Entities.Contabilidad;
using UtopiaBS.Entities.Recompensas;
using UtopiaBS.ViewModels;

namespace UtopiaBS.Controllers
{
    [Authorize(Roles = "Administrador,Empleado")]

    public class VentasController : Controller
    {
        //  PUNTO DE VENTA
        public ActionResult PuntoVenta()
        {
            var carrito = Session["Carrito"] as VentaViewModel ?? new VentaViewModel();
            return View(carrito);
        }

        //  CARRITO PARCIAL
        public PartialViewResult CarritoParcial()
        {
            var carrito = Session["Carrito"] as VentaViewModel ?? new VentaViewModel();

            using (var db = new Context())
            {
                // Rellenar CantidadStock para productos
                var productoIds = carrito.Lineas
                    .Where(l => !l.EsServicio)
                    .Select(l => l.IdProducto)
                    .Distinct()
                    .ToList();

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
                    var cupon = db.CuponDescuento
                                  .FirstOrDefault(c => c.Codigo.ToUpper() == carrito.CuponAplicado.ToUpper());

                    if (cupon != null)
                        carrito.AplicarCupon(cupon.Codigo, cupon.Tipo ?? "Monto", cupon.Valor);
                    else
                        carrito.LimpiarCupon();
                }
            }

            return PartialView("_CarritoParcial", carrito);
        }

        //  BUSCAR PRODUCTO
        public JsonResult BuscarProducto(string termino)
        {
            using (var db = new Context())
            {
                var productos = db.Productos
                    .Where(p =>
                        p.Nombre.Contains(termino) ||
                        SqlFunctions.StringConvert((double)p.IdProducto).Contains(termino))
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

        //  BUSCAR SERVICIO
        public JsonResult BuscarServicio(string termino)
        {
            using (var db = new Context())
            {
                var servicios = db.Servicios
                    .Where(s =>
                        s.Nombre.Contains(termino) ||
                        SqlFunctions.StringConvert((double)s.IdServicio).Contains(termino))
                    .Select(s => new
                    {
                        s.IdServicio,
                        s.Nombre,
                        PrecioUnitario = s.Precio,
                        s.Descripcion
                    })
                    .ToList();

                return Json(servicios, JsonRequestBehavior.AllowGet);
            }
        }

        //  AGREGAR PRODUCTO AL CARRITO
        [HttpPost]
        public JsonResult AgregarAlCarrito(int idProducto)
        {
            var carrito = Session["Carrito"] as VentaViewModel ?? new VentaViewModel();

            using (var db = new Context())
            {
                var producto = db.Productos.FirstOrDefault(p => p.IdProducto == idProducto);
                if (producto == null)
                    return Json(new { success = false, mensaje = "Producto no encontrado." });

                // VALIDACIÓN DE VENCIMIENTO
                if (producto.FechaExpiracion.HasValue)
                {
                    var hoy = DateTime.Today;
                    var fechaVence = producto.FechaExpiracion.Value.Date;

                    if (fechaVence <= hoy)
                    {
                        return Json(new
                        {
                            success = false,
                            mensaje = $"Este producto está vencido desde {fechaVence:dd/MM/yyyy} y no puede venderse."
                        });
                    }
                }

                var linea = carrito.Lineas
                    .FirstOrDefault(x => x.IdProducto == idProducto && !x.EsServicio);

                if (linea != null)
                {
                    if (linea.Cantidad + 1 > producto.CantidadStock)
                        return Json(new { success = false, mensaje = "Stock insuficiente." });

                    linea.Cantidad++;
                }
                else
                {
                    carrito.Lineas.Add(new LineaVentaViewModel
                    {
                        IdProducto = idProducto,
                        NombreProducto = producto.Nombre,
                        PrecioUnitario = producto.PrecioUnitario,
                        Cantidad = 1,
                        EsServicio = false,
                        CantidadStock = producto.CantidadStock
                    });
                }
            }

            Session["Carrito"] = carrito;
            return Json(new { success = true });
        }

        // AGREGAR SERVICIO AL CARRITO
        [HttpPost]
        public JsonResult AgregarServicioAlCarrito(int idServicio, string nombre, decimal precioUnitario)
        {
            var carrito = Session["Carrito"] as VentaViewModel ?? new VentaViewModel();

            var linea = carrito.Lineas.FirstOrDefault(x => x.EsServicio && x.IdProducto == idServicio);
            if (linea != null)
            {
                linea.Cantidad++;
            }
            else
            {
                carrito.Lineas.Add(new LineaVentaViewModel
                {
                    IdProducto = idServicio,
                    NombreProducto = nombre,
                    PrecioUnitario = precioUnitario,
                    Cantidad = 1,
                    EsServicio = true
                });
            }

            // Recalcular cupón si aplica
            if (!string.IsNullOrEmpty(carrito.CuponAplicado))
            {
                using (var db = new Context())
                {
                    var cupon = db.CuponDescuento
                                  .FirstOrDefault(c => c.Codigo.ToUpper() == carrito.CuponAplicado.ToUpper());
                    if (cupon != null)
                        carrito.AplicarCupon(cupon.Codigo, cupon.Tipo ?? "Monto", cupon.Valor);
                    else
                        carrito.LimpiarCupon();
                }
            }

            Session["Carrito"] = carrito;
            return Json(new { success = true });
        }

        // ACTUALIZAR LÍNEA
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
                    var cupon = db.CuponDescuento
                                  .FirstOrDefault(c => c.Codigo.ToUpper() == carrito.CuponAplicado.ToUpper());
                    if (cupon != null)
                        carrito.AplicarCupon(cupon.Codigo, cupon.Tipo ?? "Monto", cupon.Valor);
                    else
                        carrito.LimpiarCupon();
                }
            }

            Session["Carrito"] = carrito;
            return Json(new
            {
                success = true,
                subtotal = carrito.SubTotal,
                descuento = carrito.Descuento,
                total = carrito.Total
            });
        }

        // ELIMINAR LÍNEA
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
                    var cupon = db.CuponDescuento
                                  .FirstOrDefault(c => c.Codigo.ToUpper() == carrito.CuponAplicado.ToUpper());
                    if (cupon != null)
                        carrito.AplicarCupon(cupon.Codigo, cupon.Tipo ?? "Monto", cupon.Valor);
                    else
                        carrito.LimpiarCupon();
                }
            }

            Session["Carrito"] = carrito;
            return Json(new
            {
                success = true,
                subtotal = carrito.SubTotal,
                descuento = carrito.Descuento,
                total = carrito.Total
            });
        }

        // APLICAR CUPÓN
        [HttpPost]
        public JsonResult AplicarCupon(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
                return Json(new { success = false, mensaje = "Ingrese un código de cupón." });

            var carrito = Session["Carrito"] as VentaViewModel ?? new VentaViewModel();

            using (var db = new Context())
            {
                var now = DateTime.Now;
                var cupon = db.CuponDescuento
                              .FirstOrDefault(c => c.Codigo.ToUpper() == codigo.ToUpper());

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
                    descuentoCalculado = Math.Min(Math.Round(valor, 2), carrito.SubTotal);

                carrito.AplicarCupon(cupon.Codigo, tipo, cupon.Valor);
                Session["Carrito"] = carrito;

                return Json(new
                {
                    success = true,
                    mensaje = string.Equals(tipo, "Porcentaje", StringComparison.OrdinalIgnoreCase)
                        ? $"{valor}% de descuento aplicado."
                        : $"Descuento de {descuentoCalculado:C} aplicado.",
                    codigo = cupon.Codigo,
                    tipo = tipo,
                    valor = valor,
                    descuento = descuentoCalculado,
                    subtotal = carrito.SubTotal,
                    totalConDescuento = carrito.Total
                });
            }
        }

        // QUITAR CUPÓN
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

        // FINALIZAR VENTA
        [HttpPost]
        public JsonResult FinalizarVenta()
        {
            var carrito = Session["Carrito"] as VentaViewModel;

            if (carrito == null)
                return Json(new { success = false, mensaje = "El carrito en sesión es nulo. Vuelva a cargar la página." });

            if (carrito.Lineas == null || !carrito.Lineas.Any())
                return Json(new { success = false, mensaje = "El carrito está vacío. Agregue productos o servicios." });

            var userId = User.Identity.GetUserId();
            string puntosMensaje = "No se asignaron puntos.";

            try
            {
                using (var db = new Context())
                {
                    // CREAR LA VENTA
                    var venta = new Venta
                    {
                        IdUsuario = userId,
                        FechaVenta = DateTime.Now,
                        Total = carrito.Total,
                        IdCliente = carrito.IdCliente,
                        NombreCliente = carrito.NombreCliente,
                        CedulaCliente = carrito.CedulaCliente,
                        MontoDescuento = carrito.Descuento,

                        // AQUÍ SE ASOCIA LA RECOMPENSA CANJEADA (SI EXISTE)
                        CuponId = carrito.IdRecompensaCanjeada
                    };

                    db.Ventas.Add(venta);
                    db.SaveChanges(); 

                    // DETALLES Y STOCK
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

                            db.DetalleVentaServicios.Add(detalleServicio);

                            db.Entry(detalleServicio)
                              .Property("SubTotal")
                              .CurrentValue = subtotalLinea;
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

                            db.DetalleVentaProductos.Add(detalleProducto);

                            db.Entry(detalleProducto)
                              .Property("SubTotal")
                              .CurrentValue = subtotalLinea;

                            // DESCONTAR STOCK
                            var producto = db.Productos.First(p => p.IdProducto == linea.IdProducto);
                            producto.CantidadStock -= linea.Cantidad;
                        }
                    }

                    // ACUMULACIÓN DE PUNTOS (NO TOCA CANJE)
                    if (venta.IdCliente.HasValue)
                    {
                        var puntosService = new PuntosService();
                        var resultado = puntosService.AcumularPuntosPorVenta(
                            venta.IdCliente.Value,
                            venta.IdVenta,
                            venta.Total
                        );

                        puntosMensaje = resultado.Mensaje;
                    }
                    else
                    {
                        puntosMensaje = "No se asignaron puntos porque no se seleccionó un cliente.";
                    }

                    // INGRESO CONTABLE
                    var ingreso = new Ingreso
                    {
                        Monto = venta.Total,
                        Fecha = venta.FechaVenta,
                        Categoria = "Venta",
                        Descripcion = $"Venta #{venta.IdVenta} registrada automáticamente.",
                        UsuarioId = userId,
                        FechaCreacion = DateTime.Now
                    };

                    db.Ingresos.Add(ingreso);

                    db.SaveChanges();
                }

                // LIMPIAR CARRITO COMPLETO
                Session["Carrito"] = new VentaViewModel();

                return Json(new
                {
                    success = true,
                    mensaje = "✅ Venta registrada correctamente.",
                    puntosMensaje = puntosMensaje
                });
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                if (ex.InnerException != null)
                    msg += " | " + ex.InnerException.Message;
                if (ex.InnerException?.InnerException != null)
                    msg += " | " + ex.InnerException.InnerException.Message;

                return Json(new
                {
                    success = false,
                    mensaje = "❌ Error al registrar la venta: " + msg
                });
            }
        }

        // BUSCAR CLIENTE
        public JsonResult BuscarCliente(string termino)
        {
            using (var db = new Context())
            {
                var clientes = db.Clientes
                    .Where(c =>
                        (c.Nombre.Contains(termino) || c.Cedula.Contains(termino)))
                    .Select(c => new
                    {
                        c.IdCliente,
                        NombreCompleto = c.Nombre,
                        c.Cedula
                    })
                    .ToList();

                return Json(clientes, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult SeleccionarCliente(int idCliente)
        {
            var carrito = Session["Carrito"] as VentaViewModel ?? new VentaViewModel();

            using (var db = new Context())
            {
                var cliente = db.Clientes.FirstOrDefault(c => c.IdCliente == idCliente);
                if (cliente == null)
                    return Json(new { success = false, mensaje = "Cliente no encontrado." });

                carrito.IdCliente = cliente.IdCliente;
                carrito.NombreCliente = cliente.Nombre;
                carrito.CedulaCliente = cliente.Cedula;
            }

            Session["Carrito"] = carrito;
            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult QuitarCliente()
        {
            var carrito = Session["Carrito"] as VentaViewModel ?? new VentaViewModel();

            carrito.IdCliente = null;
            carrito.NombreCliente = null;
            carrito.CedulaCliente = null;

            Session["Carrito"] = carrito;
            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult CanjearRecompensa(int idRecompensa)
        {
            var carrito = Session["Carrito"] as VentaViewModel ?? new VentaViewModel();

            if (!carrito.IdCliente.HasValue)
                return Json(new { success = false, mensaje = "Debe seleccionar un cliente primero." });

            try
            {
                using (var db = new Context())
                {
                    var clienteId = carrito.IdCliente.Value;

                    var recompensa = db.Recompensas
                        .FirstOrDefault(r => r.IdRecompensa == idRecompensa && r.Activa);

                    if (recompensa == null)
                        return Json(new { success = false, mensaje = "Recompensa no válida." });

                    // CANJE DUPLICADO
                    bool yaCanjeada = db.CanjeRecompensas
                        .Any(c => c.IdCliente == clienteId && c.IdRecompensa == idRecompensa);

                    if (yaCanjeada)
                        return Json(new { success = false, mensaje = "Esta recompensa ya fue canjeada." });

                    //OBTENER PUNTOS ACTUALES
                    var membresia = db.Membresias
                        .OrderByDescending(m => m.FechaInicio)
                        .First(m => m.IdCliente == clienteId);

                    int puntosActuales = membresia.PuntosAcumulados;

                    if (puntosActuales < recompensa.PuntosNecesarios)
                        return Json(new { success = false, mensaje = "No tiene puntos suficientes." });

                    // REGISTRAR CANJE
                    var canje = new CanjeRecompensa
                    {
                        IdCliente = clienteId,
                        IdRecompensa = idRecompensa,
                        FechaCanje = DateTime.Now,
                        PuntosUtilizados = recompensa.PuntosNecesarios
                    };

                    db.CanjeRecompensas.Add(canje);

                    // RESTAR PUNTOS
                    membresia.PuntosAcumulados -= recompensa.PuntosNecesarios;

                    // APLICAR BENEFICIO SEGÚN TIPO
                    if (recompensa.Tipo == "Cupón")
                    {
                        carrito.Descuento += recompensa.Valor;
                    }
                    else if (recompensa.Tipo == "Servicio")
                    {
                        carrito.Lineas.Add(new LineaVentaViewModel
                        {
                            IdProducto = recompensa.IdRecompensa,
                            NombreProducto = recompensa.Nombre + " (Recompensa)",
                            PrecioUnitario = 0,
                            Cantidad = 1,
                            EsServicio = true
                        });
                    }
                    else if (recompensa.Tipo == "Producto")
                    {
                        carrito.Lineas.Add(new LineaVentaViewModel
                        {
                            IdProducto = recompensa.IdRecompensa,
                            NombreProducto = recompensa.Nombre + " (Recompensa)",
                            PrecioUnitario = 0,
                            Cantidad = 1,
                            EsServicio = false
                        });
                    }

                    db.SaveChanges();
                    Session["Carrito"] = carrito;

                    return Json(new
                    {
                        success = true,
                        mensaje = "✅ Recompensa canjeada correctamente."
                    });
                }
            }
            catch
            {
                return Json(new { success = false, mensaje = "Error interno del servidor." });
            }
        }


    }
}
