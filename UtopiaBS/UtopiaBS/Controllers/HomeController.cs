using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using UtopiaBS.Data;

namespace UtopiaBS.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        [Authorize(Roles = "Administrador")]
        public ActionResult AdminHome()
        {
            using (var db = new Context())
            {
                DateTime hoy = DateTime.Now;
                int diasAlerta = 30;

                var productos = db.Productos.ToList();

                ViewBag.Expirados = productos
                    .Where(p => p.FechaExpiracion != null && p.FechaExpiracion < hoy)
                    .ToList();

                ViewBag.PorExpirar = productos
                    .Where(p => p.FechaExpiracion != null &&
                                (p.FechaExpiracion.Value - hoy).Days >= 0 &&
                                (p.FechaExpiracion.Value - hoy).Days <= diasAlerta)
                    .ToList();
            }

            return View();
        }

        [Authorize(Roles = "Empleado")]
        public ActionResult EmpleadoHome()
        {
            return View();
        }


    }
}