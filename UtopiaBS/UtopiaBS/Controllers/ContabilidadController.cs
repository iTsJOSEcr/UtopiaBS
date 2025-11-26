using System;
using System.Web.Mvc;
using UtopiaBS.Business.Contabilidad;
using UtopiaBS.Entities.Contabilidad;

namespace UtopiaBS.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class ContabilidadController : Controller
    {
        private readonly ContabilidadService service = new ContabilidadService();

        // ------------------- INDEX / DASHBOARD -------------------
        [HttpGet]
        public ActionResult Index() => View();

        // ------------------- INGRESO -------------------
        [HttpGet]
        public ActionResult AgregarIngreso()
        {
            if (TempData["Mensaje"] != null)
                ViewBag.Mensaje = TempData["Mensaje"];
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AgregarIngreso(Ingreso ingreso)
        {
            if (!ModelState.IsValid) return View(ingreso);

            var mensaje = service.AgregarIngreso(ingreso);
            TempData["Mensaje"] = mensaje;

            if (!string.IsNullOrEmpty(mensaje) && mensaje.Contains("exitosamente"))
                return RedirectToAction("AgregarIngreso"); // PRG

            return View(ingreso);
        }

        // ------------------- EGRESO -------------------
        [HttpGet]
        public ActionResult AgregarEgreso() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AgregarEgreso(Egreso egreso)
        {
            if (!ModelState.IsValid) return View(egreso);

            var mensaje = service.AgregarEgreso(egreso);
            TempData["Mensaje"] = mensaje;

            if (!string.IsNullOrEmpty(mensaje) && mensaje.Contains("exitosamente"))
                ModelState.Clear();

            return View(egreso);
        }

        // ------------------- RESUMEN DIARIO -------------------
        [HttpGet]
        public ActionResult ResumenDiario(DateTime? fecha)
        {
            var fechaSeleccionada = (fecha ?? DateTime.Now).Date;
            var resumen = service.ObtenerResumenDiario(fechaSeleccionada);

            if (resumen == null)
            {
                TempData["Mensaje"] = "No hay registros para la fecha seleccionada o ocurrió un error.";
                return View();
            }

            return View(resumen);
        }

        // =================== RESUMEN MENSUAL (con filtro) ===================
        // GET: /Contabilidad/ResumenMensual?year=2025&month=11&filtro=productos
        [HttpGet]
        public ActionResult ResumenMensual(int? year, int? month, string filtro = "todo")
        {
            // Vista vacía con formulario si faltan parámetros
            if (!year.HasValue || !month.HasValue)
            {
                ViewBag.Filtro = NormalizarFiltro(filtro);
                return View();
            }

            var y = year.Value;
            var m = LimitarMes(month.Value);
            var f = NormalizarFiltro(filtro);

            // Usa el overload con filtro en el servicio
            var vm = service.ObtenerResumenMensual(y, m, f);
            ViewBag.Filtro = vm?.Filtro ?? f;

            return View(vm);
        }

        // ------------------- EXPORTAR RESUMEN MENSUAL (Excel) -------------------
        [HttpGet]
        public ActionResult ExportarResumenMensual(int? year, int? month, string filtro = "todo")
        {
            var y = year ?? DateTime.Now.Year;
            var m = LimitarMes(month ?? DateTime.Now.Month);
            var f = NormalizarFiltro(filtro);

            var bytes = service.ExportarResumenMensualExcel(y, m, f);
            if (bytes == null || bytes.Length == 0)
            {
                TempData["Mensaje"] = "No se pudo generar el archivo de resumen mensual.";
                return RedirectToAction("ResumenMensual", new { year = y, month = m, filtro = f });
            }

            var fileName = $"ResumenMensual_{y}_{m:00}_{f.ToUpper()}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // ------------------- CIERRE SEMANAL -------------------
        [HttpGet]
        public ActionResult GenerarCierreSemanal() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GenerarCierreSemanal(DateTime inicio, DateTime fin)
        {
            CierreSemanal cierre = null;

            if (inicio == default || fin == default)
            {
                TempData["Mensaje"] = "Debe seleccionar fecha de inicio y fecha de fin.";
            }
            else if (inicio.Date > fin.Date)
            {
                TempData["Mensaje"] = "La fecha de inicio no puede ser mayor a la fecha fin.";
            }
            else
            {
                TempData["Mensaje"] = service.GenerarCierreSemanal(inicio, fin);
                cierre = service.ObtenerCierreSemanal(inicio, fin);

                if (cierre == null)
                {
                    TempData["Mensaje"] = string.IsNullOrEmpty(TempData["Mensaje"] as string)
                        ? "No se encontró un cierre para las fechas especificadas."
                        : TempData["Mensaje"];
                }
            }

            return View(cierre);
        }

        // ------------------- DESCARGAR CIERRE (PDF/Excel) -------------------
        [HttpGet]
        public ActionResult DescargarCierre(DateTime inicio, DateTime fin, string formato)
        {
            try
            {
                var fmt = string.IsNullOrWhiteSpace(formato) ? "pdf" : formato.Trim().ToLower();
                var archivo = service.DescargarCierreSemanal(inicio, fin, fmt);

                if (archivo == null || archivo.Length == 0)
                {
                    TempData["Mensaje"] = "No se pudo generar el archivo o el archivo está vacío.";
                    return RedirectToAction("GenerarCierreSemanal");
                }

                string nombreArchivo = fmt == "excel"
                    ? $"Cierre_{inicio:yyyyMMdd}_{fin:yyyyMMdd}.xlsx"
                    : $"Cierre_{inicio:yyyyMMdd}_{fin:yyyyMMdd}.pdf";

                string mimeType = fmt == "excel"
                    ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                    : "application/pdf";

                return File(archivo, mimeType, nombreArchivo);
            }
            catch (Exception ex)
            {
                TempData["Mensaje"] = $"Error al descargar el cierre: {ex.Message}";
                return RedirectToAction("GenerarCierreSemanal");
            }
        }

        // =================== Helpers ===================
        private static int LimitarMes(int month)
        {
            if (month < 1) return 1;
            if (month > 12) return 12;
            return month;
        }

        private static string NormalizarFiltro(string filtro)
        {
            var f = (filtro ?? "todo").Trim().ToLower();
            return (f == "productos" || f == "servicios") ? f : "todo";
        }
    }
}
