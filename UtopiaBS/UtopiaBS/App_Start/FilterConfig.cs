using System.Web;
using System.Web.Mvc;

namespace UtopiaBS
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            // Usar el manejador personalizado en lugar del HandleErrorAttribute por defecto
           // filters.Add(new CustomHandleErrorAttribute());
           // filters.Add(new HandleAntiForgeryErrorAttribute());//
        }

        public class HandleAntiForgeryErrorAttribute : HandleErrorAttribute
        {
            public override void OnException(ExceptionContext filterContext)
            {
                if (filterContext.Exception is HttpAntiForgeryException)
                {
                    filterContext.ExceptionHandled = true;
                    filterContext.Result = new RedirectResult("/Account/Login");
                }
                base.OnException(filterContext);
            }
        }

        // Manejador personalizado que redirige a Error/General y pasa el mensaje mediante TempData
        public class CustomHandleErrorAttribute : HandleErrorAttribute
        {
            public override void OnException(ExceptionContext filterContext)
            {
                if (filterContext == null || filterContext.Exception == null)
                {
                    base.OnException(filterContext);
                    return;
                }

                // Evitar procesar si ya fue manejada por otro filtro
                if (filterContext.ExceptionHandled)
                {
                    base.OnException(filterContext);
                    return;
                }

                var exception = filterContext.Exception;

                // Aquí se puede registrar el error (log) si se desea
                // Ej: Log.Error(exception);

                // Marcar como manejada para que MVC no muestre la vista por defecto
                filterContext.ExceptionHandled = true;

                // Pasar el mensaje mediante TempData (se mantiene tras RedirectToRouteResult)
                if (filterContext.Controller != null)
                {
                    filterContext.Controller.TempData["mensaje"] = exception.Message;
                }

                // Redirigir a Error/General
                filterContext.Result = new RedirectToRouteResult(
                    new System.Web.Routing.RouteValueDictionary(new { controller = "Error", action = "General" })
                );

                // No llamar a base.OnException aquí para evitar comportamiento por defecto
            }
        }
    }
}
