using iTextSharp.text;           // iTextSharp
using iTextSharp.text.pdf;
using OfficeOpenXml;              // EPPlus
using System;
using System.ComponentModel;
using System.Data.Entity;
using System.IO;
using System.Linq;
using UtopiaBS.Data;
using UtopiaBS.Entities.Contabilidad;


namespace UtopiaBS.Business.Contabilidad
{
    public class ContabilidadService
    {
        public ContabilidadService()
        {
            // Configurar licencia para uso no comercial
            ExcelPackage.License.SetNonCommercialPersonal("Utopia");
        }


        // ------------------- INGRESOS -------------------
        public string AgregarIngreso(Ingreso ingreso)
        {
            try
            {
                if (ingreso == null) return "Error: datos del ingreso inválidos.";
                if (ingreso.Monto <= 0) return "Error: El monto debe ser mayor a cero.";
                if (string.IsNullOrWhiteSpace(ingreso.Categoria) || string.IsNullOrWhiteSpace(ingreso.Descripcion))
                    return "Error: Todos los campos son obligatorios.";

                using (var db = new Context())
                {
                    ingreso.Fecha = ingreso.Fecha == default ? DateTime.Now : ingreso.Fecha;
                    ingreso.FechaCreacion = DateTime.Now;

                    db.Ingresos.Add(ingreso);
                    db.SaveChanges();
                }
                return "Ingreso agregado exitosamente.";
            }
            catch (Exception ex)
            {
                return ex.InnerException != null
                    ? $"Error al agregar el ingreso: {ex.InnerException.Message}"
                    : $"Error al agregar el ingreso: {ex.Message}";
            }
        }

        // ------------------- EGRESOS -------------------
        public string AgregarEgreso(Egreso egreso)
        {
            try
            {
                if (egreso == null) return "Error: datos del egreso inválidos.";
                if (egreso.Monto <= 0) return "Error: El monto debe ser mayor a cero.";
                if (string.IsNullOrWhiteSpace(egreso.TipoGasto) || string.IsNullOrWhiteSpace(egreso.Descripcion))
                    return "Error: Todos los campos son obligatorios.";

                using (var db = new Context())
                {
                    egreso.Fecha = egreso.Fecha == default ? DateTime.Now : egreso.Fecha;
                    egreso.FechaCreacion = DateTime.Now;

                    db.Egresos.Add(egreso);
                    db.SaveChanges();
                }
                return "Egreso agregado exitosamente.";
            }
            catch (Exception ex)
            {
                return ex.InnerException != null
                    ? $"Error al agregar el egreso: {ex.InnerException.Message}"
                    : $"Error al agregar el egreso: {ex.Message}";
            }
        }

        // ------------------- RESUMEN DIARIO -------------------
        public ResumenDiarioViewModel ObtenerResumenDiario(DateTime fecha)
        {
            using (var db = new Context())
            {
                var f = fecha.Date;

                var totIngresos = db.Ingresos
                    .Where(i => DbFunctions.TruncateTime(i.Fecha) == f)
                    .Select(i => (decimal?)i.Monto).Sum() ?? 0m;

                var totEgresos = db.Egresos
                    .Where(e => DbFunctions.TruncateTime(e.Fecha) == f)
                    .Select(e => (decimal?)e.Monto).Sum() ?? 0m;

                return new ResumenDiarioViewModel
                {
                    Fecha = f,
                    TotalIngresos = totIngresos,
                    TotalEgresos = totEgresos,
                    Balance = totIngresos - totEgresos
                };
            }
        }
        // ------------------- CIERRE SEMANAL -------------------
        public string GenerarCierreSemanal(DateTime inicio, DateTime fin)
        {
            try
            {
                if ((fin.Date - inicio.Date).TotalDays != 6)
                    return "El rango debe ser exactamente de 7 días.";

                var resumen = ObtenerResumenPorRango(inicio.Date, fin.Date);

                using (var db = new Context())
                {
                    var cierre = new CierreSemanal
                    {
                        FechaInicio = inicio.Date,
                        FechaFin = fin.Date,
                        TotalIngresos = resumen.TotalIngresos,
                        TotalEgresos = resumen.TotalEgresos,
                        Balance = resumen.Balance,
                        FechaCreacion = DateTime.Now
                    };
                    db.CierresSemanales.Add(cierre);
                    db.SaveChanges();
                }

                return "Cierre semanal generado exitosamente.";
            }
            catch (Exception ex)
            {
                return ex.InnerException != null
                    ? $"Error al generar cierre: {ex.InnerException.Message}"
                    : $"Error al generar cierre: {ex.Message}";
            }
        }

        public CierreSemanal ObtenerCierreSemanal(DateTime inicio, DateTime fin)
        {
            using (var db = new Context())
            {
                return db.CierresSemanales
                         .FirstOrDefault(c => c.FechaInicio == inicio.Date && c.FechaFin == fin.Date);
            }
        }

        private ResumenDiarioViewModel ObtenerResumenPorRango(DateTime inicio, DateTime fin)
        {
            using (var db = new Context())
            {
                var ingresos = db.Ingresos
                    .Where(i => DbFunctions.TruncateTime(i.Fecha) >= inicio && DbFunctions.TruncateTime(i.Fecha) <= fin)
                    .ToList();

                var egresos = db.Egresos
                    .Where(e => DbFunctions.TruncateTime(e.Fecha) >= inicio && DbFunctions.TruncateTime(e.Fecha) <= fin)
                    .ToList();

                return new ResumenDiarioViewModel
                {
                    Fecha = inicio,
                    TotalIngresos = ingresos.Sum(i => i.Monto),
                    TotalEgresos = egresos.Sum(e => e.Monto),
                    Balance = ingresos.Sum(i => i.Monto) - egresos.Sum(e => e.Monto),

                };
            }
        }


        // ------------------- DESCARGAR CIERRE (Excel/PDF) -------------------
        public byte[] DescargarCierreSemanal(DateTime inicio, DateTime fin, string formato)
        {
            using (var db = new Context())
            {
                var fInicio = inicio.Date;
                var fFin = fin.Date;

                var ingresos = db.Ingresos
                    .Where(i => DbFunctions.TruncateTime(i.Fecha) >= fInicio && DbFunctions.TruncateTime(i.Fecha) <= fFin)
                    .Select(i => new { i.Fecha, Tipo = "Ingreso", Categoria = i.Categoria, i.Descripcion, i.Monto })
                    .ToList();

                var egresos = db.Egresos
                    .Where(e => DbFunctions.TruncateTime(e.Fecha) >= fInicio && DbFunctions.TruncateTime(e.Fecha) <= fFin)
                    .Select(e => new { e.Fecha, Tipo = "Egreso", Categoria = e.TipoGasto, e.Descripcion, e.Monto })
                    .ToList();

                var movimientos = ingresos.Cast<dynamic>().Concat(egresos.Cast<dynamic>())
                    .OrderBy(m => m.Fecha).ToList();

                decimal totalIngresos = ingresos.Sum(i => i.Monto);
                decimal totalEgresos = egresos.Sum(e => e.Monto);
                decimal balance = totalIngresos - totalEgresos;

                if (string.Equals(formato, "excel", StringComparison.OrdinalIgnoreCase))
                {
                    using (var package = new ExcelPackage())
                    {
                        // Hoja resumen
                        var ws = package.Workbook.Worksheets.Add("Resumen");
                        ws.Cells["A1"].Value = "Cierre Semanal";
                        ws.Cells["A2"].Value = "Fecha inicio:";
                        ws.Cells["B2"].Value = fInicio.ToString("yyyy-MM-dd");
                        ws.Cells["A3"].Value = "Fecha fin:";
                        ws.Cells["B3"].Value = fFin.ToString("yyyy-MM-dd");

                        ws.Cells["A5"].Value = "Total Ingresos";
                        ws.Cells["B5"].Value = totalIngresos;
                        ws.Cells["A6"].Value = "Total Egresos";
                        ws.Cells["B6"].Value = totalEgresos;
                        ws.Cells["A7"].Value = "Balance";
                        ws.Cells["B7"].Value = balance;
                        ws.Cells["B5:B7"].Style.Numberformat.Format = "#,##0.00";

                        // Hoja detalle
                        var wsDet = package.Workbook.Worksheets.Add("Detalle");
                        wsDet.Cells[1, 1].Value = "Fecha";
                        wsDet.Cells[1, 2].Value = "Tipo";
                        wsDet.Cells[1, 3].Value = "Categoría/TipoGasto";
                        wsDet.Cells[1, 4].Value = "Descripción";
                        wsDet.Cells[1, 5].Value = "Monto";

                        int row = 2;
                        foreach (var m in movimientos)
                        {
                            wsDet.Cells[row, 1].Value = m.Fecha.ToString("yyyy-MM-dd HH:mm");
                            wsDet.Cells[row, 2].Value = m.Tipo;
                            wsDet.Cells[row, 3].Value = m.Categoria ?? "";
                            wsDet.Cells[row, 4].Value = m.Descripcion ?? "";
                            wsDet.Cells[row, 5].Value = m.Monto;
                            row++;
                        }

                        wsDet.Cells[1, 1, row - 1, 5].AutoFitColumns();
                        wsDet.Cells[2, 5, row - 1, 5].Style.Numberformat.Format = "#,##0.00";

                        return package.GetAsByteArray();
                    }
                }
                else
                {
                    // PDF
                    using (var ms = new MemoryStream())
                    {
                        var doc = new Document(PageSize.A4, 36, 36, 36, 36);
                        var writer = PdfWriter.GetInstance(doc, ms);
                        doc.Open();

                        var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
                        var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);

                        doc.Add(new Paragraph("Cierre Semanal", titleFont));
                        doc.Add(new Paragraph($"Fecha inicio: {fInicio:yyyy-MM-dd}", normalFont));
                        doc.Add(new Paragraph($"Fecha fin: {fFin:yyyy-MM-dd}", normalFont));
                        doc.Add(new Paragraph(" "));

                        var tblTot = new PdfPTable(2) { WidthPercentage = 50f };
                        tblTot.AddCell(new PdfPCell(new Phrase("Total Ingresos", normalFont)));
                        tblTot.AddCell(new PdfPCell(new Phrase(totalIngresos.ToString("N2"), normalFont)));
                        tblTot.AddCell(new PdfPCell(new Phrase("Total Egresos", normalFont)));
                        tblTot.AddCell(new PdfPCell(new Phrase(totalEgresos.ToString("N2"), normalFont)));
                        tblTot.AddCell(new PdfPCell(new Phrase("Balance", normalFont)));
                        tblTot.AddCell(new PdfPCell(new Phrase(balance.ToString("N2"), normalFont)));
                        doc.Add(tblTot);

                        doc.Add(new Paragraph(" "));
                        doc.Add(new Paragraph("Detalle de movimientos", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12)));
                        doc.Add(new Paragraph(" "));

                        var table = new PdfPTable(5) { WidthPercentage = 100f };
                        table.SetWidths(new float[] { 15f, 10f, 20f, 40f, 15f });

                        void addHeader(string text)
                        {
                            var cell = new PdfPCell(new Phrase(text, normalFont)) { BackgroundColor = BaseColor.LIGHT_GRAY };
                            table.AddCell(cell);
                        }

                        addHeader("Fecha");
                        addHeader("Tipo");
                        addHeader("Categoría");
                        addHeader("Descripción");
                        addHeader("Monto");

                        foreach (var m in movimientos)
                        {
                            table.AddCell(new Phrase(m.Fecha.ToString("yyyy-MM-dd HH:mm"), normalFont));
                            table.AddCell(new Phrase(m.Tipo, normalFont));
                            table.AddCell(new Phrase(m.Categoria ?? "", normalFont));
                            table.AddCell(new Phrase(m.Descripcion ?? "", normalFont));
                            table.AddCell(new Phrase(m.Monto.ToString("N2"), normalFont));
                        }

                        doc.Add(table);
                        doc.Close();
                        writer.Close();

                        return ms.ToArray();
                    }
                }
            }
        }
    }
}
