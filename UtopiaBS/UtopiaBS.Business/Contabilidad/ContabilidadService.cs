using iTextSharp.text;           // iTextSharp
using iTextSharp.text.pdf;
using OfficeOpenXml;             // EPPlus
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using UtopiaBS.Data;
using UtopiaBS.Entities;
using UtopiaBS.Entities.Contabilidad;   // DTOs y entidades contables

namespace UtopiaBS.Business.Contabilidad
{
    public class ContabilidadService
    {
        public ContabilidadService()
        {
            // EPPlus 8+: licencia modo personal/no comercial
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

        // ------------------- RESUMEN MENSUAL (base) -------------------
        public ResumenMensualDto ObtenerResumenMensual(int year, int month)
        {
            var inicio = new DateTime(year, month, 1);
            var finExcl = inicio.AddMonths(1);

            using (var db = new Context())
            {
                var ingresosMes = db.Ingresos.Where(i => i.Fecha >= inicio && i.Fecha < finExcl);
                var egresosMes = db.Egresos.Where(e => e.Fecha >= inicio && e.Fecha < finExcl);

                var vm = new ResumenMensualDto
                {
                    Year = year,
                    Month = month,
                    TotalIngresos = ingresosMes.Any() ? ingresosMes.Sum(x => x.Monto) : 0m,
                    TotalEgresos = egresosMes.Any() ? egresosMes.Sum(x => x.Monto) : 0m,

                    IngresosPorDia = ingresosMes
                        .GroupBy(x => DbFunctions.TruncateTime(x.Fecha).Value)
                        .Select(g => new ItemDiaDto { Dia = g.Key, Monto = g.Sum(z => z.Monto) })
                        .OrderBy(x => x.Dia)
                        .ToList(),

                    EgresosPorDia = egresosMes
                        .GroupBy(x => DbFunctions.TruncateTime(x.Fecha).Value)
                        .Select(g => new ItemDiaDto { Dia = g.Key, Monto = g.Sum(z => z.Monto) })
                        .OrderBy(x => x.Dia)
                        .ToList(),

                    IngresosPorCategoria = ingresosMes
                        .GroupBy(x => x.Categoria)
                        .Select(g => new ItemCategoriaDto { Nombre = g.Key, Monto = g.Sum(z => z.Monto) })
                        .OrderByDescending(x => x.Monto)
                        .ToList(),

                    EgresosPorTipo = egresosMes
                        .GroupBy(x => x.TipoGasto)
                        .Select(g => new ItemCategoriaDto { Nombre = g.Key, Monto = g.Sum(z => z.Monto) })
                        .OrderByDescending(x => x.Monto)
                        .ToList(),
                };

                // inicializa campos de ventas en 0 (por si la vista los muestra)
                vm.TotalVentasProductos = 0m;
                vm.TotalVentasServicios = 0m;
                vm.Filtro = "todo";
                return vm;
            }
        }

        // ------------------- RESUMEN MENSUAL (con filtro Productos/Servicios) -------------------
        public ResumenMensualDto ObtenerResumenMensual(int year, int month, string filtro)
        {
            var vm = ObtenerResumenMensual(year, month);

            using (var db = new Context())
            {
                vm.TotalVentasProductos = SumarVentasProductosMes(db, year, month);
                vm.TotalVentasServicios = SumarVentasServiciosMes(db, year, month);
            }

            vm.Filtro = string.IsNullOrWhiteSpace(filtro) ? "todo" : filtro.ToLower();
            return vm;
        }

        // Helpers de ventas (DENTRO de la clase, privados)
        private decimal SumarVentasProductosMes(Context db, int year, int month)
        {
            var inicio = new DateTime(year, month, 1);
            var finExcl = inicio.AddMonths(1);

            // Si 'SubTotal' de la entidad es columna calculada en BD, puedes usarla directamente.
            // Si no, usamos Cantidad * PrecioUnitario.
            var query = from d in db.DetalleVentaProductos
                        join v in db.Ventas on d.IdVenta equals v.IdVenta
                        where v.FechaVenta >= inicio && v.FechaVenta < finExcl
                        select (decimal?)(d.Cantidad * d.PrecioUnitario);

            return query.Sum() ?? 0m;
        }

        private decimal SumarVentasServiciosMes(Context db, int year, int month)
        {
            var inicio = new DateTime(year, month, 1);
            var finExcl = inicio.AddMonths(1);

            var query = from d in db.DetalleVentaServicios
                        join v in db.Ventas on d.IdVenta equals v.IdVenta
                        where v.FechaVenta >= inicio && v.FechaVenta < finExcl
                        select (decimal?)(d.Cantidad * d.PrecioUnitario);

            return query.Sum() ?? 0m;
        }

        // ------------------- EXPORTAR RESUMEN MENSUAL (EXCEL) -------------------
        public byte[] ExportarResumenMensualExcel(int year, int month, string filtro = "todo")
        {
            // 1) Traemos el resumen mensual que ya tienes (ingresos/egresos/agrupados)
            var vm = ObtenerResumenMensual(year, month);

            var inicio = new DateTime(year, month, 1);
            var finExcl = inicio.AddMonths(1);

            // 2) Armamos las ventas del mes por TIPO via detalles (no existe TipoVenta en Venta)
            using (var db = new Context())
            {
                // Usar db.Set<T>() evita depender del nombre exacto del DbSet en tu Context
                var qProd = from d in db.Set<DetalleVentaProducto>()
                            join v in db.Set<Venta>() on d.IdVenta equals v.IdVenta
                            where v.FechaVenta >= inicio && v.FechaVenta < finExcl
                            select new
                            {
                                Fecha = v.FechaVenta,
                                Tipo = "Producto",
                                IdItem = d.IdProducto,
                                d.Cantidad,
                                d.PrecioUnitario,
                                Subtotal = (decimal?)(d.Cantidad * d.PrecioUnitario)
                            };

                var qServ = from d in db.Set<DetalleVentaServicio>()
                            join v in db.Set<Venta>() on d.IdVenta equals v.IdVenta
                            where v.FechaVenta >= inicio && v.FechaVenta < finExcl
                            select new
                            {
                                Fecha = v.FechaVenta,
                                Tipo = "Servicio",
                                IdItem = d.IdServicio,
                                d.Cantidad,
                                d.PrecioUnitario,
                                Subtotal = (decimal?)(d.Cantidad * d.PrecioUnitario)
                            };

                // Aplica el filtro
                List<dynamic> ventasSeleccionadas;
                var f = (filtro ?? "todo").ToLower();
                if (f == "productos")
                    ventasSeleccionadas = qProd.OrderBy(x => x.Fecha).ToList<dynamic>();
                else if (f == "servicios")
                    ventasSeleccionadas = qServ.OrderBy(x => x.Fecha).ToList<dynamic>();
                else
                    ventasSeleccionadas = qProd.ToList<dynamic>()
                                               .Concat(qServ.ToList<dynamic>())
                                               .OrderBy(x => x.Fecha)
                                               .ToList();

                decimal totalVentasSeleccionadas = ventasSeleccionadas.Sum(x => (decimal)(x.Subtotal ?? 0m));

                // 3) Construimos el Excel (con tus hojas existentes + hoja de Ventas filtradas)
                using (var package = new OfficeOpenXml.ExcelPackage())
                {
                    // ---------- Hoja 1: Resumen ----------
                    var ws = package.Workbook.Worksheets.Add("Resumen");
                    ws.Cells["A1"].Value = "Año";
                    ws.Cells["B1"].Value = "Mes";
                    ws.Cells["C1"].Value = "Ingresos (manuales)";
                    ws.Cells["D1"].Value = "Egresos";
                    ws.Cells["E1"].Value = "Balance";
                    ws.Cells["F1"].Value = "Filtro ventas";
                    ws.Cells["G1"].Value = "Total ventas (filtro)";

                    ws.Cells["A2"].Value = vm.Year;
                    ws.Cells["B2"].Value = vm.Month;
                    ws.Cells["C2"].Value = vm.TotalIngresos;
                    ws.Cells["D2"].Value = vm.TotalEgresos;
                    ws.Cells["E2"].Value = vm.Balance;
                    ws.Cells["F2"].Value = f.ToUpper();
                    ws.Cells["G2"].Value = totalVentasSeleccionadas;

                    ws.Cells["A1:G1"].Style.Font.Bold = true;
                    ws.Cells["C2:E2"].Style.Numberformat.Format = "#,##0.00";
                    ws.Cells["G2"].Style.Numberformat.Format = "#,##0.00";
                    ws.Cells.AutoFitColumns();

                    // ---------- Hoja 2: Ingresos x Día ----------
                    var wsi = package.Workbook.Worksheets.Add("Ingresos x Día");
                    wsi.Cells["A1"].Value = "Día";
                    wsi.Cells["B1"].Value = "Monto";
                    wsi.Cells["A1:B1"].Style.Font.Bold = true;
                    int r = 2;
                    foreach (var it in vm.IngresosPorDia)
                    {
                        wsi.Cells[r, 1].Value = it.Dia.ToShortDateString();
                        wsi.Cells[r, 2].Value = it.Monto;
                        r++;
                    }
                    wsi.Cells["B2:B" + (r - 1)].Style.Numberformat.Format = "#,##0.00";
                    wsi.Cells.AutoFitColumns();

                    // ---------- Hoja 3: Egresos x Día ----------
                    var wse = package.Workbook.Worksheets.Add("Egresos x Día");
                    wse.Cells["A1"].Value = "Día";
                    wse.Cells["B1"].Value = "Monto";
                    wse.Cells["A1:B1"].Style.Font.Bold = true;
                    r = 2;
                    foreach (var it in vm.EgresosPorDia)
                    {
                        wse.Cells[r, 1].Value = it.Dia.ToShortDateString();
                        wse.Cells[r, 2].Value = it.Monto;
                        r++;
                    }
                    wse.Cells["B2:B" + (r - 1)].Style.Numberformat.Format = "#,##0.00";
                    wse.Cells.AutoFitColumns();

                    // ---------- Hoja 4: Ingresos x Categoría ----------
                    var wsiCat = package.Workbook.Worksheets.Add("Ingresos x Categoría");
                    wsiCat.Cells["A1"].Value = "Categoría";
                    wsiCat.Cells["B1"].Value = "Monto";
                    wsiCat.Cells["A1:B1"].Style.Font.Bold = true;
                    r = 2;
                    foreach (var it in vm.IngresosPorCategoria)
                    {
                        wsiCat.Cells[r, 1].Value = it.Nombre;
                        wsiCat.Cells[r, 2].Value = it.Monto;
                        r++;
                    }
                    wsiCat.Cells["B2:B" + (r - 1)].Style.Numberformat.Format = "#,##0.00";
                    wsiCat.Cells.AutoFitColumns();

                    // ---------- Hoja 5: Egresos x Tipo ----------
                    var wseTipo = package.Workbook.Worksheets.Add("Egresos x Tipo");
                    wseTipo.Cells["A1"].Value = "Tipo";
                    wseTipo.Cells["B1"].Value = "Monto";
                    wseTipo.Cells["A1:B1"].Style.Font.Bold = true;
                    r = 2;
                    foreach (var it in vm.EgresosPorTipo)
                    {
                        wseTipo.Cells[r, 1].Value = it.Nombre;
                        wseTipo.Cells[r, 2].Value = it.Monto;
                        r++;
                    }
                    wseTipo.Cells["B2:B" + (r - 1)].Style.Numberformat.Format = "#,##0.00";
                    wseTipo.Cells.AutoFitColumns();

                    // ---------- Hoja 6: Ventas (según filtro) ----------
                    var wsV = package.Workbook.Worksheets.Add("Ventas (Filtro)");
                    wsV.Cells["A1"].Value = "Fecha";
                    wsV.Cells["B1"].Value = "Tipo";
                    wsV.Cells["C1"].Value = "Id Item";
                    wsV.Cells["D1"].Value = "Cantidad";
                    wsV.Cells["E1"].Value = "Precio Unitario";
                    wsV.Cells["F1"].Value = "Subtotal";
                    wsV.Cells["A1:F1"].Style.Font.Bold = true;

                    int rowV = 2;
                    foreach (var v in ventasSeleccionadas)
                    {
                        wsV.Cells[rowV, 1].Value = ((DateTime)v.Fecha).ToString("yyyy-MM-dd");
                        wsV.Cells[rowV, 2].Value = v.Tipo;
                        wsV.Cells[rowV, 3].Value = v.IdItem;
                        wsV.Cells[rowV, 4].Value = v.Cantidad;
                        wsV.Cells[rowV, 5].Value = v.PrecioUnitario;
                        wsV.Cells[rowV, 6].Value = (decimal)(v.Subtotal ?? 0m);
                        rowV++;
                    }

                    wsV.Cells["E2:E" + (rowV - 1)].Style.Numberformat.Format = "#,##0.00";
                    wsV.Cells["F2:F" + (rowV - 1)].Style.Numberformat.Format = "#,##0.00";
                    wsV.Cells.AutoFitColumns();

                    // Total al final
                    wsV.Cells[rowV + 1, 5].Value = "TOTAL";
                    wsV.Cells[rowV + 1, 6].Value = totalVentasSeleccionadas;
                    wsV.Cells[rowV + 1, 6].Style.Numberformat.Format = "#,##0.00";
                    wsV.Cells[rowV + 1, 5, rowV + 1, 6].Style.Font.Bold = true;

                    return package.GetAsByteArray();
                }
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

        // ------------------- DESCARGAR CIERRE (EXCEL/PDF) -------------------
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
