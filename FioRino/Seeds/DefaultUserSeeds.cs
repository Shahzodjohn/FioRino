using FioRino.Entities.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FioRino.Seeds
{
    public class DefaultUserSeeds
    {
        public static async Task AddDefaultUserSeeds(UserManager<User> userManager)
        {
            if (await userManager.FindByNameAsync("Admin") == null)
            {
                var user = new User
                {
                    FirstName = "Admin",
                    UserName = "Admin",
                    Email = "Admin"
                };
                await userManager.CreateAsync(user, "adminPas");
                await userManager.AddToRoleAsync(user, "admin");
            }


        }
    }
}
