using iTextSharp.text;
using iTextSharp.text.pdf;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using UtopiaBS.Data;
using UtopiaBS.Entities;

namespace UtopiaBS.Business
{
    public class CitaService
    {
        public CitaService()
        {
            // Configurar licencia para uso no comercial
            ExcelPackage.License.SetNonCommercialPersonal("Utopia");
        }

        public List<Cita> ListarDisponibles(int? empleadoId = null, int? servicioId = null)
        {
            using (var db = new Context())
            {
                var query = db.Citas
                              .Include(c => c.Cliente)
                              .Include(c => c.Empleado)
                              .Include(c => c.Servicio)
                              .Where(c => c.IdEstadoCita == 4)
                              .AsQueryable();

                if (empleadoId.HasValue)
                    query = query.Where(c => c.IdEmpleado == empleadoId.Value);

                if (servicioId.HasValue)
                    query = query.Where(c => c.IdServicio == servicioId.Value);

                return query.OrderBy(c => c.FechaHora).ToList();
            }
        }

        public List<Cita> ListarPendientes(int? empleadoId = null, int? servicioId = null)
        {
            using (var db = new Context())
            {
                var query = db.Citas
                               .Include(c => c.Cliente)
                              .Include(c => c.Empleado)
                              .Include(c => c.Servicio)
                              .Where(c => c.IdEstadoCita == 1)
                              .AsQueryable();

                if (empleadoId.HasValue)
                    query = query.Where(c => c.IdEmpleado == empleadoId.Value);

                if (servicioId.HasValue)
                    query = query.Where(c => c.IdServicio == servicioId.Value);

                return query.OrderBy(c => c.FechaHora).ToList();
            }
        }

        public string ReservarCita(int idCita, int idCliente, int? idEmpleado, int? idServicio)
        {
            try
            {
                using (var db = new Context())
                {
                    var cita = db.Citas.Find(idCita);
                    if (cita == null)
                        return "No se encontró la cita.";


                    if (cita.IdEstadoCita != 4 && cita.IdEstadoCita != 1)
                        return "La cita ya no está disponible.";

                    cita.IdCliente = idCliente;
                    cita.IdEmpleado = idEmpleado;
                    cita.IdServicio = idServicio;
                    cita.IdEstadoCita = 1;

                    db.SaveChanges();
                    return "Cita reservada exitosamente. Pendiente de Confirmación";
                }
            }
            catch (Exception ex)
            {
                return $"Error al reservar la cita: {ex.Message}";
            }
        }
        public string AgregarCita(Cita nuevaCita)
        {
            try
            {
                using (var db = new Context())
                {
                    db.Citas.Add(nuevaCita);
                    db.SaveChanges();
                    return "Cita agregada correctamente.";
                }
            }
            catch (Exception ex)
            {
                return $"Error al agregar la cita: {ex.Message}";
            }
        }


        public string ConfirmarCita(int idCita)
        {
            using (var db = new Context())
            {
                var cita = db.Citas.Find(idCita);
                if (cita == null) return "Cita no encontrada.";

                cita.IdEstadoCita = 2;
                cita.FechaUltimoRecordatorio = DateTime.Now;
                db.SaveChanges();
                return "Cita confirmada correctamente.";
            }
        }

        public string CancelarCita(int idCita)
        {
            using (var db = new Context())
            {
                var cita = db.Citas.Find(idCita);
                if (cita == null) return "Cita no encontrada.";

                cita.IdEstadoCita = 4;
                cita.FechaUltimoRecordatorio = DateTime.Now;
                db.SaveChanges();
                return "Cita cancelada correctamente.";
            }
        }

        public List<Cita> ListarTodas(int? empleadoId = null, int? servicioId = null)
        {
            using (var db = new Context())
            {
                var query = db.Citas
                              .Include(c => c.Cliente)
                              .Include(c => c.Empleado)
                              .Include(c => c.Servicio)
                              .AsQueryable();

                if (empleadoId.HasValue)
                    query = query.Where(c => c.IdEmpleado == empleadoId.Value);

                if (servicioId.HasValue)
                    query = query.Where(c => c.IdServicio == servicioId.Value);

                return query.OrderBy(c => c.FechaHora).ToList();
            }
        }

        public string CambiarCita(int idCitaActual, int idNuevaCita)
        {
            using (var db = new Context())
            {
                var citaActual = db.Citas.Find(idCitaActual);
                var nuevaCita = db.Citas.Find(idNuevaCita);

                if (citaActual == null || nuevaCita == null)
                    return "No se encontraron las citas especificadas.";

                if (nuevaCita.IdEstadoCita != 4)
                    return "La nueva cita ya no está disponible.";


                int? clienteId = citaActual.IdCliente;


                citaActual.IdCliente = null;
                citaActual.IdEstadoCita = 4;


                nuevaCita.IdCliente = clienteId;
                nuevaCita.IdEstadoCita = 1;

                db.SaveChanges();
                return "Cita cambiada exitosamente.";
            }
        }

        public string EliminarCita(int idCita)
        {
            using (var db = new Context())
            {
                var cita = db.Citas.Find(idCita);
                if (cita == null) return "Cita no encontrada.";

                db.Citas.Remove(cita);
                db.SaveChanges();
                return "Cita eliminada correctamente.";
            }
        }

        public byte[] DescargarReporteCitas(DateTime inicio, DateTime fin, string formato)
        {
            using (var db = new Context())
            {
                var fInicio = inicio.Date;
                var fFin = fin.Date.AddDays(1);

                var citas = db.Citas
                    .Include(c => c.Empleado)
                    .Include(c => c.Servicio)
                    .Where(c => c.FechaHora >= fInicio && c.FechaHora < fFin)
                    .OrderBy(c => c.FechaHora)
                    .ToList();

                if (string.Equals(formato, "excel", StringComparison.OrdinalIgnoreCase))
                {
                    using (var package = new ExcelPackage())
                    {
                        var ws = package.Workbook.Worksheets.Add("Citas");

                        // ENCABEZADOS
                        ws.Cells[1, 1].Value = "Fecha y Hora";
                        ws.Cells[1, 2].Value = "Empleado";
                        ws.Cells[1, 3].Value = "Servicio";
                        ws.Cells[1, 4].Value = "Estado";
                        ws.Cells[1, 5].Value = "Observaciones";

                        // Estilo encabezado
                        using (var range = ws.Cells[1, 1, 1, 5])
                        {
                            range.Style.Font.Bold = true;
                            range.Style.Font.Color.SetColor(System.Drawing.Color.White);
                            range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                            range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(0, 102, 204)); // Azul elegante
                            range.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        }

                        int row = 2;
                        foreach (var c in citas)
                        {
                            ws.Cells[row, 1].Value = c.FechaHora;
                            ws.Cells[row, 1].Style.Numberformat.Format = "yyyy-MM-dd HH:mm";

                            ws.Cells[row, 2].Value = c.Empleado?.Nombre ?? "-";
                            ws.Cells[row, 3].Value = c.Servicio?.Nombre ?? "-";

                            string estado = c.IdEstadoCita == 1 ? "Pendiente" :
                                            c.IdEstadoCita == 2 ? "Confirmada" :
                                            c.IdEstadoCita == 3 ? "Cancelada" :
                                            "Disponible";

                            ws.Cells[row, 4].Value = estado;
                            ws.Cells[row, 5].Value = c.Observaciones ?? "";

                            row++;
                        }

                        // AutoFit
                        ws.Cells[1, 1, row - 1, 5].AutoFitColumns();

                        // Bordes en toda la tabla
                        using (var range = ws.Cells[1, 1, row - 1, 5])
                        {
                            range.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                            range.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                            range.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                            range.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                        }

                        // Filtro en encabezado
                        ws.Cells["A1:E1"].AutoFilter = true;

                        return package.GetAsByteArray();
                    }
                }
                else
                {
                    using (var ms = new MemoryStream())
                    {
                        var doc = new Document(PageSize.A4, 36, 36, 36, 36);
                        PdfWriter.GetInstance(doc, ms);
                        doc.Open();

                        var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
                        var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);

                        doc.Add(new Paragraph("Reporte de Citas", titleFont));
                        doc.Add(new Paragraph($"Desde: {fInicio:yyyy-MM-dd}  Hasta: {fin:yyyy-MM-dd}", normalFont));
                        doc.Add(new Paragraph(" "));

                        var table = new PdfPTable(5) { WidthPercentage = 100f };
                        table.SetWidths(new float[] { 20f, 20f, 20f, 20f, 20f });

                        void Header(string t) => table.AddCell(new PdfPCell(new Phrase(t, normalFont)) { BackgroundColor = BaseColor.LIGHT_GRAY });

                        Header("Fecha y Hora");
                        Header("Empleado");
                        Header("Servicio");
                        Header("Estado");
                        Header("Observaciones");

                        foreach (var c in citas)
                        {
                            string estado = c.IdEstadoCita == 1 ? "Pendiente" :
                                            c.IdEstadoCita == 2 ? "Confirmada" :
                                            c.IdEstadoCita == 3 ? "Cancelada" :
                                            "Disponible";

                            table.AddCell(new Phrase(c.FechaHora.ToString("yyyy-MM-dd HH:mm")));
                            table.AddCell(new Phrase(c.Empleado?.Nombre ?? "-"));
                            table.AddCell(new Phrase(c.Servicio?.Nombre ?? "-"));
                            table.AddCell(new Phrase(estado));
                            table.AddCell(new Phrase(c.Observaciones ?? ""));
                        }

                        doc.Add(table);
                        doc.Close();

                        return ms.ToArray();
                    }
                }
            }
        }

    }
}