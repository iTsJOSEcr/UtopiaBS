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
                    new Microsoft.Owin.Security.DataProtection
                        .DpapiDataProtectionProvider("UtopiaBS")
                    .Create("Identity"))
                {
                    TokenLifespan = TimeSpan.FromHours(2)
                };
        }

        // LISTAR USUARIOS
        public ActionResult GestionUsuarios()
        {
            var usuarios = _userManager.Users.ToList();

            using (var db = new Context())
            {
                var usuariosConHistorial = db.UsuarioActividad
                    .Select(a => a.UserId)
                    .Distinct()
                    .ToList();

                ViewBag.UsuariosConHistorial = usuariosConHistorial;
            }

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

        //  ACTIVAR / DESACTIVAR USUARIO (con regla de 7 días)
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
                    //  Quiere DESACTIVAR

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
                    // Quiere ACTIVAR

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

        // ELIMINAR CUENTA
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EliminarUsuario(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "No se recibió el usuario a eliminar.";
                return RedirectToAction("GestionUsuarios");
            }

            try
            {
                var user = _userManager.FindById(userId);

                if (user == null)
                {
                    TempData["Error"] = "La cuenta no existe o ya fue eliminada.";
                    return RedirectToAction("GestionUsuarios");
                }

                using (var db = new Context())
                {
                    //  Eliminar actividad
                    var actividades = db.UsuarioActividad
                        .Where(a => a.UserId == user.Id)
                        .ToList();

                    if (actividades.Any())
                        db.UsuarioActividad.RemoveRange(actividades);

                    //  Buscar cliente ligado
                    var cliente = db.Clientes.FirstOrDefault(c => c.IdUsuario == user.Id);

                    if (cliente != null)
                    {
                        //  Eliminar puntos
                        var puntos = db.PuntosCliente
                            .Where(p => p.IdCliente == cliente.IdCliente)
                            .ToList();

                        if (puntos.Any())
                            db.PuntosCliente.RemoveRange(puntos);

                        //  Eliminar membresías
                        var membresias = db.Membresias
                            .Where(m => m.IdCliente == cliente.IdCliente)
                            .ToList();

                        if (membresias.Any())
                            db.Membresias.RemoveRange(membresias);

                        // . Eliminar cliente
                        db.Clientes.Remove(cliente);
                    }

                    db.SaveChanges();
                }

                //  Eliminar roles
                var roles = _userManager.GetRoles(userId).ToList();
                if (roles.Any())
                    _userManager.RemoveFromRoles(userId, roles.ToArray());

                // Eliminar usuario de Identity
                var result = _userManager.Delete(user);

                if (result.Succeeded)
                    TempData["Success"] = "✅ La cuenta fue eliminada correctamente.";
                else
                    TempData["Error"] = "❌ Identity no permitió eliminar la cuenta.";
            }
            catch (Exception)
            {
                TempData["Error"] = "❌ No se pudo eliminar la cuenta porque tiene datos relacionados.";
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

        [Authorize(Roles = "Administrador,Empleado,Cliente")]
        public ActionResult Bitacora(string userId, DateTime? fecha)
        {
            using (var db = new Context())
            {
                var query = db.UsuarioActividad.AsQueryable();

                // FILTRAR POR USUARIO
                if (!string.IsNullOrEmpty(userId))
                {
                    query = query.Where(x => x.UserId == userId);
                }

                // FILTRAR POR FECHA
                if (fecha.HasValue)
                {
                    var inicio = fecha.Value.Date;
                    var fin = inicio.AddDays(1);
                    query = query.Where(x => x.FechaInicio >= inicio && x.FechaInicio < fin);
                }

                var resultado = query
                    .OrderByDescending(x => x.FechaInicio)
                    .ToList();

                // NO EXISTE USUARIO O FECHA
                if ((userId != null || fecha != null) && !resultado.Any())
                {
                    TempData["Error"] =
                        "No existen registros de bitácora para los filtros ingresados.";
                }

                // Para mostrar combo de usuarios en la vista
                ViewBag.Usuarios = _userManager.Users.ToList();

                return View(resultado);
            }
        }

    }
}
