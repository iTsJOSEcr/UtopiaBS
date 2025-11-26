using OfficeOpenXml;
using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using UtopiaBS.Business;
using UtopiaBS.Data;
using UtopiaBS.Entities;
using UtopiaBS.ViewModels;

namespace UtopiaBS.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class ProductoController : Controller
    {
        private readonly ProductoService service = new ProductoService();

        // GET: Producto/Agregar
        public ActionResult Agregar()
        {
            return View();
        }

        // POST: Producto/Agregar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Agregar(Producto nuevo)
        {
            if (ModelState.IsValid)
            {
                nuevo.Fecha = DateTime.Now;
                nuevo.Threshold = (nuevo.CantidadStock > 0) ? 0 : 1;

                // Nuevo: si no se define, default = 30 días
                nuevo.DiasAnticipacion = nuevo.DiasAnticipacion ?? 30;

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
                var vm = new InventarioViewModel
                {
                    Productos = db.Productos.ToList(),
                    Cupones = db.CuponDescuento.ToList()
                };

                // ALERTAS
                DateTime hoy = DateTime.Now.Date;
                int diasAlerta = 30; // 1 mes

                vm.ProductosExpirados = vm.Productos
                    .Where(p => p.FechaExpiracion.HasValue &&
                                p.FechaExpiracion.Value.Date < hoy)
                    .ToList();

                vm.ProductosPorExpirar = vm.Productos
                    .Where(p =>
                        p.FechaExpiracion.HasValue &&
                        p.FechaExpiracion.Value.Date >= hoy &&
                        (p.FechaExpiracion.Value.Date - hoy).TotalDays <= diasAlerta
                    )
                    .ToList();

                return View(vm);
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
                {
                    // TempData["MensajeProducto"] = resultado;
                    return RedirectToAction("Listar");
                }
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

        // GET: Producto/ExportarInventario
        // Genera un Excel con 2 hojas: Productos y Servicios
        public ActionResult ExportarInventario()
        {
            using (var db = new Context())
            {
                var productos = db.Productos.ToList();
                var servicios = db.Servicios.ToList();

                using (var package = new ExcelPackage())
                {
                    // -----------------------------
                    // HOJA 1: PRODUCTOS
                    // -----------------------------
                    var wsProductos = package.Workbook.Worksheets.Add("Productos");

                    wsProductos.Cells["A1"].Value = "Nombre";
                    wsProductos.Cells["B1"].Value = "Tipo";
                    wsProductos.Cells["C1"].Value = "Proveedor";
                    wsProductos.Cells["D1"].Value = "Precio";
                    wsProductos.Cells["E1"].Value = "Stock";
                    wsProductos.Cells["F1"].Value = "Fecha";
                    wsProductos.Cells["G1"].Value = "Threshold";

                    wsProductos.Row(1).Style.Font.Bold = true;

                    int row = 2;
                    foreach (var p in productos)
                    {
                        wsProductos.Cells[row, 1].Value = p.Nombre;
                        wsProductos.Cells[row, 2].Value = p.Tipo;
                        wsProductos.Cells[row, 3].Value = p.Proveedor;
                        wsProductos.Cells[row, 4].Value = p.PrecioUnitario;
                        wsProductos.Cells[row, 5].Value = p.CantidadStock;
                        wsProductos.Cells[row, 6].Value = p.Fecha.ToShortDateString();
                        wsProductos.Cells[row, 7].Value = p.Threshold;
                        row++;
                    }

                    wsProductos.Cells.AutoFitColumns();

                    // -----------------------------
                    // HOJA 2: SERVICIOS
                    // -----------------------------
                    var wsServicios = package.Workbook.Worksheets.Add("Servicios");

                    wsServicios.Cells["A1"].Value = "Nombre";
                    wsServicios.Cells["B1"].Value = "Descripción";
                    wsServicios.Cells["C1"].Value = "Precio";
                    wsServicios.Cells["D1"].Value = "Estado";

                    wsServicios.Row(1).Style.Font.Bold = true;

                    row = 2;
                    foreach (var s in servicios)
                    {
                        wsServicios.Cells[row, 1].Value = s.Nombre;
                        wsServicios.Cells[row, 2].Value = s.Descripcion;
                        wsServicios.Cells[row, 3].Value = s.Precio;
                        wsServicios.Cells[row, 4].Value = s.IdEstado;
                        row++;
                    }

                    wsServicios.Cells.AutoFitColumns();

                    var stream = new MemoryStream(package.GetAsByteArray());
                    return File(stream,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "ReporteInventario.xlsx");
                }
            }
        }
    }
}
