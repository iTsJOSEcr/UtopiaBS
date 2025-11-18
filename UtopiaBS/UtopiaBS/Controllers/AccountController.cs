using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using System;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using UtopiaBS.Data;
using UtopiaBS.Entities;
using UtopiaBS.Entities.Clientes;
using UtopiaBS.Models;
using System.Linq;


namespace UtopiaBS.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<UsuarioDA> _userManager;

        public AccountController()
        {
            _userManager = new UserManager<UsuarioDA>(new UserStore<UsuarioDA>(new ApplicationDbContext()));
        }

        [AllowAnonymous]
        public ActionResult Login() => View();

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.UserName) || string.IsNullOrWhiteSpace(model.Password))
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

            // Revisar si está bloqueado
            if (await _userManager.IsLockedOutAsync(user.Id))
            {
                ModelState.AddModelError("", "Tu cuenta está bloqueada por múltiples intentos fallidos.");
                return View(model);
            }

            // Validar contraseña
            if (await _userManager.CheckPasswordAsync(user, model.Password))
            {
                await _userManager.ResetAccessFailedCountAsync(user.Id);

                // Crear cookie de sesión
                var identity = await _userManager.CreateIdentityAsync(user, DefaultAuthenticationTypes.ApplicationCookie);
                HttpContext.GetOwinContext().Authentication.SignIn(new AuthenticationProperties { IsPersistent = false }, identity);

                // Registrar actividad
                using (var db = new Context())
                {
                    db.UsuarioActividad.Add(new UsuarioActividad
                    {
                        UserId = user.Id,
                        FechaInicio = DateTime.Now
                    });
                    db.SaveChanges();
                }

                if (await _userManager.IsInRoleAsync(user.Id, "Administrador"))
                    return RedirectToAction("AdminHome", "Home");

                return RedirectToAction("Index", "Home");
            }

            await _userManager.AccessFailedAsync(user.Id);

            ModelState.AddModelError("", "Usuario o contraseña incorrectos.");
            return View(model);
        }


        [AllowAnonymous]
        public ActionResult Register() => View();

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // 1) Usuario duplicado
            if (_userManager.Users.Any(u => u.UserName == model.UserName))
            {
                ModelState.AddModelError("", "El nombre de usuario ya está registrado.");
                return View(model);
            }

            // 2) Correo duplicado
            if (_userManager.Users.Any(u => u.Email == model.Email))
            {
                ModelState.AddModelError("", "El correo electrónico ya está en uso.");
                return View(model);
            }

            // Si pasa las validaciones, crear usuario
            var user = new UsuarioDA
            {
                UserName = model.UserName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                PhoneNumberConfirmed = true,
                Nombre = model.Nombre,
                Apellido = model.Apellido,
                FechaNacimiento = model.FechaNacimiento
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                AddErrors(result);
                return View(model);
            }

            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(new ApplicationDbContext()));
            if (!await roleManager.RoleExistsAsync("Cliente"))
                await roleManager.CreateAsync(new IdentityRole("Cliente"));

            await _userManager.AddToRoleAsync(user.Id, "Cliente");


            using (var db = new Context())
            {
                // 3) Cliente duplicado (IdUsuario)
                if (db.Clientes.Any(c => c.IdUsuario == user.Id))
                {
                    ModelState.AddModelError("", "El cliente ya está registrado.");
                    return View(model);
                }

                var cliente = new Cliente
                {
                    Nombre = model.Nombre + " " + model.Apellido,
                    IdTipoMembresia = null,
                    IdUsuario = user.Id
                };

                db.Clientes.Add(cliente);
                db.SaveChanges();
            }

            TempData["RegisterSuccess"] = "Cuenta creada correctamente. Ahora puedes iniciar sesión.";
            return RedirectToAction("Login", "Account");
        }

        [Authorize]
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
            HttpContext.GetOwinContext().Authentication.SignOut();

            return RedirectToAction("Login", "Account");
        }


        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        [Authorize]
        public async Task<ActionResult> Perfil()
        {
            var userId = User.Identity.GetUserId();
            var usuario = await _userManager.FindByIdAsync(userId);

            return View(usuario);
        }

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
            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["Error"] = "Debes ingresar tu correo.";
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                TempData["Error"] = "No existe ninguna cuenta con este correo.";
                return View();
            }

            // Crear token seguro
            var token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");

            using (var db = new ApplicationDbContext())
            {
                var userDb = db.Users.FirstOrDefault(u => u.Id == user.Id);
                userDb.ResetToken = token;

                db.SaveChanges();
            }

            // Redirigir a la pantalla de cambio de contraseña
            return RedirectToAction("ResetPassword", new { token = token });
        }

        [AllowAnonymous]
        public ActionResult ResetPassword(string token)
        {
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login");

            ViewBag.Token = token;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(string token, string password, string confirmPassword)
        {
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login");

            if (password != confirmPassword)
            {
                TempData["Error"] = "Las contraseñas no coinciden.";
                return RedirectToAction("ResetPassword", new { token });
            }

            using (var db = new ApplicationDbContext())
            {
                var user = db.Users.FirstOrDefault(u => u.ResetToken == token);

                if (user == null)
                {
                    TempData["Error"] = "El enlace no es válido o ya fue utilizado.";
                    return RedirectToAction("Login");
                }

                var result = await _userManager.RemovePasswordAsync(user.Id);
                await _userManager.AddPasswordAsync(user.Id, password);

                user.ResetToken = null;
                db.SaveChanges();
            }

            TempData["Success"] = "Tu contraseña fue actualizada correctamente.";
            return RedirectToAction("Login");
        }


    }
}