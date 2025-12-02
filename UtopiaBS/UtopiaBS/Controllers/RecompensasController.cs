using System.Linq;
using System.Web.Mvc;
using UtopiaBS.Data;
using UtopiaBS.Entities.Recompensas;

namespace UtopiaBS.Controllers
{
    [Authorize(Roles = "Administrador")]

    public class RecompensasController : Controller
    {
        // CLIENTES (POS)
        [Authorize]
        public JsonResult ListarRecompensas()
        {
            using (var db = new Context())
            {
                var datos = db.Recompensas
                    .Where(r => r.Activa)
                    .Select(r => new
                    {
                        r.IdRecompensa,
                        r.Nombre,
                        r.Tipo,
                        r.PuntosNecesarios,
                        r.Valor
                    })
                    .ToList();

                return Json(datos, JsonRequestBehavior.AllowGet);
            }
        }

        //  ADMIN - PANEL
        [Authorize(Roles = "Administrador")]
        public ActionResult Index()
        {
            using (var db = new Context())
            {
                return View(db.Recompensas.ToList());
            }
        }

        // CREAR
        [Authorize(Roles = "Administrador")]
        public ActionResult Crear()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(Recompensa model)
        {
            if (!ModelState.IsValid)
                return View(model);

            using (var db = new Context())
            {
                model.Activa = true;
                db.Recompensas.Add(model);
                db.SaveChanges();
            }

            TempData["Success"] = "✅ Recompensa creada.";
            return RedirectToAction("Index");
        }

        //  EDITAR
        [Authorize(Roles = "Administrador")]
        public ActionResult Editar(int id)
        {
            using (var db = new Context())
            {
                var r = db.Recompensas.Find(id);
                return View(r);
            }
        }

[HttpPost]
[ValidateAntiForgeryToken]
public ActionResult Editar(Recompensa model)
{
    using (var db = new Context())
    {
        var recompensa = db.Recompensas.Find(model.IdRecompensa);

        recompensa.Nombre = model.Nombre;
        recompensa.Tipo = model.Tipo;
        recompensa.PuntosNecesarios = model.PuntosNecesarios;
        recompensa.Valor = model.Valor;
        recompensa.Activa = model.Activa; 

        db.SaveChanges();
    }

    return RedirectToAction("Index");
}

        //  ACTIVAR / DESACTIVAR
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public ActionResult CambiarEstado(int id)
        {
            using (var db = new Context())
            {
                var r = db.Recompensas.Find(id);
                r.Activa = !r.Activa;
                db.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Eliminar(int id)
        {
            try
            {
                using (var db = new Context())
                {
                    var recompensa = db.Recompensas.Find(id);

                    if (recompensa == null)
                    {
                        TempData["Error"] = "La recompensa no existe.";
                        return RedirectToAction("Index");
                    }

                    //  VALIDAR SI YA FUE CANJEADA
                    bool fueCanjeada = db.CanjeRecompensas.Any(c => c.IdRecompensa == id);

                    if (fueCanjeada)
                    {
                        TempData["Error"] = "No se puede eliminar una recompensa que ya fue canjeada.";
                        return RedirectToAction("Index");
                    }

                    db.Recompensas.Remove(recompensa);
                    db.SaveChanges();

                    TempData["Success"] = "✅ Recompensa eliminada correctamente.";
                    return RedirectToAction("Index");
                }
            }
            catch
            {
                TempData["Error"] = "❌ Error al eliminar la recompensa.";
                return RedirectToAction("Index");
            }
        }

    }
}
