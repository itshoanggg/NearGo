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
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            await context.Database.EnsureCreatedAsync();

            await context.Database.ExecuteSqlRawAsync(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserFollowedSupermarkets')
                CREATE TABLE UserFollowedSupermarkets (
                    UserId nvarchar(450) NOT NULL,
                    SupermarketId int NOT NULL,
                    PRIMARY KEY (UserId, SupermarketId),
                    CONSTRAINT FK_UserFollowed_Users FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE,
                    CONSTRAINT FK_UserFollowed_Supermarkets FOREIGN KEY (SupermarketId) REFERENCES Supermarkets(Id) ON DELETE CASCADE
                )");

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

            var categories = new List<Category>
{
    new Category
    {
        Name = "Sữa & Chế phẩm từ sữa",
        Slug = "sua-che-pham-tu-sua",
        Description = "Sữa tươi, sữa chua, phô mai và các chế phẩm từ sữa",
        ImageUrl = "https://images.unsplash.com/photo-1550583724-b2692b85b150?q=80&w=400",
        IconClass = "fas fa-glass-whiskey",
        SortOrder = 1
    },

    new Category
    {
        Name = "Thịt & Hải sản",
        Slug = "thit-hai-san",
        Description = "Thịt heo, thịt bò, thịt gà và hải sản tươi sống",
        ImageUrl = "https://images.unsplash.com/photo-1603048297172-c92544798d5a?q=80&w=400",
        IconClass = "fas fa-drumstick-bite",
        SortOrder = 2
    },

    new Category
    {
        Name = "Rau củ quả",
        Slug = "rau-cu-qua",
        Description = "Rau xanh, củ quả tươi nhập khẩu",
        ImageUrl = "https://images.unsplash.com/photo-1540420773420-3366772f4999?q=80&w=400",
        IconClass = "fas fa-carrot",
        SortOrder = 3
    },

    new Category
    {
        Name = "Bánh kẹo & Snack",
        Slug = "banh-keo-snack",
        Description = "Bánh quy, kẹo, snack các loại",
        ImageUrl = "https://images.unsplash.com/photo-1558961363-fa8fdf82db35?q=80&w=400",
        IconClass = "fas fa-cookie-bite",
        SortOrder = 4
    },

    new Category
    {
        Name = "Nước giải khát",
        Slug = "nuoc-giai-khat",
        Description = "Nước ngọt, nước khoáng, trà, cà phê",
        ImageUrl = "https://images.unsplash.com/photo-1544787219-7f47ccb76574?q=80&w=400",
        IconClass = "fas fa-wine-bottle",
        SortOrder = 5
    },

    new Category
    {
        Name = "Gia vị & Nguyên liệu",
        Slug = "gia-vi-nguyen-lieu",
        Description = "Gia vị nấu ăn, nguyên liệu chế biến",
        ImageUrl = "https://images.unsplash.com/photo-1474979266404-7eaacbcd87c5?q=80&w=400",
        IconClass = "fas fa-mortar-pestle",
        SortOrder = 6
    },

    new Category
    {
        Name = "Mì & Đồ khô",
        Slug = "mi-do-kho",
        Description = "Mì gói, miến, bún, nui và đồ khô",
        ImageUrl = "https://images.unsplash.com/photo-1617093727343-374698b1b08d?q=80&w=400",
        IconClass = "fas fa-egg",
        SortOrder = 7
    },

    new Category
    {
        Name = "Đồ đông lạnh",
        Slug = "do-dong-lanh",
        Description = "Thực phẩm đông lạnh tiện lợi",
        ImageUrl = "https://images.unsplash.com/photo-1510130387422-82bed34b37e9?q=80&w=400",
        IconClass = "fas fa-snowflake",
        SortOrder = 8
    },

    new Category
    {
        Name = "Đồ uống có cồn",
        Slug = "do-uong-co-con",
        Description = "Bia, rượu vang và đồ uống có cồn",
        ImageUrl = "https://images.unsplash.com/photo-1514362545857-3bc16c4c7d1b?q=80&w=400",
        IconClass = "fas fa-wine-glass-alt",
        SortOrder = 9
    },

    new Category
    {
        Name = "Chăm sóc nhà cửa",
        Slug = "cham-soc-nha-cua",
        Description = "Nước rửa chén, nước lau sàn, chất tẩy rửa",
        ImageUrl = "https://images.unsplash.com/photo-1583947582886-f40ec95dd752?q=80&w=400",
        IconClass = "fas fa-soap",
        SortOrder = 10
    },

    new Category
    {
        Name = "Chăm sóc cá nhân",
        Slug = "cham-soc-ca-nhan",
        Description = "Sữa tắm, dầu gội, kem đánh răng",
        ImageUrl = "https://images.unsplash.com/photo-1526947425960-945c6e72858f?q=80&w=400",
        IconClass = "fas fa-hand-sparkles",
        SortOrder = 11
    },

    new Category
    {
        Name = "Đồ ăn vặt",
        Slug = "do-an-vat",
        Description = "Đồ ăn vặt nhập khẩu, local brand",
        ImageUrl = "https://images.unsplash.com/photo-1512058564366-18510be2db19?q=80&w=400",
        IconClass = "fas fa-candy-cane",
        SortOrder = 12
    },

    new Category
    {
        Name = "Bia & Đồ nhậu",
        Slug = "bia-do-nhau",
        Description = "Bia tươi, bia chai và đồ nhậu",
        ImageUrl = "https://images.unsplash.com/photo-1566633806327-68e152aaf26d?q=80&w=400",
        IconClass = "fas fa-beer",
        SortOrder = 13
    },

    new Category
    {
        Name = "Sản phẩm Organic",
        Slug = "san-pham-organic",
        Description = "Sản phẩm hữu cơ, sạch, an toàn",
        ImageUrl = "https://images.unsplash.com/photo-1490818387583-1baba5e638af?q=80&w=400",
        IconClass = "fas fa-leaf",
        SortOrder = 14
    },

    new Category
    {
        Name = "Quà tặng & Set quà",
        Slug = "qua-tang-set-qua",
        Description = "Hộp quà tặng, set quà Tết",
        ImageUrl = "https://images.unsplash.com/photo-1512909006721-3d6018887383?q=80&w=400",
        IconClass = "fas fa-gift",
        SortOrder = 15
    }
};
            context.Categories.AddRange(categories);
            await context.SaveChangesAsync();

            var supermarkets = new List<Supermarket>();
            var supermarketUsers = new List<(string name, string email, string phone, string address, string slug, string desc, string logo, string cover, bool verified, string tier)>()
{
    ("VinMart+ Thủ Đức", "market1@neargo.vn", "0901122334", "123 Lê Văn Việt, Thủ Đức, TP.HCM", "vinmart-thu-duc", "VinMart+ cung cấp thực phẩm sạch, an toàn với giá tốt nhất thị trường.", "https://images.unsplash.com/photo-1520607162513-77705c0f0d4a?q=80&w=200", "https://images.unsplash.com/photo-1542838132-92c53300491e?q=80&w=1200", true, "Premium"),

    ("Co.op Food Bình Thạnh", "market2@neargo.vn", "0902233445", "456 Nguyễn Xí, Bình Thạnh, TP.HCM", "coop-food-binh-thanh", "Hệ thống siêu thị thực phẩm Co.op Food với hàng ngàn sản phẩm chất lượng.", "https://images.unsplash.com/photo-1517248135467-4c7edcad34c4?q=80&w=200", "https://images.unsplash.com/photo-1550989460-0adf9ea622e2?q=80&w=1200", true, "Premium"),

    ("AEON Food Market", "market3@neargo.vn", "0903344556", "789 Tân Phú, Quận 7, TP.HCM", "aeon-food-market", "AEON Food Market - Siêu thị Nhật Bản với thực phẩm nhập khẩu chất lượng cao.", "https://images.unsplash.com/photo-1495474472287-4d71bcdd2085?q=80&w=200", "https://images.unsplash.com/photo-1515169067868-5387ec356754?q=80&w=1200", true, "Premium"),

    ("Bách Hóa Xanh Quận 1", "market4@neargo.vn", "0904455667", "12 Nguyễn Huệ, Quận 1, TP.HCM", "bach-hoa-xanh-q1", "Bách Hóa Xanh - thực phẩm tươi sống giá gốc mỗi ngày.", "https://images.unsplash.com/photo-1488459716781-31db52582fe9?q=80&w=200", "https://images.unsplash.com/photo-1542838132-92c53300491e?q=80&w=1200", true, "Standard"),

    ("Mart Vạn Phúc", "market5@neargo.vn", "0905566778", "345 Vạn Phúc, Hà Đông, Hà Nội", "mart-van-phuc", "Mart Vạn Phúc - siêu thị gia đình với hơn 10 năm kinh nghiệm.", "https://images.unsplash.com/photo-1514933651103-005eec06c04b?q=80&w=200", "https://images.unsplash.com/photo-1506617420156-8e4536971650?q=80&w=1200", false, "Standard"),

    ("Green Food Đà Nẵng", "market6@neargo.vn", "0906677889", "567 Nguyễn Văn Linh, Đà Nẵng", "green-food-da-nang", "Thực phẩm xanh, sạch, an toàn cho mọi gia đình.", "https://images.unsplash.com/photo-1521017432531-fbd92d768814?q=80&w=200", "https://images.unsplash.com/photo-1490818387583-1baba5e638af?q=80&w=1200", true, "Premium"),

    ("Fresh Market Cần Thơ", "market7@neargo.vn", "0907788990", "89 Mậu Thân, Ninh Kiều, Cần Thơ", "fresh-market-can-tho", "Fresh Market - Nông sản sạch từ đồng bằng sông Cửu Long.", "https://images.unsplash.com/photo-1466978913421-dad2ebd01d17?q=80&w=200", "https://images.unsplash.com/photo-1488459716781-31db52582fe9?q=80&w=1200", false, "Free"),

    ("Satra Food Biên Hòa", "market8@neargo.vn", "0908899001", "234 Phạm Văn Thuật, Biên Hòa, Đồng Nai", "satra-food-bien-hoa", "Satra Food - Hệ thống thực phẩm an toàn số 1 Đồng Nai.", "https://images.unsplash.com/photo-1498837167922-ddd27525d352?q=80&w=200", "https://images.unsplash.com/photo-1550989460-0adf9ea622e2?q=80&w=1200", true, "Standard"),

    ("Market Hải Phòng", "market9@neargo.vn", "0909900112", "456 Lạch Tray, Ngô Quyền, Hải Phòng", "market-hai-phong", "Siêu thị thực phẩm Hải Phòng với hải sản tươi ngon mỗi ngày.", "https://images.unsplash.com/photo-1504674900247-0877df9cc836?q=80&w=200", "https://images.unsplash.com/photo-1515169067868-5387ec356754?q=80&w=1200", false, "Free"),

    ("Eco Food Huế", "market10@neargo.vn", "0910011223", "78 Trần Hưng Đạo, Huế", "eco-food-hue", "Thực phẩm Organic và đặc sản Huế.", "https://images.unsplash.com/photo-1512621776951-a57141f2eefd?q=80&w=200", "https://images.unsplash.com/photo-1490818387583-1baba5e638af?q=80&w=1200", false, "Standard"),

    ("Market Nha Trang", "market11@neargo.vn", "0911122334", "123 Nguyễn Trãi, Nha Trang, Khánh Hòa", "market-nha-trang", "Hải sản tươi sống Nha Trang chất lượng cao.", "https://images.unsplash.com/photo-1502741338009-cac2772e18bc?q=80&w=200", "https://images.unsplash.com/photo-1542838132-92c53300491e?q=80&w=1200", true, "Standard"),

    ("Foody Market Vũng Tàu", "market12@neargo.vn", "0912233445", "567 Thùy Vân, Vũng Tàu", "foody-market-vung-tau", "Thực phẩm nhập khẩu và hải sản Vũng Tàu.", "https://images.unsplash.com/photo-1523475472560-d2df97ec485c?q=80&w=200", "https://images.unsplash.com/photo-1550989460-0adf9ea622e2?q=80&w=1200", false, "Free"),

    ("Organic Mart Đà Lạt", "market13@neargo.vn", "0913344556", "89 Nguyễn Văn Cừ, Đà Lạt", "organic-mart-da-lat", "Rau củ Organic Đà Lạt - Nông sản sạch cao nguyên.", "https://images.unsplash.com/photo-1471193945509-9ad0617afabf?q=80&w=200", "https://images.unsplash.com/photo-1490645935967-10de6ba17061?q=80&w=1200", true, "Premium"),

    ("Mart Quy Nhơn", "market14@neargo.vn", "0914455667", "234 Trần Hưng Đạo, Quy Nhơn", "mart-quy-nhon", "Hải sản và thực phẩm tươi Quy Nhơn.", "https://images.unsplash.com/photo-1515003197210-e0cd71810b5f?q=80&w=200", "https://images.unsplash.com/photo-1515169067868-5387ec356754?q=80&w=1200", false, "Free"),

    ("City Food Cần Thơ", "market15@neargo.vn", "0915566778", "456 Nguyễn Văn Cừ, Ninh Kiều, Cần Thơ", "city-food-can-tho", "City Food - Thực phẩm thành phố miền Tây.", "https://images.unsplash.com/photo-1504674900247-0877df9cc836?q=80&w=200", "https://images.unsplash.com/photo-1490818387583-1baba5e638af?q=80&w=1200", false, "Standard"),

    ("Sài Gòn Food", "market16@neargo.vn", "0916677889", "789 Lý Thường Kiệt, Quận 10, TP.HCM", "sai-gon-food", "Sài Gòn Food - Thực phẩm Sài Gòn với đa dạng sản phẩm.", "https://images.unsplash.com/photo-1520607162513-77705c0f0d4a?q=80&w=200", "https://images.unsplash.com/photo-1542838132-92c53300491e?q=80&w=1200", true, "Premium"),

    ("Metro An Phú", "market17@neargo.vn", "0917788990", "12 Cao Thắng, Quận 3, TP.HCM", "metro-an-phu", "Metro An Phú - Hàng nhập khẩu và bán sỉ.", "https://images.unsplash.com/photo-1495474472287-4d71bcdd2085?q=80&w=200", "https://images.unsplash.com/photo-1550989460-0adf9ea622e2?q=80&w=1200", true, "Standard"),

    ("Happy Food Bình Dương", "market18@neargo.vn", "0918899001", "567 Đại lộ Bình Dương, Thủ Dầu Một", "happy-food-binh-duong", "Happy Food - Giao hàng nhanh thực phẩm sạch.", "https://images.unsplash.com/photo-1466978913421-dad2ebd01d17?q=80&w=200", "https://images.unsplash.com/photo-1515169067868-5387ec356754?q=80&w=1200", false, "Free"),

    ("Food Center Đà Nẵng", "market19@neargo.vn", "0919900112", "234 Hùng Vương, Đà Nẵng", "food-center-da-nang", "Food Center - Thực phẩm Đà Nẵng giá gốc.", "https://images.unsplash.com/photo-1512621776951-a57141f2eefd?q=80&w=200", "https://images.unsplash.com/photo-1490818387583-1baba5e638af?q=80&w=1200", true, "Standard"),

    ("Best Food HCM", "market20@neargo.vn", "0920011223", "345 Nguyễn Đình Chiểu, Quận 1, TP.HCM", "best-food-hcm", "Best Food - Thực phẩm chất lượng nhất cho bạn.", "https://images.unsplash.com/photo-1488459716781-31db52582fe9?q=80&w=200", "https://images.unsplash.com/photo-1542838132-92c53300491e?q=80&w=1200", true, "Premium")
};

            var supermarketIds = new List<int>();
            foreach (var (name, email, phone, address, slug, desc, logo, cover, verified, tier) in supermarketUsers)
            {
                var sm = new Supermarket
                {
                    Name = name,
                    Slug = slug,
                    Description = desc,
                    LogoUrl = logo,
                    CoverImageUrl = cover,
                    Address = address,
                    Phone = phone,
                    Email = email,
                    IsActive = true,
                    IsVerified = verified,
                    SubscriptionTier = tier,
                    SubscriptionExpiry = DateTime.UtcNow.AddYears(1),
                    Rating = Random.Shared.NextDouble() * 2 + 3,
                    ReviewCount = Random.Shared.Next(10, 500)
                };
                context.Supermarkets.Add(sm);
                await context.SaveChangesAsync();
                supermarketIds.Add(sm.Id);

                var user = new AppUser
                {
                    UserName = email,
                    Email = email,
                    FullName = name,
                    PhoneNumber = phone,
                    EmailConfirmed = true,
                    IsActive = true,
                    SupermarketId = sm.Id,
                    CreatedAt = DateTime.UtcNow
                };
                await userManager.CreateAsync(user, "Market@123");
                await userManager.AddToRoleAsync(user, "Supermarket");
            }

            var customers = new List<(string name, string email, string phone, string address)>
            {
                ("Nguyễn Văn An", "customer1@neargo.vn", "0901010101", "123 Nguyễn Trãi, Quận 1, TP.HCM"),
                ("Trần Thị Bình", "customer2@neargo.vn", "0902020202", "456 Lê Lợi, Quận 3, TP.HCM"),
                ("Lê Hoàng Cường", "customer3@neargo.vn", "0903030303", "789 Hai Bà Trưng, Quận 1, TP.HCM"),
                ("Phạm Thị Dung", "customer4@neargo.vn", "0904040404", "12 Nguyễn Huệ, Quận Bình Thạnh, TP.HCM"),
                ("Hoàng Văn Em", "customer5@neargo.vn", "0905050505", "34 Trần Phú, Quận 5, TP.HCM"),
                ("Võ Thị Phượng", "customer6@neargo.vn", "0906060606", "56 Lý Tự Trọng, Quận 1, TP.HCM"),
                ("Đặng Minh Giàu", "customer7@neargo.vn", "0907070707", "78 Nguyễn Đình Chiểu, Quận 3, TP.HCM"),
                ("Bùi Thị Hạnh", "customer8@neargo.vn", "0908080808", "90 Cách Mạng Tháng 8, Quận 10, TP.HCM"),
                ("Ngô Văn Ý", "customer9@neargo.vn", "0909090909", "123 Võ Văn Tần, Quận 3, TP.HCM"),
                ("Dương Thị Kim", "customer10@neargo.vn", "0910101010", "456 Điện Biên Phủ, Quận Bình Thạnh, TP.HCM")
            };

            foreach (var (name, email, phone, address) in customers)
            {
                var user = new AppUser
                {
                    UserName = email,
                    Email = email,
                    FullName = name,
                    PhoneNumber = phone,
                    Address = address,
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                await userManager.CreateAsync(user, "Customer@123");
                await userManager.AddToRoleAsync(user, "Customer");
            }

            var productData = new List<(string name, decimal price, double disc, int stock, DateTime expiry, string img, int catIdx, string unit, string origin, string desc, string tags)>
{
    ("TH True Milk 1L", 45000, 20, 50, DateTime.UtcNow.AddDays(5), "https://images.unsplash.com/photo-1550583724-b2692b85b150?q=80&w=400", 0, "hộp", "Việt Nam", "Sữa tươi TH True Milk tiệt trùng 1L, hạn sử dụng còn 5 ngày", "sữa,th true milk,tiệt trùng"),

    ("Sữa chua Vinamilk 4 hộp", 35000, 30, 100, DateTime.UtcNow.AddDays(3), "https://images.unsplash.com/photo-1488477181946-6428a0291777?q=80&w=400", 0, "lốc", "Việt Nam", "Sữa chua Vinamilk hương dâu, 4 hộp/lốc", "sữa chua,vinamilk,đường"),

    ("Phô mai Con Bò Cười 48g", 25000, 25, 80, DateTime.UtcNow.AddDays(7), "https://images.unsplash.com/photo-1486297678162-eb2a19b0a32d?q=80&w=400", 0, "gói", "Việt Nam", "Phô mai Con Bò Cười vị nguyên bản", "phô mai,con bò cười"),

    ("Ba rọi heo Mỹ 500g", 85000, 35, 30, DateTime.UtcNow.AddDays(2), "https://images.unsplash.com/photo-1603048297172-c92544798d5a?q=80&w=400", 1, "kg", "Mỹ", "Ba rọi heo Mỹ nhập khẩu, thịt tươi ngon", "thịt heo,ba rọi,mỹ"),

    ("Đùi gà tươi 1kg", 65000, 25, 40, DateTime.UtcNow.AddDays(4), "https://images.unsplash.com/photo-1604503468506-a8da13d82791?q=80&w=400", 1, "kg", "Việt Nam", "Đùi gà tươi, thịt chắc, ngọt", "đùi gà,thịt gà"),

   ("Tôm sú size lớn 500g", 120000, 40, 20, DateTime.UtcNow.AddDays(1), "https://images.unsplash.com/photo-1510130387422-82bed34b37e9?q=80&w=400", 1, "kg", "Việt Nam", "Tôm sú tươi size lớn, đánh bắt tự nhiên", "tôm sú,hải sản"),

    ("Cá hồi Nauy phi lê 200g", 150000, 20, 15, DateTime.UtcNow.AddDays(6), "https://images.unsplash.com/photo-1599084993091-1cb5c0721cc6?q=80&w=400", 1, "kg", "Nauy", "Cá hồi Nauy phi lê nguyên vỉ, giàu Omega-3", "cá hồi,nauy,phi lê"),

    ("Bông cải xanh Đà Lạt 500g", 25000, 30, 60, DateTime.UtcNow.AddDays(3), "https://images.unsplash.com/photo-1459411621453-7b03977f4bfc?q=80&w=400", 2, "bông", "Việt Nam", "Bông cải xanh tươi Đà Lạt, không thuốc trừ sâu", "bông cải,đà lạt,rau"),

    ("Cà rốt hữu cơ 1kg", 30000, 25, 70, DateTime.UtcNow.AddDays(5), "https://images.unsplash.com/photo-1447175008436-054170c2e979?q=80&w=400", 2, "kg", "Việt Nam", "Cà rốt hữu cơ, ngọt, giàu vitamin A", "cà rốt,hữu cơ"),

    ("Cải thảo 1kg", 20000, 20, 80, DateTime.UtcNow.AddDays(4), "https://images.unsplash.com/photo-1518977676601-b53f82aba655?q=80&w=400", 2, "kg", "Việt Nam", "Cải thảo tươi, giòn, ngọt", "cải thảo,rau xanh"),

    ("Oreo vị sữa 97g", 15000, 30, 120, DateTime.UtcNow.AddDays(10), "https://images.unsplash.com/photo-1558961363-fa8fdf82db35?q=80&w=400", 3, "gói", "Mỹ", "Bánh Oreo vị sữa, nhân kem vani", "oreo,bánh,quy"),

    ("Snack Poca 60g", 10000, 35, 150, DateTime.UtcNow.AddDays(8), "https://images.unsplash.com/photo-1566478989037-eec170784d0b?q=80&w=400", 3, "gói", "Việt Nam", "Snack Poca khoai tây vị BBQ", "snack,poca,khoai tây"),

    ("Kẹo Socola Ferrero 8 viên", 85000, 25, 40, DateTime.UtcNow.AddDays(15), "https://images.unsplash.com/photo-1549007994-cb92caebd54b?q=80&w=400", 3, "hộp", "Ý", "Kẹo Socola Ferrero Rocher nhập khẩu Ý", "socola,ferrero,kẹo"),

    ("Coca Cola Zero 1.5L", 15000, 20, 200, DateTime.UtcNow.AddDays(20), "https://images.unsplash.com/photo-1622483767028-3f66f32aef97?q=80&w=400", 4, "chai", "Mỹ", "Coca Cola Zero không đường, 1.5L", "coca cola,nước ngọt,zero"),

    ("Trà xanh Ocha 0.5L", 12000, 25, 180, DateTime.UtcNow.AddDays(12), "https://images.unsplash.com/photo-1544787219-7f47ccb76574?q=80&w=400", 4, "chai", "Việt Nam", "Trà xanh Ocha ít ngọt, thanh mát", "trà xanh,ocha"),

    ("Nước suối Aquafina 1L", 8000, 15, 300, DateTime.UtcNow.AddDays(30), "https://images.unsplash.com/photo-1564419320461-6870880221ad?q=80&w=400", 4, "chai", "Việt Nam", "Nước suối Aquafina tinh khiết", "nước suối,aquafina"),

    ("Dầu ăn Tường An 1L", 40000, 20, 60, DateTime.UtcNow.AddDays(14), "https://images.unsplash.com/photo-1474979266404-7eaacbcd87c5?q=80&w=400", 5, "chai", "Việt Nam", "Dầu ăn Tường An tinh luyện từ dầu thực vật", "dầu ăn,tường an"),

    ("Nước mắm Nam Ngư 500ml", 35000, 25, 50, DateTime.UtcNow.AddDays(20), "https://images.unsplash.com/photo-1515003197210-e0cd71810b5f?q=80&w=400", 5, "chai", "Việt Nam", "Nước mắm cốt Nam Ngư đậm đà", "nước mắm,nam ngư"),

    ("Hạt nêm Knorr 200g", 20000, 30, 90, DateTime.UtcNow.AddDays(18), "https://images.unsplash.com/photo-1509440159596-0249088772ff?q=80&w=400", 5, "gói", "Việt Nam", "Hạt nêm Knorr từ thịt heo thượng hạng", "hạt nêm,knorr"),

    ("Mì Hảo Hảo 30 gói", 85000, 20, 100, DateTime.UtcNow.AddDays(25), "https://images.unsplash.com/photo-1617093727343-374698b1b08d?q=80&w=400", 6, "thùng", "Việt Nam", "Mì gói Hảo Hảo tôm chua cay, thùng 30 gói", "mì,hảo hảo,gói"),

    ("Miến dong Việt 500g", 25000, 25, 70, DateTime.UtcNow.AddDays(22), "https://images.unsplash.com/photo-1585032226651-759b368d7246?q=80&w=400", 6, "gói", "Việt Nam", "Miến dong Việt sạch, dai ngon", "miến dong,miến"),

    ("Cá basa đông lạnh phi lê 500g", 45000, 35, 35, DateTime.UtcNow.AddDays(7), "https://images.unsplash.com/photo-1510130387422-82bed34b37e9?q=80&w=400", 7, "gói", "Việt Nam", "Cá basa phi lê đông lạnh sạch", "cá basa,đông lạnh"),

    ("Đậu hũ non Đại Phát 400g", 15000, 25, 80, DateTime.UtcNow.AddDays(4), "https://images.unsplash.com/photo-1528735602780-2552fd46c7af?q=80&w=400", 7, "cây", "Việt Nam", "Đậu hũ non Đại Phát, mềm mịn", "đậu hũ,đậu phụ"),

    ("Sáp thơm Febreze 300ml", 45000, 30, 60, DateTime.UtcNow.AddDays(30), "https://images.unsplash.com/photo-1583947582886-f40ec95dd752?q=80&w=400", 9, "chai", "Mỹ", "Sáp thơm phòng Febreze hương oải hương", "febreze,sáp thơm"),

    ("Dầu gội Sunsilk 650ml", 85000, 20, 45, DateTime.UtcNow.AddDays(40), "https://images.unsplash.com/photo-1526947425960-945c6e72858f?q=80&w=400", 10, "chai", "Thái Lan", "Dầu gội Sunsilk mượt mà tổ ong", "dầu gội,sunsilk"),

    ("Kem đánh răng P/S 225g", 35000, 25, 80, DateTime.UtcNow.AddDays(50), "https://imgs.search.brave.com/dhtjk6dCiqb0ebtFcVAUT-LjpJFeAmZp-pjOKQ8uDsA/rs:fit:860:0:0:0/g:ce/aHR0cHM6Ly9jZG4u/dGdkZC52bi8vTmV3/cy8xNDU4NTMzLy9o/dW9uZy1kYW4tY2Fj/aC1jaG9uLWtpbmgt/bWF0LXBodS1ob3At/dm9pLWtodW9uLW1h/dC0zLTg0NXg1NTAu/anBn?q=80&w=400", 10, "tuýp", "Việt Nam", "Kem đánh răng P/S trắng sáng", "kem đánh răng,p/s"),

    ("Bánh tráng trộn 500g", 25000, 20, 100, DateTime.UtcNow.AddDays(15), "https://images.unsplash.com/photo-1512058564366-18510be2db19?q=80&w=400", 11, "gói", "Việt Nam", "Bánh tráng trộn sẵn gia vị", "bánh tráng,đồ ăn vặt"),

    ("Hạt dẻ cười rang muối 200g", 65000, 30, 40, DateTime.UtcNow.AddDays(20), "https://images.unsplash.com/photo-1599599810769-bcde5a160d32?q=80&w=400", 11, "gói", "Mỹ", "Hạt dẻ cười rang muối nhập khẩu", "hạt dẻ,hạt khô"),

    ("Rau hữu cơ Mix 500g", 35000, 30, 50, DateTime.UtcNow.AddDays(3), "https://images.unsplash.com/photo-1540420773420-3366772f4999?q=80&w=400", 13, "túi", "Việt Nam", "Bộ rau hữu cơ mix các loại", "rau hữu cơ,organic"),

    ("Set quà Tết Luxury 2025", 500000, 20, 10, DateTime.UtcNow.AddDays(60), "https://images.unsplash.com/photo-1512909006721-3d6018887383?q=80&w=400", 14, "set", "Việt Nam", "Hộp quà Tết cao cấp với rượu vang, socola, trà", "set quà,tết,quà tặng"),

    ("Snack Lays 60g", 12000, 25, 180, DateTime.UtcNow.AddDays(14), "https://images.unsplash.com/photo-1566478989037-eec170784d0b?q=80&w=400", 3, "gói", "Mỹ", "Snack Lays khoai tây vị tự nhiên", "snack,lays,khoai tây"),

    ("Bánh Cosy 192g", 25000, 20, 90, DateTime.UtcNow.AddDays(20), "https://images.unsplash.com/photo-1509440159596-0249088772ff?q=80&w=400", 3, "gói", "Việt Nam", "Bánh Cosy mềm bơ", "bánh cosy,bơ"),

};

            var products = new List<Product>();
            var rand = new Random();
            int productCount = 0;
            foreach (var (name, price, disc, stock, expiry, img, catIdx, unit, origin, desc, tags) in productData)
            {
                int smId = supermarketIds[productCount % supermarketIds.Count];
                var discountedPrice = price - (price * (decimal)(disc / 100));
                var prod = new Product
                {
                    Name = name,
                    Slug = name.ToLower().Replace(" ", "-").Replace(",", "").Replace(".", "").Replace("&", "va").Replace("/", "-") + "-" + Guid.NewGuid().ToString().Substring(0, 6),
                    Description = desc,
                    OriginalPrice = price,
                    DiscountedPrice = Math.Round(discountedPrice, 0),
                    DiscountPercentage = disc,
                    StockQuantity = stock,
                    ExpiryDate = expiry,
                    ImageUrl = img,
                    Unit = unit,
                    Origin = origin,
                    IsActive = true,
                    IsBoosted = productCount < 10,
                    BoostExpiry = productCount < 10 ? DateTime.UtcNow.AddDays(30) : null,
                    ViewCount = rand.Next(50, 5000),
                    SoldCount = rand.Next(10, 500),
                    SmartExpiryScore = Math.Round(100 - ((DateTime.UtcNow.AddDays(60) - expiry).TotalDays / 60 * 100), 1),
                    Tags = tags,
                    CategoryId = categories[catIdx].Id,
                    SupermarketId = smId,
                    CreatedAt = DateTime.UtcNow.AddDays(-rand.Next(1, 30))
                };
                products.Add(prod);
                productCount++;
            }
            context.Products.AddRange(products);
            await context.SaveChangesAsync();

            var vouchers = new List<Voucher>();
            for (int i = 0; i < 30; i++)
            {
                vouchers.Add(new Voucher
                {
                    Code = $"NEARGO{rand.Next(1000, 9999)}",
                    Title = $"Khuyến mãi {rand.Next(5, 30)}%",
                    Description = "Giảm giá cho đơn hàng từ 100,000đ",
                    DiscountType = rand.Next(2) == 0 ? "Percentage" : "Fixed",
                    DiscountValue = rand.Next(2) == 0 ? rand.Next(5, 30) : rand.Next(10000, 50000),
                    MaxDiscountAmount = 100000,
                    MinOrderAmount = 100000,
                    MaxUsage = 100,
                    CurrentUsage = rand.Next(0, 50),
                    SupermarketId = i < 20 ? supermarketIds[i % supermarketIds.Count] : null,
                    StartDate = DateTime.UtcNow.AddDays(-5),
                    ExpiryDate = DateTime.UtcNow.AddDays(rand.Next(5, 30)),
                    IsActive = true
                });
            }
            context.Vouchers.AddRange(vouchers);
            await context.SaveChangesAsync();
            }
        }
    }
}
