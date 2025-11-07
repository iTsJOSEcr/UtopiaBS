using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using UtopiaBS.Entities.Contabilidad;
using UtopiaBS.Business.Contabilidad;


namespace UtopiaBS.Web.Controllers
{
    public class ContabilidadController : Controller
    {
        private readonly ContabilidadService service = new ContabilidadService();

        // ------------------- INDEX / DASHBOARD -------------------
        public ActionResult Index()
        {
            return View();
        }

        // ------------------- INGRESO -------------------
        // GET
        public ActionResult AgregarIngreso()
        {
            // Mostrar mensaje si existe en TempData
            if (TempData["Mensaje"] != null)
                ViewBag.Mensaje = TempData["Mensaje"];

            return View();
        }

        // POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AgregarIngreso(Ingreso ingreso)
        {
            if (ModelState.IsValid)
            {
                var mensaje = service.AgregarIngreso(ingreso);
                TempData["Mensaje"] = mensaje;

                if (!string.IsNullOrEmpty(mensaje) && mensaje.Contains("exitosamente"))
                    return RedirectToAction("AgregarIngreso"); // PRG: redirige al GET
            }

            return View(ingreso); // si hay error, se queda en POST
        }


        // ------------------- EGRESO -------------------
        public ActionResult AgregarEgreso() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AgregarEgreso(Egreso egreso)
        {
            if (ModelState.IsValid)
            {
                var mensaje = service.AgregarEgreso(egreso);
                TempData["Mensaje"] = mensaje;
                if (!string.IsNullOrEmpty(mensaje) && mensaje.Contains("exitosamente"))
                    ModelState.Clear();
            }
            return View(egreso);
        }

        // ------------------- RESUMEN DIARIO -------------------

        [Authorize(Roles = "Administrador")]
        public ActionResult ResumenDiario(DateTime? fecha)
        {
            var fechaSeleccionada = fecha ?? DateTime.Now;
            var resumen = service.ObtenerResumenDiario(fechaSeleccionada);

            if (resumen == null)
            {
                TempData["Mensaje"] = "No hay registros para la fecha seleccionada o ocurrió un error.";
                return View();
            }

            return View(resumen);
        }
        // ------------------- CIERRE SEMANAL -------------------
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
                // Genera el cierre (puedes dejar mensaje opcional)
                TempData["Mensaje"] = service.GenerarCierreSemanal(inicio, fin);

                // Obtén el cierre filtrado por el rango exacto
                cierre = service.ObtenerCierreSemanal(inicio, fin);

                if (cierre == null)
                {
                    TempData["Mensaje"] = string.IsNullOrEmpty(TempData["Mensaje"] as string)
                        ? "No se encontró un cierre para las fechas especificadas."
                        : TempData["Mensaje"];
                }
            }

            // Siempre devuelve la vista con el modelo (puede ser null)
            return View(cierre);
        }


        // ------------------- DESCARGAR CIERRE -------------------
        public ActionResult DescargarCierre(DateTime inicio, DateTime fin, string formato)
        {
            try
            {
                formato = string.IsNullOrWhiteSpace(formato) ? "pdf" : formato.Trim().ToLower();
                var archivo = service.DescargarCierreSemanal(inicio, fin, formato);

                if (archivo == null || archivo.Length == 0)
                {
                    TempData["Mensaje"] = "No se pudo generar el archivo o el archivo está vacío.";
                    return RedirectToAction("GenerarCierreSemanal");
                }

                string nombreArchivo = formato == "excel"
                    ? $"Cierre_{inicio:yyyyMMdd}_{fin:yyyyMMdd}.xlsx"
                    : $"Cierre_{inicio:yyyyMMdd}_{fin:yyyyMMdd}.pdf";

                string mimeType = formato == "excel"
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
    }
}




