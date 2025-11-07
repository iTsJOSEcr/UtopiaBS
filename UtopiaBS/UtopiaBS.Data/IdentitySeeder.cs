using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace UtopiaBS.Data
{
    public static class IdentitySeeder
    {
        public static void Seed()
        {
            using (var context = new ApplicationDbContext())
            {
                var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(context));
                var userManager = new UserManager<UsuarioDA>(new UserStore<UsuarioDA>(context));

                // Crear roles si no existen
                if (!roleManager.RoleExists("Administrador"))
                    roleManager.Create(new IdentityRole("Administrador"));
                if (!roleManager.RoleExists("Empleado"))
                    roleManager.Create(new IdentityRole("Empleado"));
                if (!roleManager.RoleExists("Cliente"))
                    roleManager.Create(new IdentityRole("Cliente"));

                // Crear usuario admin por defecto
                var admin = userManager.FindByName("admin@utopiabs.com");
                if (admin == null)
                {
                    var user = new UsuarioDA
                    {
                        UserName = "admin@utopiabs.com",
                        Email = "admin@utopiabs.com",
                        EmailConfirmed = true
                    };
                    var result = userManager.Create(user, "Admin123*");
                    if (result.Succeeded)
                        userManager.AddToRole(user.Id, "Administrador");
                }
            }
        }
    }
}
