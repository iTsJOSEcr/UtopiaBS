using System.Web;
using System.Web.Mvc;

namespace UtopiaBS
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            filters.Add(new HandleAntiForgeryErrorAttribute());

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

    }
}
