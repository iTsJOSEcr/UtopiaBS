using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using UtopiaBS.Business.Services;
using UtopiaBS.Data;
using UtopiaBS.Entities;
using UtopiaBS.ViewModels;

namespace UtopiaBS.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class MembresiasController : Controller
    {
        // PANEL DE MEMBRESÍAS
        public ActionResult Panel()
        {
            using (var db = new Context())
            {
                var hoy = DateTime.Today;

                var data = db.Clientes
                    .Select(c => new ClienteMembresiaViewModel
                    {
                        IdCliente = c.IdCliente,
                        Nombre = c.Nombre,
                        Cedula = c.Cedula,

                        TipoMembresia = db.Membresias
                            .Where(m => m.IdCliente == c.IdCliente)
                            .OrderByDescending(m => m.FechaInicio)
                            .Select(m =>
                                m.IdTipoMembresia == 1 ? "Básica" :
                                m.IdTipoMembresia == 2 ? "Premium" :
                                m.IdTipoMembresia == 3 ? "VIP" : "Desconocida"
                            ).FirstOrDefault() ?? "Sin membresía",

                        Activa = db.Membresias.Any(m =>
                            m.IdCliente == c.IdCliente &&
                            (m.FechaFin == null || m.FechaFin >= hoy)),

                        Puntos = db.Membresias
                            .Where(m => m.IdCliente == c.IdCliente)
                            .OrderByDescending(m => m.FechaInicio)
                            .Select(m => m.PuntosAcumulados)
                            .FirstOrDefault(),

                        FechaInicio = db.Membresias
                            .Where(m => m.IdCliente == c.IdCliente)
                            .OrderByDescending(m => m.FechaInicio)
                            .Select(m => m.FechaInicio)
                            .FirstOrDefault(),

                        FechaFin = db.Membresias
                            .Where(m => m.IdCliente == c.IdCliente)
                            .OrderByDescending(m => m.FechaInicio)
                            .Select(m => m.FechaFin)
                            .FirstOrDefault()
                    })
                    .ToList();

                return View(data);
            }
        }
    
        // ASIGNAR
        public ActionResult Asignar(int? idCliente)
        {
            using (var db = new Context())
            {
                ViewBag.Clientes = db.Clientes.ToList();

                ViewBag.TiposMembresia = db.TipoMembresia
                    .Select(t => new SelectListItem
                    {
                        Value = t.IdTipoMembresia.ToString(),
                        Text = t.NombreTipo
                    })
                    .ToList();

                var model = new AsignarMembresiaViewModel();

                if (idCliente.HasValue)
                {
                    var ultima = db.Membresias
                        .Where(m => m.IdCliente == idCliente.Value)
                        .OrderByDescending(m => m.FechaInicio)
                        .FirstOrDefault();

                    if (ultima != null)
                    {
                        model.IdCliente = ultima.IdCliente;
                        model.IdTipoMembresia = ultima.IdTipoMembresia;
                        model.FechaInicio = ultima.FechaInicio;
                    }
                    else
                    {
                        model.IdCliente = idCliente.Value;
                        model.FechaInicio = DateTime.Today;
                    }
                }
                else
                {
                    model.FechaInicio = DateTime.Today;
                }

                return View(model);
            }
        }

        // CAMBIAR ESTADO
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CambiarEstado(int idCliente)
        {
            try
            {
                using (var db = new Context())
                {
                    var hoy = DateTime.Today;

                    var membresia = db.Membresias
                        .Where(m => m.IdCliente == idCliente)
                        .OrderByDescending(m => m.FechaInicio)
                        .FirstOrDefault();

                    if (membresia == null)
                    {
                        TempData["Error"] = "❌ El cliente no tiene membresía asignada.";
                        return RedirectToAction("Panel");
                    }

                    if (membresia.FechaFin == null || membresia.FechaFin >= hoy)
                    {
                        // Desactivar
                        membresia.FechaFin = hoy.AddDays(-1);
                        TempData["Success"] = "✅ Membresía desactivada correctamente.";
                    }
                    else
                    {
                        // Reactivar por 1 año
                        membresia.FechaFin = hoy.AddYears(1);
                        TempData["Success"] = "✅ Membresía reactivada correctamente.";
                    }

                    db.SaveChanges();
                }
            }
            catch
            {
                TempData["Error"] = "❌ Error al cambiar el estado de la membresía.";
            }

            return RedirectToAction("Panel");
        }

        // GUARDAR ASIGNACIÓN
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Asignar(AsignarMembresiaViewModel model)
        {
            if (!ModelState.IsValid || model.FechaInicio == DateTime.MinValue)
            {
                TempData["Error"] = "⚠️ Debe seleccionar cliente, tipo y una fecha válida.";
                return RedirectToAction("Asignar", new { idCliente = model.IdCliente });
            }

            try
            {
                using (var db = new Context())
                {
                    var hoy = DateTime.Today;

                    var cliente = db.Clientes.FirstOrDefault(c => c.IdCliente == model.IdCliente);
                    if (cliente == null)
                    {
                        TempData["Error"] = "❌ Cliente no encontrado.";
                        return RedirectToAction("Asignar");
                    }

                    // BUSCAR MEMBRESÍA ACTIVA
                    var membresiaActiva = db.Membresias
                        .Where(m => m.IdCliente == model.IdCliente &&
                                    (m.FechaFin == null || m.FechaFin >= hoy))
                        .OrderByDescending(m => m.FechaInicio)
                        .FirstOrDefault();

                    // CALCULAR FECHA FIN
                    DateTime fechaFin;
                    switch (model.IdTipoMembresia)
                    {
                        case 1: fechaFin = model.FechaInicio.AddYears(1); break;
                        case 2: fechaFin = model.FechaInicio.AddYears(2); break;
                        case 3: fechaFin = model.FechaInicio.AddYears(3); break;
                        default:
                            TempData["Error"] = "❌ Tipo de membresía inválido.";
                            return RedirectToAction("Asignar");
                    }

                    // SI EXISTE → UPDATE
                    if (membresiaActiva != null)
                    {
                        membresiaActiva.IdTipoMembresia = model.IdTipoMembresia;
                        membresiaActiva.FechaInicio = model.FechaInicio;
                        membresiaActiva.FechaFin = fechaFin;
                    }
                    else
                    {
                        //  SI NO EXISTE → INSERT
                        var nueva = new Membresia
                        {
                            IdCliente = model.IdCliente,
                            IdTipoMembresia = model.IdTipoMembresia,
                            FechaInicio = model.FechaInicio,
                            FechaFin = fechaFin,
                            PuntosAcumulados = 0
                        };

                        db.Membresias.Add(nueva);
                    }

                    //  REFLEJO EN CLIENTE
                    cliente.IdTipoMembresia = model.IdTipoMembresia;

                    db.SaveChanges();

                    TempData["Success"] = "✅ La membresía fue actualizada correctamente.";
                    return RedirectToAction("Panel");
                }
            }
            catch (Exception)
            {
                TempData["Error"] = "❌ Error al guardar la información.";
                return RedirectToAction("Asignar");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EnviarAlertasVencimiento()
        {
            try
            {
                using (var db = new Context())
                using (var dbIdentity = new ApplicationDbContext())
                {
                    var hoy = DateTime.Today;

                    var clientesConVencimiento = (
                        from m in db.Membresias
                        join c in db.Clientes on m.IdCliente equals c.IdCliente
                        where m.FechaFin.HasValue
                        && DbFunctions.DiffDays(hoy, m.FechaFin.Value) <= 7
                        && DbFunctions.DiffDays(hoy, m.FechaFin.Value) >= 0
                        select new
                        {
                            Cliente = c.Nombre,
                            UserId = c.IdUsuario,
                            FechaVencimiento = m.FechaFin.Value,
                            Puntos = m.PuntosAcumulados
                        }
                    ).ToList();

                    if (!clientesConVencimiento.Any())
                    {
                        TempData["Error"] = "ℹ No hay clientes con puntos próximos a vencer.";
                        return RedirectToAction("Panel");
                    }

                    int enviados = 0;
                    int errores = 0;

                    foreach (var item in clientesConVencimiento)
                    {
                        var usuario = dbIdentity.Users.FirstOrDefault(u => u.Id == item.UserId);

                        if (usuario == null || string.IsNullOrWhiteSpace(usuario.Email))
                        {
                            errores++;
                            continue;
                        }

                        string mensaje = $@"
                <h2>⚠️ Tus puntos están por vencer</h2>
                <p>Hola <strong>{item.Cliente}</strong>,</p>
                <p>Tienes <strong>{item.Puntos} puntos</strong> próximos a vencer.</p>
                <p><strong>Fecha de vencimiento:</strong> {item.FechaVencimiento:dd/MM/yyyy}</p>
                <p>Te recomendamos usarlos antes de que expiren.</p>
                <p>Utopía Beauty Salon</p>";

                        try
                        {
                            await EmailService.EnviarCorreoAsync(
                                usuario.Email,
                                "⚠️ Notificación de Puntos por Vencer - Utopía",
                                mensaje
                            );

                            enviados++;
                        }
                        catch
                        {
                            errores++;
                        }
                    }

                    TempData["Success"] = $"✅ Correos enviados: {enviados}. Errores: {errores}.";
                    return RedirectToAction("Panel");
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "❌ Error real al enviar las notificaciones: " + ex.Message;
                return RedirectToAction("Panel");
            }
        }

    }
}
