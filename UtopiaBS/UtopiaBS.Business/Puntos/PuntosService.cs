using System;
using System.Linq;
using UtopiaBS.Data;
using UtopiaBS.Entities;

namespace UtopiaBS.Business.Puntos
{
    public class PuntosService
    {
        private const decimal MONTO_MINIMO = 5000m; // Ajustable cuando querás

        public ResultadoPuntos AcumularPuntosPorVenta(int idCliente, int idVenta, decimal montoVenta)
        {
            var resultado = new ResultadoPuntos();

            try
            {
                using (var db = new Context())
                {
                    // ===============================
                    // ESCENARIO 2 → MEMBRESÍA ACTIVA
                    // ===============================
                    var membresia = db.Membresias
                        .FirstOrDefault(m =>
                            m.IdCliente == idCliente &&
                            (m.FechaFin == null || m.FechaFin >= DateTime.Today));

                    if (membresia == null)
                    {
                        resultado.Exito = false;
                        resultado.Mensaje = "No se pueden acumular puntos sin una membresía activa.";
                        resultado.PuntosSumados = 0;
                        return resultado;
                    }

                    // ==================================
                    // ESCENARIO 6 → VENTA DUPLICADA
                    // ==================================
                    bool yaExiste = db.PuntosCliente.Any(p => p.IdVenta == idVenta);
                    if (yaExiste)
                    {
                        resultado.Exito = false;
                        resultado.Mensaje = "Esta venta ya fue registrada para acumulación de puntos.";
                        resultado.PuntosSumados = 0;
                        return resultado;
                    }

                    // ==================================
                    // ESCENARIO 5 → MONTO MÍNIMO
                    // ==================================
                    if (montoVenta < MONTO_MINIMO)
                    {
                        resultado.Exito = false;
                        resultado.Mensaje = "El monto de la venta no permite acumular puntos.";
                        resultado.PuntosSumados = 0;
                        return resultado;
                    }

                    // ==================================
                    // ESCENARIO 1 → ACUMULACIÓN BÁSICA
                    // (Luego pasamos a por servicio)
                    // ==================================
                    int puntosCalculados = (int)(montoVenta / 1000);
                    // ejemplo: 1 punto por cada ₡1000

                    if (puntosCalculados <= 0)
                    {
                        resultado.Exito = false;
                        resultado.Mensaje = "No se generaron puntos para esta venta.";
                        resultado.PuntosSumados = 0;
                        return resultado;
                    }

                    // INSERT EN PUNTOSCLIENTE
                    var puntos = new PuntosCliente
                    {
                        IdCliente = idCliente,
                        IdVenta = idVenta,
                        Puntos = puntosCalculados,
                        FechaRegistro = DateTime.Now
                    };

                    db.PuntosCliente.Add(puntos);

                    // UPDATE EN MEMBRESIAS
                    membresia.PuntosAcumulados += puntosCalculados;

                    db.SaveChanges();

                    resultado.Exito = true;
                    resultado.Mensaje = "Puntos acumulados correctamente.";
                    resultado.PuntosSumados = puntosCalculados;
                    return resultado;
                }
            }
            catch (Exception)
            {
                // ==================================
                // ESCENARIO 7 → ERROR DEL SISTEMA
                // ==================================
                resultado.Exito = false;
                resultado.Mensaje = "No se pudieron acumular los puntos por un error de conexión o del sistema.";
                resultado.PuntosSumados = 0;
                return resultado;
            }
        }
    }
}
