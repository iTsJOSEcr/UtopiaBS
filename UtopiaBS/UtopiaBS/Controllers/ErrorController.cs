using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace UtopiaBS.Controllers
{
    [AllowAnonymous]
    public class ErrorController : Controller
    {
        public ActionResult General()
        {
            // Si el mensaje viene por TempData lo pasamos a ViewBag
            if (TempData.ContainsKey("mensaje"))
            {
                ViewBag.mensaje = TempData["mensaje"] as string;
            }
            else
            {
                ViewBag.mensaje = "Ocurrió un error inesperado.";
            }

            return View();
        }

        public ActionResult NotFound()
        {
            return View();
        }
    }
}
