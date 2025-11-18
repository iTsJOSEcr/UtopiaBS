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
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindAsync(model.UserName, model.Password);
            if (user != null)
            {
                var identity = await _userManager.CreateIdentityAsync(user, DefaultAuthenticationTypes.ApplicationCookie);
                HttpContext.GetOwinContext().Authentication.SignIn(new AuthenticationProperties { IsPersistent = false }, identity);

                using (var db = new Context())
                {
                    var registro = new UsuarioActividad
                    {
                        UserId = user.Id,
                        FechaInicio = DateTime.Now,
                        FechaFin = null
                    };
                    db.UsuarioActividad.Add(registro);
                    db.SaveChanges();
                }

                // ✅ Redirección según el rol
                if (await _userManager.IsInRoleAsync(user.Id, "Administrador"))
                {
                    return RedirectToAction("AdminHome", "Home");
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
            }

            // Si no se encuentra el usuario o credenciales incorrectas
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

            if (result.Succeeded)
            {
                var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(new ApplicationDbContext()));
                if (!await roleManager.RoleExistsAsync("Cliente"))
                {
                    await roleManager.CreateAsync(new IdentityRole("Cliente"));
                }

                var addToRole = await _userManager.AddToRoleAsync(user.Id, "Cliente");
                if (!addToRole.Succeeded)
                {
                    AddErrors(addToRole);
                    return View(model);
                }

                // Guardar Cliente vinculado
                try
                {
                    using (var db = new Context())
                    {
                        var cliente = new Cliente
                        {
                            Nombre = model.Nombre + " " + model.Apellido,
                            IdTipoMembresia = null,
                            IdUsuario = user.Id
                        };

                        db.Clientes.Add(cliente);
                        db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error creando el perfil de cliente: " + ex.Message);
                    return View(model);
                }

                TempData["RegisterSuccess"] = "Cuenta creada correctamente. Ahora puedes iniciar sesión.";
                return RedirectToAction("Login", "Account");
            }

            AddErrors(result);
            return View(model);
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
    }
}