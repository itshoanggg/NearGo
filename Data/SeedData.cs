using Microsoft.AspNetCore.Identity;
using NearGo.Models;

namespace NearGo.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            var alreadySeeded = await roleManager.RoleExistsAsync("Admin");
            if (!alreadySeeded)
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
                await roleManager.CreateAsync(new IdentityRole("Supermarket"));
                await roleManager.CreateAsync(new IdentityRole("Customer"));

                var admin = new AppUser
                {
                    UserName = "admin@neargo.vn",
                    Email = "admin@neargo.vn",
                    FullName = "Quản trị viên NearGo",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                await userManager.CreateAsync(admin, "Admin@123");
                await userManager.AddToRoleAsync(admin, "Admin");
            }
        }
    }
}
