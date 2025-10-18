using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UtopiaBS.Data;     
using UtopiaBS.Entities;
using UtopiaBS.Entities.Contabilidad;

namespace UtopiaBS.Business.Contabilidad
{
    public class ContabilidadService
    {
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
                return ex.InnerException != null ? $"Error al agregar el ingreso: {ex.InnerException.Message}" : $"Error al agregar el ingreso: {ex.Message}";
            }
        }

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
                return ex.InnerException != null ? $"Error al agregar el egreso: {ex.InnerException.Message}" : $"Error al agregar el egreso: {ex.Message}";
            }
        }

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
                return ex.InnerException != null ? $"Error al generar cierre: {ex.InnerException.Message}" : $"Error al generar cierre: {ex.Message}";
            }
        }

        public byte[] DescargarCierreSemanal(DateTime inicio, DateTime fin, string formato)
        {
            using (var db = new Context())
            {
                // Obtener totales de ingresos y egresos en el rango
                var totIngresos = db.Ingresos
                    .Where(i => DbFunctions.TruncateTime(i.Fecha) >= inicio && DbFunctions.TruncateTime(i.Fecha) <= fin)
                    .Select(i => (decimal?)i.Monto).Sum() ?? 0m;

                var totEgresos = db.Egresos
                    .Where(e => DbFunctions.TruncateTime(e.Fecha) >= inicio && DbFunctions.TruncateTime(e.Fecha) <= fin)
                    .Select(e => (decimal?)e.Monto).Sum() ?? 0m;

                var balance = totIngresos - totEgresos;

                // Por simplicidad, devolvemos un archivo en blanco (luego lo reemplazás con Excel/PDF real)
                return new byte[0];
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
                var totIngresos = db.Ingresos
                    .Where(i => DbFunctions.TruncateTime(i.Fecha) >= inicio && DbFunctions.TruncateTime(i.Fecha) <= fin)
                    .Select(i => (decimal?)i.Monto).Sum() ?? 0m;

                var totEgresos = db.Egresos
                    .Where(e => DbFunctions.TruncateTime(e.Fecha) >= inicio && DbFunctions.TruncateTime(e.Fecha) <= fin)
                    .Select(e => (decimal?)e.Monto).Sum() ?? 0m;

                return new ResumenDiarioViewModel
                {
                    Fecha = inicio,
                    TotalIngresos = totIngresos,
                    TotalEgresos = totEgresos,
                    Balance = totIngresos - totEgresos
                };
            }
        }
    }
}
