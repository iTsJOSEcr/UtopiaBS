using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using OfficeOpenXml;          // ?? agregado
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

            IdentitySeeder.Seed();
        }
    }
}
