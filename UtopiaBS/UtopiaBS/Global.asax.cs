using OfficeOpenXml;          // ?? agregado
using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using UtopiaBS.Data;

namespace UtopiaBS
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            // EPPlus 8: establecer licencia (no comercial personal)
            ExcelPackage.License.SetNonCommercialPersonal("UtopiaBS");

            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            //descomentar cuando la base de datos se publique 
           // IdentitySeeder.Seed();
        }

        protected void Application_Error()
        {
            Exception exception = Server.GetLastError();
            if (exception == null)
            {
                return;
            }

            try
            {
                // Registrar el error si lo deseas (aquí podrías grabarlo en log)
                // Example: Log.Error(exception);

                // Preparar la respuesta para renderizar la vista de error
                Response.Clear();
                Server.ClearError();
                Response.StatusCode = 500;
                Response.TrySkipIisCustomErrors = true;

                // Creamos un controlador envoltorio para renderizar la vista MVC fuera de una acción
                var controller = new ErrorControllerWrapper();
                var httpContextWrapper = new HttpContextWrapper(Context);
                var routeData = new RouteData();
                // No necesitamos valores concretos de controller/action, pero los podemos poner por claridad
                routeData.Values["controller"] = "Error";
                routeData.Values["action"] = "General";

                controller.ControllerContext = new ControllerContext(new RequestContext(httpContextWrapper, routeData), controller);

                // Pasar el mensaje de error a la vista mediante ViewBag
                controller.ViewBag.mensaje = exception.Message+" inner:"+exception.InnerException?.Message;

                // Renderizar la vista directamente (ruta física virtual desde la raíz del proyecto)
                var viewResult = new ViewResult
                {
                    ViewName = "~/Views/Error/General.cshtml",
                    ViewData = controller.ViewData,
                    TempData = controller.TempData
                };

                viewResult.ExecuteResult(controller.ControllerContext);
            }
            catch (Exception exRendering)
            {
                // Si la renderización falla, aseguramos que la respuesta no quede en blanco
                Response.Clear();
                Response.StatusCode = 500;
                Response.ContentType = "text/plain";
                Response.Write("Ocurrió un error interno al procesar la solicitud.");
                // opcional: Response.Write(exRendering.Message);
            }
        }

        // Controlador envoltorio mínimo para renderizar vistas fuera de una acción MVC
        private class ErrorControllerWrapper : Controller { }
    }
}