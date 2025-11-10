using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Owin;
using UtopiaBS.Data;

[assembly: OwinStartup(typeof(UtopiaBS.Startup))]
namespace UtopiaBS
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.CreatePerOwinContext(ApplicationDbContext.Create);
            app.CreatePerOwinContext<UserManager<UsuarioDA>>(CreateUserManager);

            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                LoginPath = new PathString("/Account/Login"),
                LogoutPath = new PathString("/Account/Logout")
            });
        }

        private static UserManager<UsuarioDA> CreateUserManager(IdentityFactoryOptions<UserManager<UsuarioDA>> options, IOwinContext context)
        {
            var manager = new UserManager<UsuarioDA>(new UserStore<UsuarioDA>(context.Get<ApplicationDbContext>()));
            manager.PasswordValidator = new PasswordValidator
            {
                RequiredLength = 6
            };
            return manager;
        }
    }
}
