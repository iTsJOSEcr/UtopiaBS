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
            if (TempData.ContainsKey("Error"))
            {
                ViewBag.mensaje = TempData["Error"];
            }
            else if (TempData.ContainsKey("Mensaje"))
            {
                ViewBag.mensaje = TempData["Mensaje"];
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