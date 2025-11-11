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

        public byte[] DescargarReporteCitas(DateTime inicio, DateTime fin, string formato, string profesionalNombre = null)
        {
            using (var db = new Context())
            {
                var fInicio = inicio.Date;
                var fFin = fin.Date.AddDays(1);

                // ====== OBTENER CITAS ======
                var citas = db.Citas
                    .Include(c => c.Empleado)
                    .Include(c => c.Servicio)
                    .Where(c => c.FechaHora >= fInicio && c.FechaHora < fFin)
                    .OrderBy(c => c.FechaHora)
                    .ToList();

                // === FILTRO ADICIONAL SI SE SELECCIONÓ UN PROFESIONAL ===
                if (!string.IsNullOrEmpty(profesionalNombre))
                {
                    citas = citas
                        .Where(c => c.Empleado != null && c.Empleado.Nombre == profesionalNombre)
                        .ToList();
                }

                // ====== ESTADÍSTICAS ======
                var totalAgendadas = citas.Count(c => c.IdEstadoCita == 1);
                var totalCompletadas = citas.Count(c => c.IdEstadoCita == 2);
                var totalCanceladas = citas.Count(c => c.IdEstadoCita == 3);
                var totalDisponibles = citas.Count(c => c.IdEstadoCita == 4);

                var porProfesional = citas
                    .Where(c => c.Empleado != null)
                    .GroupBy(c => c.Empleado.Nombre)
                    .Select(g => new { Nombre = g.Key, Total = g.Count() })
                    .OrderByDescending(x => x.Total)
                    .ToList();

                var porHorario = citas
                    .GroupBy(c =>
                    {
                        int hora = c.FechaHora.Hour;
                        if (hora < 12) return "Mañana (6am-11am)";
                        else if (hora < 18) return "Tarde (12pm-5pm)";
                        else return "Noche (6pm-10pm)";
                    })
                    .Select(g => new { Franja = g.Key, Total = g.Count() })
                    .OrderByDescending(x => x.Total)
                    .ToList();

                // ===========================
                //        EXCEL
                // ===========================
                if (string.Equals(formato, "excel", StringComparison.OrdinalIgnoreCase))
                {
                    using (var package = new ExcelPackage())
                    {
                        var ws = package.Workbook.Worksheets.Add("Reporte de Citas");

                        int row = 1;

                        // ======= ENCABEZADO =======
                        ws.Cells[row++, 1].Value = "Reporte de Citas";
                        ws.Cells[row - 1, 1].Style.Font.Bold = true;
                        ws.Cells[row - 1, 1].Style.Font.Size = 16;

                        ws.Cells[row++, 1].Value = $"Período: {inicio:yyyy-MM-dd} al {fin:yyyy-MM-dd}";

                        // 👇 FILTRO POR PROFESIONAL
                        if (!string.IsNullOrEmpty(profesionalNombre))
                        {
                            ws.Cells[row++, 1].Value = $"Filtrado por profesional: {profesionalNombre}";
                            ws.Cells[row - 1, 1].Style.Font.Italic = true;
                            ws.Cells[row - 1, 1].Style.Font.Color.SetColor(System.Drawing.Color.DarkSlateGray);
                        }

                        row++;

                        // ======= RESUMEN GENERAL =======
                        ws.Cells[row++, 1].Value = "Resumen General";
                        ws.Cells[row - 1, 1].Style.Font.Bold = true;
                        ws.Cells[row++, 1].Value = "Agendadas"; ws.Cells[row - 1, 2].Value = totalAgendadas;
                        ws.Cells[row++, 1].Value = "Completadas"; ws.Cells[row - 1, 2].Value = totalCompletadas;
                        ws.Cells[row++, 1].Value = "Canceladas"; ws.Cells[row - 1, 2].Value = totalCanceladas;
                        ws.Cells[row++, 1].Value = "Disponibles"; ws.Cells[row - 1, 2].Value = totalDisponibles;
                        row += 2;

                        // ======= CITAS POR PROFESIONAL =======
                        if (porProfesional.Any())
                        {
                            ws.Cells[row++, 1].Value = "Citas por Profesional";
                            ws.Cells[row - 1, 1].Style.Font.Bold = true;
                            ws.Cells[row, 1].Value = "Profesional"; ws.Cells[row, 2].Value = "Total de Citas";
                            ws.Cells[row, 1, row, 2].Style.Font.Bold = true;
                            row++;

                            foreach (var p in porProfesional)
                            {
                                ws.Cells[row, 1].Value = p.Nombre;
                                ws.Cells[row, 2].Value = p.Total;
                                row++;
                            }
                            row += 2;
                        }

                        // ======= CITAS POR HORARIO =======
                        if (porHorario.Any())
                        {
                            ws.Cells[row++, 1].Value = "Citas por Horario";
                            ws.Cells[row - 1, 1].Style.Font.Bold = true;
                            ws.Cells[row, 1].Value = "Franja Horaria"; ws.Cells[row, 2].Value = "Total de Citas";
                            ws.Cells[row, 1, row, 2].Style.Font.Bold = true;
                            row++;

                            foreach (var h in porHorario)
                            {
                                ws.Cells[row, 1].Value = h.Franja;
                                ws.Cells[row, 2].Value = h.Total;
                                row++;
                            }
                            row += 2;
                        }

                        // ======= LISTADO DE CITAS =======
                        ws.Cells[row++, 1].Value = "Listado de Citas";
                        ws.Cells[row - 1, 1].Style.Font.Bold = true;

                        string[] headers = { "Fecha y Hora", "Empleado", "Servicio", "Estado", "Observaciones" };
                        for (int i = 0; i < headers.Length; i++)
                        {
                            ws.Cells[row, i + 1].Value = headers[i];
                        }

                        using (var range = ws.Cells[row, 1, row, 5])
                        {
                            range.Style.Font.Bold = true;
                            range.Style.Font.Color.SetColor(System.Drawing.Color.White);
                            range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                            range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(0, 102, 204));
                            range.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        }

                        row++;

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

                        ws.Cells[1, 1, row - 1, 5].AutoFitColumns();
                        return package.GetAsByteArray();
                    }
                }

                // ===========================
                //           PDF
                // ===========================
                else
                {
                    using (var ms = new MemoryStream())
                    {
                        var doc = new Document(PageSize.A4, 40, 40, 50, 50);
                        PdfWriter.GetInstance(doc, ms);
                        doc.Open();

                        // ===== FUENTES =====
                        var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16, BaseColor.BLACK);
                        var sectionFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BaseColor.BLACK);
                        var tableHeaderFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.WHITE);
                        var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.BLACK);

                        // ===== LOGO =====
                        try
                        {
                            string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content", "assets", "utopia_logo.png");
                            if (File.Exists(logoPath))
                            {
                                iTextSharp.text.Image logo = iTextSharp.text.Image.GetInstance(logoPath);
                                logo.ScaleAbsolute(80f, 35f);
                                logo.Alignment = Element.ALIGN_LEFT;
                                doc.Add(logo);
                            }
                        }
                        catch { }

                        // ===== TÍTULO Y PERÍODO =====
                        doc.Add(new Paragraph("Reporte de Citas - Utopia Beauty Salon", titleFont)
                        {
                            Alignment = Element.ALIGN_LEFT,
                            SpacingAfter = 8f
                        });

                        doc.Add(new Paragraph($"Período: {inicio:yyyy-MM-dd} al {fin:yyyy-MM-dd}", normalFont)
                        {
                            Alignment = Element.ALIGN_LEFT,
                            SpacingAfter = 10f
                        });

                        // 👇 FILTRO POR PROFESIONAL
                        if (!string.IsNullOrEmpty(profesionalNombre))
                        {
                            doc.Add(new Paragraph($"Filtrado por profesional: {profesionalNombre}", normalFont)
                            {
                                Alignment = Element.ALIGN_LEFT,
                                SpacingAfter = 15f
                            });
                        }

                        // ===== SECCIÓN: RESUMEN =====
                        doc.Add(new Paragraph("Resumen General", sectionFont) { SpacingBefore = 5f, SpacingAfter = 8f });

                        PdfPTable resumen = new PdfPTable(2)
                        {
                            WidthPercentage = 50,
                            HorizontalAlignment = Element.ALIGN_LEFT,
                            SpacingAfter = 15f
                        };
                        resumen.SetWidths(new float[] { 2f, 1f });

                        void AddHeader(PdfPTable t, string text)
                        {
                            var cell = new PdfPCell(new Phrase(text, tableHeaderFont))
                            {
                                BackgroundColor = new BaseColor(60, 60, 60),
                                HorizontalAlignment = Element.ALIGN_CENTER,
                                Padding = 5
                            };
                            t.AddCell(cell);
                        }

                        void AddCell(PdfPTable t, string text)
                        {
                            var cell = new PdfPCell(new Phrase(text, normalFont))
                            {
                                Padding = 5,
                                HorizontalAlignment = Element.ALIGN_CENTER
                            };
                            t.AddCell(cell);
                        }

                        AddHeader(resumen, "Estado");
                        AddHeader(resumen, "Total");

                        AddCell(resumen, "Agendadas"); AddCell(resumen, totalAgendadas.ToString());
                        AddCell(resumen, "Completadas"); AddCell(resumen, totalCompletadas.ToString());
                        AddCell(resumen, "Canceladas"); AddCell(resumen, totalCanceladas.ToString());
                        AddCell(resumen, "Disponibles"); AddCell(resumen, totalDisponibles.ToString());

                        doc.Add(resumen);

                        // ===== SECCIÓN: POR PROFESIONAL =====
                        if (porProfesional.Any())
                        {
                            doc.Add(new Paragraph("Citas por Profesional", sectionFont) { SpacingBefore = 5f, SpacingAfter = 8f });
                            PdfPTable tablaProf = new PdfPTable(2)
                            {
                                WidthPercentage = 70,
                                HorizontalAlignment = Element.ALIGN_LEFT,
                                SpacingAfter = 15f
                            };
                            tablaProf.SetWidths(new float[] { 3f, 1f });
                            AddHeader(tablaProf, "Profesional");
                            AddHeader(tablaProf, "Total de Citas");

                            foreach (var p in porProfesional)
                            {
                                AddCell(tablaProf, p.Nombre);
                                AddCell(tablaProf, p.Total.ToString());
                            }

                            doc.Add(tablaProf);
                        }

                        // ===== SECCIÓN: POR HORARIO =====
                        if (porHorario.Any())
                        {
                            doc.Add(new Paragraph("Citas por Horario", sectionFont) { SpacingBefore = 5f, SpacingAfter = 8f });

                            PdfPTable tablaHorario = new PdfPTable(2)
                            {
                                WidthPercentage = 70,
                                HorizontalAlignment = Element.ALIGN_LEFT,
                                SpacingAfter = 15f
                            };
                            tablaHorario.SetWidths(new float[] { 3f, 1f });
                            AddHeader(tablaHorario, "Franja Horaria");
                            AddHeader(tablaHorario, "Total de Citas");

                            foreach (var h in porHorario)
                            {
                                AddCell(tablaHorario, h.Franja);
                                AddCell(tablaHorario, h.Total.ToString());
                            }

                            doc.Add(tablaHorario);
                        }

                        // ===== SECCIÓN: LISTADO =====
                        doc.Add(new Paragraph("Listado de Citas", sectionFont) { SpacingBefore = 5f, SpacingAfter = 10f });
                        PdfPTable table = new PdfPTable(5)
                        {
                            WidthPercentage = 100,
                            SpacingBefore = 10f
                        };
                        table.SetWidths(new float[] { 20f, 20f, 20f, 15f, 25f });

                        string[] headers = { "Fecha y Hora", "Empleado", "Servicio", "Estado", "Observaciones" };
                        foreach (var h in headers)
                        {
                            var headerCell = new PdfPCell(new Phrase(h, tableHeaderFont))
                            {
                                BackgroundColor = new BaseColor(60, 60, 60),
                                HorizontalAlignment = Element.ALIGN_CENTER,
                                Padding = 5
                            };
                            table.AddCell(headerCell);
                        }

                        foreach (var c in citas)
                        {
                            string estado = c.IdEstadoCita == 1 ? "Pendiente" :
                                            c.IdEstadoCita == 2 ? "Confirmada" :
                                            c.IdEstadoCita == 3 ? "Cancelada" :
                                            "Disponible";

                            table.AddCell(new PdfPCell(new Phrase(c.FechaHora.ToString("yyyy-MM-dd HH:mm"), normalFont)) { Padding = 5 });
                            table.AddCell(new PdfPCell(new Phrase(c.Empleado?.Nombre ?? "-", normalFont)) { Padding = 5 });
                            table.AddCell(new PdfPCell(new Phrase(c.Servicio?.Nombre ?? "-", normalFont)) { Padding = 5 });
                            table.AddCell(new PdfPCell(new Phrase(estado, normalFont)) { Padding = 5 });
                            table.AddCell(new PdfPCell(new Phrase(c.Observaciones ?? "", normalFont)) { Padding = 5 });
                        }

                        doc.Add(table);
                        doc.Close();

                        return ms.ToArray();
                    }
                }
            }
        }

        public class EstadisticasCitasDTO
        {
            public int TotalAgendadas { get; set; }
            public int TotalCanceladas { get; set; }
            public int TotalCompletadas { get; set; }
            public int TotalDisponibles { get; set; }

            public List<ProfesionalEstadistica> PorProfesional { get; set; }
            public List<HorarioEstadistica> PorHorario { get; set; }
        }

        public class ProfesionalEstadistica
        {
            public string Profesional { get; set; }
            public int TotalCitas { get; set; }
        }

        public class HorarioEstadistica
        {
            public string Franja { get; set; }
            public int Total { get; set; }
        }
        public EstadisticasCitasDTO ObtenerEstadisticas(DateTime inicio, DateTime fin, string profesionalNombre = null)
        {
            using (var db = new Context())
            {
                var fInicio = inicio.Date;
                var fFin = fin.Date.AddDays(1);

                var citas = db.Citas
                    .Include(c => c.Empleado)
                    .Where(c => c.FechaHora >= fInicio && c.FechaHora < fFin)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(profesionalNombre))
                    citas = citas.Where(c => c.Empleado.Nombre == profesionalNombre);

                var lista = citas.ToList();

                var estadisticas = new EstadisticasCitasDTO
                {
                    TotalAgendadas = lista.Count(c => c.IdEstadoCita == 1),
                    TotalCompletadas = lista.Count(c => c.IdEstadoCita == 2),
                    TotalCanceladas = lista.Count(c => c.IdEstadoCita == 3),
                    TotalDisponibles = lista.Count(c => c.IdEstadoCita == 4),

                    PorProfesional = lista
                        .Where(c => c.Empleado != null)
                        .GroupBy(c => c.Empleado.Nombre)
                        .Select(g => new ProfesionalEstadistica
                        {
                            Profesional = g.Key,
                            TotalCitas = g.Count()
                        }).ToList(),

                    PorHorario = lista
                        .GroupBy(c =>
                        {
                            int hora = c.FechaHora.Hour;
                            if (hora < 12) return "Mañana (6am-11am)";
                            else if (hora < 18) return "Tarde (12pm-5pm)";
                            else return "Noche (6pm-10pm)";
                        })
                        .Select(g => new HorarioEstadistica
                        {
                            Franja = g.Key,
                            Total = g.Count()
                        }).ToList()
                };

                return estadisticas;
            }
        }
    }
}