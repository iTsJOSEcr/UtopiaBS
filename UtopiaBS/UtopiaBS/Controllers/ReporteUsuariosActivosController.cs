using System;
using System.Web.Mvc;
using UtopiaBS.Services;
using ClosedXML.Excel;
using System.IO;

namespace UtopiaBS.Controllers
{
    [Authorize(Roles = "Administrador")]

    public class ReporteUsuariosActivosController : Controller
    {
        private readonly ReporteUsuariosActivosService _service;

        public ReporteUsuariosActivosController()
        {
            _service = new ReporteUsuariosActivosService();
        }

        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Index(DateTime fechaInicio, DateTime fechaFin)
        {
            // Ajustar fecha fin para incluir todo el día completo
            fechaFin = fechaFin.AddDays(1).AddSeconds(-1);

            var datos = _service.ObtenerReporte(fechaInicio, fechaFin);

            ViewBag.FechaInicio = fechaInicio;
            ViewBag.FechaFin = fechaFin;

            return View("Resultado", datos);
        }

        // ====================
        // EXPORTAR A EXCEL
        // ====================
        public FileResult ExportarExcel(DateTime fechaInicio, DateTime fechaFin)
        {
            fechaFin = fechaFin.AddDays(1).AddSeconds(-1);

            var datos = _service.ObtenerReporte(fechaInicio, fechaFin);

            using (var wb = new XLWorkbook())
            {
                var ws = wb.Worksheets.Add("Usuarios Activos");

                // Encabezados
                ws.Cell(1, 1).Value = "Usuario";
                ws.Cell(1, 2).Value = "Inicio de Sesión";
                ws.Cell(1, 3).Value = "Fin de Sesión";

                int row = 2;

                foreach (var item in datos)
                {
                    ws.Cell(row, 1).Value = item.UserId;
                    ws.Cell(row, 2).Value = item.FechaInicio.ToString("yyyy-MM-dd HH:mm:ss");
                    ws.Cell(row, 3).Value = item.FechaFin?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Activo";
                    row++;
                }

                ws.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    var fileName = $"Reporte_Usuarios_Activos_{DateTime.Now:yyyyMMddHHmm}.xlsx";

                    return File(stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileName);
                }
            }
        }
    }
}
