using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using System;
using System.Collections.Generic;
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

        // ACTIVAR / DESACTIVAR USUARIO (Lockout)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleEstado(string userId)
        {
            var user = _userManager.FindById(userId);

            if (user == null)
            {
                TempData["Error"] = "El usuario no existe.";
                return RedirectToAction("GestionUsuarios");
            }

            bool activo = user.LockoutEndDateUtc == null || user.LockoutEndDateUtc <= DateTime.UtcNow;

            if (activo)
            {
                user.LockoutEndDateUtc = DateTime.UtcNow.AddYears(50); // bloquear
                TempData["Success"] = "La cuenta ha sido desactivada.";
            }
            else
            {
                user.LockoutEndDateUtc = null; // activar
                TempData["Success"] = "La cuenta ha sido activada.";
            }

            _userManager.Update(user);
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

                    // Opcional: Guardar en sesión para mostrar discretamente la contraseña
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
