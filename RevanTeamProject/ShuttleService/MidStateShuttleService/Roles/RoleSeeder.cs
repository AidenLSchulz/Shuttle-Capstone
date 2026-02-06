using Microsoft.AspNetCore.Identity;

namespace MidStateShuttleService.Roles
{
    public class RoleSeeder
    {
        public static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            //Define the roles you want in your system
            string[] roleNames = { "Admin", "Driver", "Rider" };

            foreach (var roleName in roleNames)
            {
                //Check if the role already exists in the database
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    //If it doesn't exist, create it
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
        }
    }
}
