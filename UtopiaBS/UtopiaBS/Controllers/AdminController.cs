using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using System;
using System.Linq;
using System.Web.Mvc;
using UtopiaBS.Data;
using UtopiaBS.Entities;

namespace UtopiaBS.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class AdminController : Controller
    {
        private readonly UserManager<UsuarioDA> _userManager;

        public AdminController()
        {
            var db = new ApplicationDbContext();
            _userManager = new UserManager<UsuarioDA>(new UserStore<UsuarioDA>(db));
            _userManager.UserTokenProvider =
                new DataProtectorTokenProvider<UsuarioDA>(
                    new Microsoft.Owin.Security.DataProtection.DpapiDataProtectionProvider("UtopiaBS")
                    .Create("Identity"))
                {
                    TokenLifespan = TimeSpan.FromHours(2)
                };
        }

        // LISTAR USUARIOS
        public ActionResult GestionUsuarios()
        {
            var usuarios = _userManager.Users.ToList();
            return View(usuarios);
        }

        // ASIGNAR / CAMBIAR ROL
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AsignarRol(string userId, string rolName)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(rolName))
            {
                TempData["Error"] = "Debe seleccionar un usuario y un rol.";
                return RedirectToAction("GestionUsuarios");
            }

            var usuario = _userManager.FindById(userId);
            if (usuario == null)
            {
                TempData["Error"] = "No se encontró el usuario.";
                return RedirectToAction("GestionUsuarios");
            }

            // remover roles anteriores
            var rolesActuales = _userManager.GetRoles(userId).ToArray();
            if (rolesActuales.Any())
                _userManager.RemoveFromRoles(userId, rolesActuales);

            var result = _userManager.AddToRole(userId, rolName);

            if (result.Succeeded)
                TempData["Success"] = $"El rol '{rolName}' se asignó correctamente.";
            else
                TempData["Error"] = "No se pudo asignar el rol.";

            return RedirectToAction("GestionUsuarios");
        }

        // ✅ ACTIVAR / DESACTIVAR USUARIO (con regla de 7 días)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleEstado(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "Debe seleccionar un usuario.";
                return RedirectToAction("GestionUsuarios");
            }

            try
            {
                var user = _userManager.FindById(userId);

                if (user == null)
                {
                    // Escenario 3: no existe
                    TempData["Error"] = "La cuenta no existe.";
                    return RedirectToAction("GestionUsuarios");
                }

                if (user.Activo)
                {
                    // 👉 Quiere DESACTIVAR

                    // Escenario 4: solo se puede desactivar si han pasado 7 días desde la última activación
                    if (user.FechaUltimaActivacion.HasValue &&
                        user.FechaUltimaActivacion.Value.AddDays(7) > DateTime.Now)
                    {
                        TempData["Error"] =
                            "La cuenta no se puede desactivar ya que tiene menos de 7 días de haberse activado o vuelto a activar.";
                        return RedirectToAction("GestionUsuarios");
                    }

                    user.Activo = false;

                    // también bloqueamos por lockout
                    user.LockoutEnabled = true;
                    user.LockoutEndDateUtc = DateTime.UtcNow.AddYears(10);

                    // Escenario 2
                    TempData["Success"] = "La cuenta se desactivó temporalmente exitosamente.";
                }
                else
                {
                    // 👉 Quiere ACTIVAR

                    user.Activo = true;
                    user.FechaUltimaActivacion = DateTime.Now;

                    // Quitamos lockout para que pueda entrar
                    user.LockoutEndDateUtc = null;

                    TempData["Success"] = "La cuenta se activó correctamente.";
                }

                _userManager.Update(user);
            }
            catch
            {
                TempData["Error"] = "Ocurrió un error al cambiar el estado de la cuenta.";
            }

            return RedirectToAction("GestionUsuarios");
        }

        // ✅ ELIMINAR CUENTA (escenarios 1, 3 y 5)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EliminarUsuario(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "Debe seleccionar un usuario.";
                return RedirectToAction("GestionUsuarios");
            }

            try
            {
                var user = _userManager.FindById(userId);

                if (user == null)
                {
                    // Escenario 3: no existe
                    TempData["Error"] = "La cuenta no existe.";
                    return RedirectToAction("GestionUsuarios");
                }

                // 🔹 Primero eliminar datos relacionados en tu BD de negocio
                using (var db = new Context())
                {
                    // 1) Historial de actividad
                    var actividades = db.UsuarioActividad
                        .Where(a => a.UserId == user.Id)
                        .ToList();
                    if (actividades.Any())
                    {
                        db.UsuarioActividad.RemoveRange(actividades);
                        db.SaveChanges();
                    }

                    // 2) Cliente ligado a este usuario
                    var cliente = db.Clientes.FirstOrDefault(c => c.IdUsuario == user.Id);

                    if (cliente != null)
                    {
                        var idCliente = cliente.IdCliente;

                        // 2.1) Puntos del cliente
                        var puntos = db.PuntosCliente
                            .Where(p => p.IdCliente == idCliente)
                            .ToList();
                        if (puntos.Any())
                        {
                            db.PuntosCliente.RemoveRange(puntos);
                            db.SaveChanges();
                        }

                        // 2.2) Membresías del cliente
                        var membresias = db.Membresias
                            .Where(m => m.IdCliente == idCliente)
                            .ToList();
                        if (membresias.Any())
                        {
                            db.Membresias.RemoveRange(membresias);
                            db.SaveChanges();
                        }

                        // 2.3) Eliminar el cliente
                        db.Clientes.Remove(cliente);
                        db.SaveChanges();
                    }
                }

                // 🔹 Ahora sí, eliminar el usuario de Identity
                var result = _userManager.Delete(user);

                if (result.Succeeded)
                {
                    // Escenario 1: eliminada correctamente
                    TempData["Success"] = "La cuenta se eliminó exitosamente.";
                }
                else
                {
                    TempData["Error"] = "No se pudo eliminar la cuenta.";
                }
            }
            catch
            {
                // Escenario 5: error de conexión (para el enunciado de la historia)
                TempData["Error"] = "Hubo un error de conexión al intentar eliminar la cuenta.";
            }

            return RedirectToAction("GestionUsuarios");
        }

        // RESET PASSWORD (Administrador)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(string userId)
        {
            try
            {
                var user = _userManager.FindById(userId);

                if (user == null)
                {
                    TempData["Error"] = "El usuario seleccionado no existe.";
                    return RedirectToAction("GestionUsuarios");
                }

                // 1. Generar token oficial de reseteo
                var token = _userManager.GeneratePasswordResetToken(userId);

                // 2. Generar contraseña temporal aleatoria
                string tempPassword = "Utopia" + new Random().Next(1000, 9999) + "*";

                // 3. Aplicar reset con la nueva contraseña
                var result = _userManager.ResetPassword(userId, token, tempPassword);

                if (result.Succeeded)
                {
                    TempData["Success"] =
                        "La contraseña ha sido restablecida correctamente. " +
                        "El usuario deberá cambiarla en su próximo inicio de sesión.";

                    TempData["TempPassword"] = tempPassword;
                }
                else
                {
                    TempData["Error"] = "No se pudo restablecer la contraseña.";
                }

                return RedirectToAction("GestionUsuarios");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Ocurrió un error al procesar la solicitud: " + ex.Message;
                return RedirectToAction("GestionUsuarios");
            }
        }

        // HISTORIAL DE ACTIVIDAD
        public ActionResult Actividad(string userId)
        {
            using (var db = new Context())
            {
                var registros = db.UsuarioActividad
                    .Where(a => a.UserId == userId)
                    .OrderByDescending(a => a.FechaInicio)
                    .ToList();

                ViewBag.Usuario = _userManager.FindById(userId);
                return View(registros);
            }
        }
    }
}
