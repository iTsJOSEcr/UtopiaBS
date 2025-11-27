using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Owin.Security;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
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

            // 1️⃣ PRIMERO: cuenta desactivada (nuestro caso de GDU-005)
            if (!user.Activo)
            {
                ModelState.AddModelError("",
                    "Tu cuenta está desactivada temporalmente. Si necesitás volver a activarla, contactá al administrador.");
                return View(model);
            }

            // 2️⃣ LUEGO: bloqueo por múltiples intentos fallidos
            if (await _userManager.IsLockedOutAsync(user.Id))
            {
                ModelState.AddModelError("", "Tu cuenta está bloqueada por múltiples intentos fallidos.");
                return View(model);
            }

            // 3️⃣ Validar contraseña
            if (await _userManager.CheckPasswordAsync(user, model.Password))
            {
                await _userManager.ResetAccessFailedCountAsync(user.Id);

                // Crear cookie de sesión
                var identity = await _userManager.CreateIdentityAsync(
                    user, DefaultAuthenticationTypes.ApplicationCookie);

                HttpContext.GetOwinContext().Authentication.SignIn(
                    new AuthenticationProperties { IsPersistent = false }, identity);

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

            // 3) Cédula duplicada (en AspNetUsers)
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
                Cedula = model.Cedula  
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                AddErrors(result);
                return View(model);
            }

            // Asegurar rol "Cliente"
            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(new ApplicationDbContext()));
            if (!await roleManager.RoleExistsAsync("Cliente"))
                await roleManager.CreateAsync(new IdentityRole("Cliente"));

            await _userManager.AddToRoleAsync(user.Id, "Cliente");

            // Crear Cliente + Membresía Básica
            using (var db = new Context())
            {
                // Evitar duplicar cliente para el mismo usuario
                if (db.Clientes.Any(c => c.IdUsuario == user.Id))
                {
                    ModelState.AddModelError("", "El cliente ya está registrado.");
                    return View(model);
                }

                // Crear fila en Clientes
                var cliente = new Cliente
                {
                    Nombre = model.Nombre + " " + model.Apellido,
                    IdTipoMembresia = null,
                    IdUsuario = user.Id,
                    Cedula = model.Cedula
                };

                db.Clientes.Add(cliente);
                db.SaveChanges();

                // Buscar el tipo de membresía Básica
                int idTipoBasica = 1; // 👈 ID real de la membresía Básica en tu BD

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
                TempData["RegisterSuccess"] = "Cuenta creada correctamente. Ya puedes iniciar sesión y comenzar a acumular puntos con tu membresía básica.";
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


        // PERFIL
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


        // EDITAR PERFIL - GET
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


        // EDITAR PERFIL - POST
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

        [Authorize(Roles = "Cliente")]
        public ActionResult MisPuntos()
        {
            var userId = User.Identity.GetUserId();

            using (var db = new Context())
            {
                // Buscar el cliente ligado al usuario
                var cliente = db.Clientes.FirstOrDefault(c => c.IdUsuario == userId);

                if (cliente == null)
                {
                    // No es cliente (o aún no tiene registro en Clientes)
                    var modeloVacio = new MisPuntosViewModel
                    {
                        PuntosTotales = 0,
                        TipoMembresia = "Sin membresía activa"
                    };
                    return View("MisPuntos", modeloVacio);
                }

                // Membresía activa (la Básica que creamos al registrar)
                var membresia = db.Membresias
                    .FirstOrDefault(m =>
                        m.IdCliente == cliente.IdCliente &&
                        (m.FechaFin == null || m.FechaFin >= DateTime.Today));

                int puntosTotales = membresia?.PuntosAcumulados ?? 0;

                // Movimientos de puntos
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

    }
}