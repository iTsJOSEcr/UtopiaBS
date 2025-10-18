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
            return View(); // Vista con links a ingresos, egresos y cierre semanal
        }

        // ------------------- INGRESO -------------------
        public ActionResult AgregarIngreso() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AgregarIngreso(Ingreso ingreso)
        {
            if (ModelState.IsValid)
            {
                ViewBag.Mensaje = service.AgregarIngreso(ingreso);
                if (ViewBag.Mensaje.ToString().Contains("exitosamente"))
                    ModelState.Clear();
            }
            return View(ingreso);
        }

        // ------------------- EGRESO -------------------
        public ActionResult AgregarEgreso() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AgregarEgreso(Egreso egreso)
        {
            if (ModelState.IsValid)
            {
                ViewBag.Mensaje = service.AgregarEgreso(egreso);
                if (ViewBag.Mensaje.ToString().Contains("exitosamente"))
                    ModelState.Clear();
            }
            return View(egreso);
        }

        // ------------------- RESUMEN DIARIO -------------------
        public ActionResult ResumenDiario(DateTime? fecha)
        {
            var fechaSeleccionada = fecha ?? DateTime.Now;
            var resumen = service.ObtenerResumenDiario(fechaSeleccionada);

            if (resumen == null)
                ViewBag.Mensaje = "No hay registros para la fecha seleccionada o ocurrió un error.";

            return View(resumen);
        }

        // ------------------- CIERRE SEMANAL -------------------
        public ActionResult GenerarCierreSemanal() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GenerarCierreSemanal(DateTime inicio, DateTime fin)
        {
            ViewBag.Mensaje = service.GenerarCierreSemanal(inicio, fin);

            // Traer el cierre generado para pasarlo a la vista
            var cierre = service.ObtenerCierreSemanal(inicio, fin);
            return View(cierre);
        }

        // ------------------- DESCARGAR CIERRE -------------------
        public ActionResult DescargarCierre(DateTime inicio, DateTime fin, string formato)
        {
            var archivo = service.DescargarCierreSemanal(inicio, fin, formato);
            string nombreArchivo = $"Cierre_{inicio:yyyyMMdd}_{fin:yyyyMMdd}.{(formato.ToLower() == "excel" ? "xlsx" : "pdf")}";
            return File(archivo, "application/octet-stream", nombreArchivo);
        }
    }
}