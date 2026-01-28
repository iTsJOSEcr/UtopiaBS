using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using UtopiaBS.Business.Services;
using UtopiaBS.Data;
using UtopiaBS.Entities;
using UtopiaBS.Entities.Clientes;
using UtopiaBS.Models;

namespace UtopiaBS.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<UsuarioDA> _userManager;

        public AccountController()
        {
            var db = new ApplicationDbContext();

            _userManager = new UserManager<UsuarioDA>(
                new UserStore<UsuarioDA>(db));

            _userManager.UserTokenProvider =
                new DataProtectorTokenProvider<UsuarioDA>(
                    new Microsoft.Owin.Security.DataProtection
                        .DpapiDataProtectionProvider("UtopiaBS")
                    .Create("Identity"))
                {
                    TokenLifespan = TimeSpan.FromHours(2)
                };
        }

        // ===================== LOGIN =====================

        [HttpGet]
        [AllowAnonymous]
        public ActionResult Login() => View();

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.UserName) ||
                string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.AddModelError("", "No se puede iniciar sesión: hay campos en blanco.");
                return View(model);
            }

            // Buscar usuario por username o correo
            var user = _userManager.Users
                .FirstOrDefault(u => u.UserName == model.UserName || u.Email == model.UserName);

            if (user == null)
            {
                ModelState.AddModelError("", "La cuenta no existe en el sistema.");
                return View(model);
            }

            // Cuenta desactivada → enviar enlace de reactivación
            if (!user.Activo)
            {
                var token = await _userManager.GenerateUserTokenAsync(
                    "ReactivarCuenta", user.Id);

                var enlace = Url.Action("ConfirmarReactivacion", "Account",
                    new { userId = user.Id, token = token },
                    protocol: Request.Url.Scheme);

                string mensaje = $@"
                <h2>Reactivación de cuenta</h2>
                <p>Hola {user.UserName},</p>
                <p>Tu cuenta está desactivada.</p>
                <p>Haz clic aquí para reactivarla de forma segura:</p>
                <p><a href='{enlace}'>Reactivar cuenta</a></p>
                <p>Utopía Beauty Salon</p>
                ";

                await EmailService.EnviarCorreoAsync(
                    user.Email,
                    "Reactivación de cuenta - Utopía Beauty Salon",
                    mensaje
                );

                ModelState.AddModelError("",
                    "Tu cuenta está desactivada. Te enviamos un correo para reactivarla.");

                return View(model);
            }

            // Bloqueo por intentos fallidos
            if (await _userManager.IsLockedOutAsync(user.Id))
            {
                ModelState.AddModelError("",
                    "Tu cuenta está bloqueada por múltiples intentos fallidos.");
                return View(model);
            }

            // Validar contraseña
            if (await _userManager.CheckPasswordAsync(user, model.Password))
            {
                await _userManager.ResetAccessFailedCountAsync(user.Id);

                // Crear cookie de sesión
                var identity = await _userManager.CreateIdentityAsync(
                    user, DefaultAuthenticationTypes.ApplicationCookie);

                HttpContext.GetOwinContext().Authentication.SignIn(
                    new AuthenticationProperties
                    {
                        IsPersistent = false,
                        ExpiresUtc = DateTime.UtcNow.AddHours(6)
                    },
                    identity);

                using (var db = new Context())
                {
                    db.UsuarioActividad.Add(new UsuarioActividad
                    {
                        UserId = user.Id,
                        FechaInicio = DateTime.Now
                    });
                    db.SaveChanges();
                }

                // Redirección por rol
                if (await _userManager.IsInRoleAsync(user.Id, "Administrador"))
                    return RedirectToAction("AdminHome", "Home");

                if (await _userManager.IsInRoleAsync(user.Id, "Empleado"))
                    return RedirectToAction("EmpleadoHome", "Home");

                if (await _userManager.IsInRoleAsync(user.Id, "Cliente"))
                    return RedirectToAction("ClienteHome", "Home");
                
                // fallback de seguridad
                return RedirectToAction("Index", "Home");


            }

            // Contraseña incorrecta
            await _userManager.AccessFailedAsync(user.Id);
            ModelState.AddModelError("", "Usuario o contraseña incorrectos.");
            return View(model);
        }

        // ===================== REGISTRO =====================

        [HttpGet]
        [AllowAnonymous]
        public ActionResult Register() => View();

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Validar requisitos de contraseña
            if (!PasswordCumpleRequisitos(model.Password))
            {
                TempData["Error"] = "La contraseña debe tener al menos 8 caracteres, incluir al menos 1 mayúscula y 1 carácter especial.";
                return View(model);
            }

            try
            {
                // Usuario duplicado
                if (_userManager.Users.Any(u => u.UserName == model.UserName))
                {
                    ModelState.AddModelError("", "El nombre de usuario ya está registrado.");
                    return View(model);
                }

                // Correo duplicado
                if (_userManager.Users.Any(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("", "El correo electrónico ya está en uso.");
                    return View(model);
                }

                // Cédula duplicada
                if (_userManager.Users.Any(u => u.Cedula == model.Cedula))
                {
                    ModelState.AddModelError("", "Ya existe un usuario registrado con esta cédula.");
                    return View(model);
                }

                // Crear usuario de Identity
                var user = new UsuarioDA
                {
                    UserName = model.UserName,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    PhoneNumberConfirmed = true,
                    Nombre = model.Nombre,
                    Apellido = model.Apellido,
                    FechaNacimiento = model.FechaNacimiento,
                    Cedula = model.Cedula,
                    Activo = true,
                    FechaUltimaActivacion = DateTime.Now
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (!result.Succeeded)
                {
                    AddErrors(result);
                    return View(model);
                }

                // Asegurar rol "Cliente"
                var roleManager = new RoleManager<IdentityRole>(
                    new RoleStore<IdentityRole>(new ApplicationDbContext()));

                if (!await roleManager.RoleExistsAsync("Cliente"))
                    await roleManager.CreateAsync(new IdentityRole("Cliente"));

                await _userManager.AddToRoleAsync(user.Id, "Cliente");

                // Crear Cliente + Membresía
                using (var db = new Context())
                {
                    if (db.Clientes.Any(c => c.IdUsuario == user.Id))
                    {
                        ModelState.AddModelError("", "El cliente ya está registrado.");
                        return View(model);
                    }

                    var cliente = new Cliente
                    {
                        Nombre = model.Nombre + " " + model.Apellido,
                        IdUsuario = user.Id,
                        Cedula = model.Cedula
                    };

                    db.Clientes.Add(cliente);
                    db.SaveChanges();

                    int idTipoBasica = 1;

                    var membresia = new Membresia
                    {
                        IdCliente = cliente.IdCliente,
                        IdTipoMembresia = idTipoBasica,
                        FechaInicio = DateTime.Now,
                        FechaFin = null,
                        PuntosAcumulados = 0
                    };

                    db.Membresias.Add(membresia);
                    db.SaveChanges();
                }

                TempData["RegisterSuccess"] =
                    "Cuenta creada correctamente. Ya puedes iniciar sesión y comenzar a acumular puntos.";

                return RedirectToAction("Login", "Account");
            }
            catch
            {
                TempData["Error"] =
                    "El sistema está tardando más de lo normal. Intenta nuevamente en unos minutos.";

                return RedirectToAction("Register");
            }
        }

        // ===================== LOGOUT =====================

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            var userId = User.Identity.GetUserId();

            using (var db = new Context())
            {
                var ultimaActividad = db.UsuarioActividad
                    .Where(a => a.UserId == userId && a.FechaFin == null)
                    .OrderByDescending(a => a.FechaInicio)
                    .FirstOrDefault();

                if (ultimaActividad != null)
                {
                    ultimaActividad.FechaFin = DateTime.Now;
                    db.SaveChanges();
                }
            }

            // Cerrar sesión
            var auth = HttpContext.GetOwinContext().Authentication;
            auth.SignOut(
                DefaultAuthenticationTypes.ApplicationCookie,
                DefaultAuthenticationTypes.ExternalCookie
            );

            Session.Clear();
            Session.Abandon();

            return RedirectToAction("Login", "Account");
        }

        // ===================== PERFIL =====================

        [Authorize]
        public async Task<ActionResult> Perfil()
        {
            var id = User.Identity.GetUserId();

            if (string.IsNullOrEmpty(id))
                return RedirectToAction("Login");

            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
                return RedirectToAction("Login");

            return View("Perfil", user);
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult> EditarPerfil()
        {
            var id = User.Identity.GetUserId();
            var user = await _userManager.FindByIdAsync(id);

            var model = new UpdateProfileViewModel
            {
                UserName = user.UserName,
                Nombre = user.Nombre,
                Apellido = user.Apellido,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                FechaNacimiento = user.FechaNacimiento ?? DateTime.Now
            };

            return View("EditarPerfil", model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditarPerfil(UpdateProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Hay campos vacíos o inválidos.";
                return View("EditarPerfil", model);
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(model.PhoneNumber, @"^[0-9]{8,15}$"))
            {
                TempData["Error"] = "El teléfono debe contener solo números (8 a 15 dígitos).";
                return View("EditarPerfil", model);
            }

            try
            {
                var userId = User.Identity.GetUserId();
                var usuario = await _userManager.FindByIdAsync(userId);

                if (usuario == null)
                {
                    TempData["Error"] = "El cliente no existe.";
                    return View("EditarPerfil", model);
                }

                if (_userManager.Users.Any(u => u.UserName == model.UserName && u.Id != userId))
                {
                    TempData["Error"] = "Ese nombre de usuario ya está en uso.";
                    return View("EditarPerfil", model);
                }

                if (_userManager.Users.Any(u => u.Email == model.Email && u.Id != userId))
                {
                    TempData["Error"] = "Ese correo ya está en uso.";
                    return View("EditarPerfil", model);
                }

                usuario.UserName = model.UserName;
                usuario.Nombre = model.Nombre;
                usuario.Apellido = model.Apellido;
                usuario.Email = model.Email;
                usuario.PhoneNumber = model.PhoneNumber;
                usuario.FechaNacimiento = model.FechaNacimiento;

                var result = await _userManager.UpdateAsync(usuario);

                if (!result.Succeeded)
                {
                    TempData["Error"] = "No se pudo actualizar el perfil.";
                    return View("EditarPerfil", model);
                }

                using (var db = new Context())
                {
                    var cliente = db.Clientes.FirstOrDefault(c => c.IdUsuario == userId);
                    if (cliente != null)
                    {
                        cliente.Nombre = model.Nombre + " " + model.Apellido;
                        db.SaveChanges();
                    }
                }

                TempData["Success"] = "Perfil actualizado correctamente.";
                return RedirectToAction("Perfil");
            }
            catch
            {
                TempData["Error"] = "Error de conexión al actualizar.";
                return View("EditarPerfil", model);
            }
        }

        // ===================== OLVIDAR CONTRASEÑA =====================

        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ForgotPassword(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                TempData["Error"] = "No existe una cuenta con ese correo.";
                return RedirectToAction("ForgotPassword");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user.Id);

            var enlace = Url.Action(
                "ResetPassword",
                "Account",
                new { userId = user.Id, token = token },
                protocol: Request.Url.Scheme
            );

            string mensaje = $@"
        <h2>Recuperación de contraseña</h2>
        <p>Hola {user.UserName},</p>
        <p>Haz clic en el siguiente enlace para restablecer tu contraseña:</p>
        <p><a href='{enlace}'>Restablecer contraseña</a></p>
        <p>Utopía Beauty Salon</p>
    ";

            try
            {
                await EmailService.EnviarCorreoAsync(
                    user.Email,
                    "Recuperar contraseña - Utopía Beauty Salon",
                    mensaje
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

            TempData["Success"] = "Revisa tu correo para recuperar tu contraseña.";
            return RedirectToAction("Login");
        }


        // ===================== VALIDACIÓN DE CONTRASEÑA =====================

        // 1 mayúscula, 1 carácter especial, longitud >= 8
        private bool PasswordCumpleRequisitos(string password)
        {
            if (string.IsNullOrEmpty(password))
                return false;

            bool tieneMayuscula = password.Any(char.IsUpper);
            bool tieneCaracterEspecial = password.Any(ch => !char.IsLetterOrDigit(ch) && !char.IsWhiteSpace(ch));
            bool longitudOk = password.Length >= 8;

            return tieneMayuscula && tieneCaracterEspecial && longitudOk;
        }

        // ===================== RESET PASSWORD =====================

        [HttpGet]
        [AllowAnonymous]
        public ActionResult ResetPassword(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                TempData["Error"] = "Enlace de recuperación inválido.";
                return RedirectToAction("Login");
            }

            ViewBag.UserId = userId;
            ViewBag.Token = token;

            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(
            string userId,
            string token,
            string password,
            string confirmPassword)
        {
            // 1) Coincidencia
            if (password != confirmPassword)
            {
                TempData["Error"] = "Las contraseñas no coinciden.";
                ViewBag.UserId = userId;
                ViewBag.Token = token;
                return View();
            }

            // 2) Requisitos
            if (!PasswordCumpleRequisitos(password))
            {
                TempData["Error"] = "La nueva contraseña no cumple los requisitos. Debe tener al menos 8 caracteres, incluir al menos 1 mayúscula y 1 carácter especial.";
                ViewBag.UserId = userId;
                ViewBag.Token = token;
                return View();
            }

            // 3) Buscar usuario
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                TempData["Error"] = "El usuario no existe.";
                return RedirectToAction("Login");
            }

            // 4) Resetear contraseña
            var result = await _userManager.ResetPasswordAsync(userId, token, password);

            if (result.Succeeded)
            {
                TempData["Success"] = "Tu contraseña fue actualizada correctamente.";
                return RedirectToAction("Login");
            }

            TempData["Error"] = "No se pudo cambiar la contraseña.";
            ViewBag.UserId = userId;
            ViewBag.Token = token;
            return View();
        }

        // ===================== MIS PUNTOS =====================

        [Authorize(Roles = "Cliente")]
        public ActionResult MisPuntos()
        {
            var userId = User.Identity.GetUserId();

            using (var db = new Context())
            {
                var cliente = db.Clientes.FirstOrDefault(c => c.IdUsuario == userId);

                if (cliente == null)
                {
                    var modeloVacio = new MisPuntosViewModel
                    {
                        PuntosTotales = 0,
                        TipoMembresia = "Sin membresía activa"
                    };
                    return View("MisPuntos", modeloVacio);
                }

                var membresia = db.Membresias
                    .FirstOrDefault(m =>
                        m.IdCliente == cliente.IdCliente &&
                        (m.FechaFin == null || m.FechaFin >= DateTime.Today));

                int puntosTotales = membresia?.PuntosAcumulados ?? 0;

                var movimientos = db.PuntosCliente
                    .Where(p => p.IdCliente == cliente.IdCliente)
                    .OrderByDescending(p => p.FechaRegistro)
                    .Select(p => new MovimientoPuntosViewModel
                    {
                        Fecha = p.FechaRegistro,
                        Descripcion = "Puntos por venta #" + p.IdVenta,
                        Puntos = p.Puntos
                    })
                    .ToList();

                var model = new MisPuntosViewModel
                {
                    PuntosTotales = puntosTotales,
                    TipoMembresia = membresia != null ? "Membresía Básica" : "Sin membresía activa",
                    FechaInicioMembresia = membresia?.FechaInicio,
                    FechaFinMembresia = membresia?.FechaFin,
                    Movimientos = movimientos
                };

                return View("MisPuntos", model);
            }
        }

        // ===================== DESACTIVAR CUENTA (TEMPORAL) =====================

        [Authorize(Roles = "Cliente")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DesactivarCuentaTemporal()
        {
            try
            {
                string userId = User.Identity.GetUserId();

                var user = _userManager.FindById(userId);

                if (user == null)
                {
                    TempData["Error"] = "La cuenta no existe.";
                    return RedirectToAction("Perfil");
                }

                // Regla de 7 días
                if (user.FechaUltimaActivacion.HasValue &&
                    user.FechaUltimaActivacion.Value.AddDays(7) > DateTime.Now)
                {
                    TempData["Error"] =
                        "Tu cuenta no se puede desactivar porque tiene menos de 7 días de haber sido activada.";
                    return RedirectToAction("Perfil");
                }

                user.Activo = false;
                user.LockoutEnabled = true;
                user.LockoutEndDateUtc = DateTime.UtcNow.AddYears(10);

                _userManager.Update(user);

                // Cerrar sesión automáticamente
                HttpContext.GetOwinContext().Authentication.SignOut();

                TempData["Success"] = "Tu cuenta fue desactivada temporalmente exitosamente.";
                return RedirectToAction("Login", "Account");
            }
            catch
            {
                TempData["Error"] = "Ocurrió un error de conexión al intentar desactivar la cuenta.";
                return RedirectToAction("Perfil");
            }
        }

        // ===================== DESACTIVAR CUENTA (PERMANENTE) =====================

        [Authorize(Roles = "Cliente")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DesactivarCuentaPermanente()
        {
            try
            {
                string userId = User.Identity.GetUserId();
                var user = _userManager.FindById(userId);

                if (user == null)
                {
                    TempData["Error"] = "La cuenta no existe.";
                    return RedirectToAction("Perfil");
                }

                // Marcamos como inactiva y bloqueada "para siempre"
                user.Activo = false;
                user.LockoutEnabled = true;
                user.LockoutEndDateUtc = DateTime.UtcNow.AddYears(100);

                _userManager.Update(user);

                // Cerrar sesión
                HttpContext.GetOwinContext().Authentication.SignOut();
                Session.Clear();
                Session.Abandon();

                TempData["Success"] = "Tu cuenta fue eliminada de forma permanente.";
                return RedirectToAction("Login", "Account");
            }
            catch
            {
                TempData["Error"] = "Ocurrió un error al intentar eliminar la cuenta.";
                return RedirectToAction("Perfil");
            }
        }

        // ===================== CONFIRMAR REACTIVACIÓN =====================

        [AllowAnonymous]
        public async Task<ActionResult> ConfirmarReactivacion(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                TempData["Error"] = "Enlace inválido.";
                return RedirectToAction("Login");
            }

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                TempData["Error"] = "Cuenta inválida.";
                return RedirectToAction("Login");
            }

            bool valido = await _userManager.VerifyUserTokenAsync(
                user.Id, "ReactivarCuenta", token);

            if (!valido)
            {
                TempData["Error"] = "Este enlace ha expirado o ya fue utilizado.";
                return RedirectToAction("Login");
            }

            ViewBag.UserId = userId;
            ViewBag.Token = token;

            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult> ConfirmarReactivacionPost(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                TempData["Error"] = "La cuenta no existe.";
                return RedirectToAction("Login");
            }

            bool valido = await _userManager.VerifyUserTokenAsync(
                user.Id, "ReactivarCuenta", token);

            if (!valido)
            {
                TempData["Error"] = "Este enlace ya no es válido.";
                return RedirectToAction("Login");
            }

            user.Activo = true;
            user.LockoutEndDateUtc = null;
            user.FechaUltimaActivacion = DateTime.Now;

            await _userManager.UpdateAsync(user);

            TempData["Success"] = "✅ Tu cuenta fue reactivada correctamente.";
            return RedirectToAction("Login");
        }

        // ===================== ERRORES IDENTITY =====================

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }
    }
}