using Microsoft.AspNetCore.Identity;
using MVCSampleApp.Models;

namespace MVCSampleApp
{
    // Creates the "Admin" and "Employee" roles and one default admin account
    // so you have someone to log in as right after your first run.
    public static class SeedData
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            string[] roles = { "Admin", "Employee" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            var adminEmail = "admin@school.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    // Replace with your own real phone number if you want to test SMS 2FA on this account
                    PhoneNumber = "+15555550123",
                    PhoneNumberConfirmed = true
                };

                await userManager.CreateAsync(adminUser, "Password1");
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
    }
}
