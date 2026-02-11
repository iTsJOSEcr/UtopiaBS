using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using UtopiaBS.Data;

namespace UtopiaBS.App_Start
{
    public class ApplicationUserManager : UserManager<UsuarioDA>
    {
        public ApplicationUserManager(IUserStore<UsuarioDA> store)
            : base(store)
        {
        }

        public static ApplicationUserManager Create(
            IdentityFactoryOptions<ApplicationUserManager> options,
            IOwinContext context)
        {
            var manager = new ApplicationUserManager(
                new UserStore<UsuarioDA>(context.Get<ApplicationDbContext>()));

            manager.UserTokenProvider =
                new DataProtectorTokenProvider<UsuarioDA>(
                    options.DataProtectionProvider.Create("ASP.NET Identity"));

            return manager;
        }
    }
}