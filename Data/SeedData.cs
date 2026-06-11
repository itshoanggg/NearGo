using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

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

            if (!await context.Categories.AnyAsync())
            {
                var categories = new List<Category>
                {
                    new()
                    {
                        Name = "Thực phẩm tươi sống",
                        Slug = "thuc-pham-tuoi-song",
                        Description = "Rau củ quả, thịt cá, trứng và các thực phẩm tươi sống khác",
                        IconClass = "fas fa-apple-alt",
                        SortOrder = 1,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Name = "Thực phẩm đông lạnh",
                        Slug = "thuc-pham-dong-lanh",
                        Description = "Thực phẩm cấp đông, bảo quản lạnh",
                        IconClass = "fas fa-snowflake",
                        SortOrder = 2,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Name = "Đồ uống & Giải khát",
                        Slug = "do-uong-giai-khat",
                        Description = "Nước ngọt, nước khoáng, trà, cà phê và các loại đồ uống",
                        IconClass = "fas fa-wine-bottle",
                        SortOrder = 3,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Name = "Sữa & Sản phẩm từ sữa",
                        Slug = "sua-va-san-pham-tu-sua",
                        Description = "Sữa tươi, sữa chua, phô mai, bơ và các chế phẩm từ sữa",
                        IconClass = "fas fa-tag",
                        SortOrder = 4,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Name = "Bánh kẹo & Snack",
                        Slug = "banh-keo-snack",
                        Description = "Bánh quy, kẹo, snack, chocolate và đồ ăn vặt",
                        IconClass = "fas fa-cookie-bite",
                        SortOrder = 5,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Name = "Gia vị & Nguyên liệu nấu ăn",
                        Slug = "gia-vi-nguyen-lieu-nau-an",
                        Description = "Dầu ăn, nước mắm, hạt nêm, gia vị và nguyên liệu chế biến món ăn",
                        IconClass = "fas fa-mortar-pestle",
                        SortOrder = 6,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Name = "Mì, Cháo, Bún, Phở",
                        Slug = "mi-chao-bun-pho",
                        Description = "Mì gói, miến, bún, phở khô, cháo ăn liền",
                        IconClass = "fas fa-utensils",
                        SortOrder = 7,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Name = "Đồ hộp & Đồ khô",
                        Slug = "do-hop-do-kho",
                        Description = "Đồ hộp, thực phẩm sấy khô, các loại hạt, nấm",
                        IconClass = "fas fa-tag",
                        SortOrder = 8,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Name = "Chăm sóc cá nhân",
                        Slug = "cham-soc-ca-nhan",
                        Description = "Sữa tắm, dầu gội, kem đánh răng, khăn giấy và sản phẩm vệ sinh cá nhân",
                        IconClass = "fas fa-soap",
                        SortOrder = 9,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Name = "Đồ gia dụng",
                        Slug = "do-gia-dung",
                        Description = "Dụng cụ nhà bếp, đồ dùng gia đình, chất tẩy rửa",
                        IconClass = "fas fa-home",
                        SortOrder = 10,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    }
                };

                context.Categories.AddRange(categories);
                await context.SaveChangesAsync();
            }
        }
    }
}
