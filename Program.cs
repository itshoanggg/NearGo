using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NearGo.Configurations;
using NearGo.Data;
using NearGo.Hubs;
using NearGo.Models;
using NearGo.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/auth/login";
    options.LogoutPath = "/auth/logout";
    options.AccessDeniedPath = "/auth/access-denied";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("Supermarket", policy => policy.RequireRole("Supermarket"));
    options.AddPolicy("Customer", policy => policy.RequireRole("Customer"));
});

builder.Services.Configure<SEPaySettings>(builder.Configuration.GetSection("SEPay"));

builder.Services.Configure<OpenAISettings>(builder.Configuration.GetSection("OpenAI"));
builder.Services.Configure<GeminiSettings>(builder.Configuration.GetSection("Gemini"));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));

builder.Services.AddHttpClient<OpenAIService>();
builder.Services.AddHttpClient<GeminiService>();
builder.Services.AddScoped<ChatbotContextService>();
builder.Services.AddScoped<SEPayService>();
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<FinanceService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddHostedService<ExpiryDiscountService>();

builder.Services.AddSignalR();
builder.Services.AddRazorPages();

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "NearGo-Antiforgery";
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapHub<NotificationHub>("/notificationHub");

async Task<IResult> HandleSepayWebhook(HttpContext context, SEPayService sePayService)
{
    try
    {
        var token = context.Request.Query["token"].ToString();
        var settingsToken = sePayService.GetWebhookToken();
        if (!string.IsNullOrEmpty(settingsToken) && token != settingsToken)
        {
            return Results.Unauthorized();
        }

        string body;
        using (var reader = new StreamReader(context.Request.Body))
        {
            body = await reader.ReadToEndAsync();
        }

        var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(body);
        if (data == null)
        {
            return Results.BadRequest(new { message = "Invalid JSON" });
        }

        var content = data.GetValueOrDefault("content")?.ToString() ?? "";
        var gateway = data.GetValueOrDefault("gateway")?.ToString() ?? "";
        var transferAmountStr = data.GetValueOrDefault("transferAmount")?.ToString() ?? "0";
        var transactionId = data.GetValueOrDefault("referenceCode")?.ToString()
            ?? data.GetValueOrDefault("id")?.ToString() ?? "";

        if (string.IsNullOrEmpty(content) || !content.Contains("SEVQR"))
        {
            return Results.Ok(new { message = "Ignored - not SEVQR transfer" });
        }

        var tkpIndex = content.IndexOf("TKP", StringComparison.OrdinalIgnoreCase);
        if (tkpIndex < 0)
        {
            return Results.Ok(new { message = "Ignored - no TKP code" });
        }

        var orderCode = content.Substring(tkpIndex + 3).Trim();
        if (string.IsNullOrEmpty(orderCode))
        {
            return Results.Ok(new { message = "Ignored - empty order code" });
        }

        using var scope = context.RequestServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var pending = await db.PendingCheckouts.FirstOrDefaultAsync(p => p.OrderCode == orderCode);
        if (pending == null)
        {
            return Results.Ok(new { message = "Pending checkout not found" });
        }

        var existingOrder = await db.Orders.FirstOrDefaultAsync(o => o.OrderCode == orderCode);
        if (existingOrder != null)
        {
            db.PendingCheckouts.Remove(pending);
            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Order already exists" });
        }

        decimal.TryParse(transferAmountStr, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var transferAmount);

        var cartItems = await db.CartItems
            .Include(c => c.Product)
            .Where(c => c.UserId == pending.UserId && c.Product.SupermarketId == pending.SupermarketId)
            .ToListAsync();

        if (!cartItems.Any())
        {
            return Results.Ok(new { message = "Cart is empty" });
        }

        var subTotal = cartItems.Sum(c => c.Product.DiscountedPrice * c.Quantity);
        decimal discountAmount = 0;
        decimal loyaltyDiscount = 0;

        if (pending.VoucherId.HasValue)
        {
            var voucher = await db.Vouchers.FindAsync(pending.VoucherId.Value);
            if (voucher != null && voucher.IsActive && voucher.CurrentUsage < voucher.MaxUsage
                && subTotal >= voucher.MinOrderAmount && voucher.ExpiryDate > DateTime.UtcNow)
            {
                discountAmount = voucher.DiscountType == "Percentage"
                    ? Math.Min(subTotal * voucher.DiscountValue / 100, voucher.MaxDiscountAmount)
                    : Math.Min(voucher.DiscountValue, subTotal);

                voucher.CurrentUsage++;
            }
        }

        if (pending.UsePoints)
        {
            var points = await db.LoyaltyPoints
                .Where(lp => lp.UserId == pending.UserId && lp.ExpiryDate > DateTime.UtcNow)
                .SumAsync(lp => (int?)lp.Points) ?? 0;

            if (points >= 1000)
            {
                loyaltyDiscount = 10000;
            }
        }

        var totalAmount = subTotal - discountAmount - loyaltyDiscount;
        if (totalAmount < 0) totalAmount = 0;

        var order = new Order
        {
            OrderCode = orderCode,
            CustomerId = pending.UserId,
            SupermarketId = pending.SupermarketId,
            SubTotal = subTotal,
            DiscountAmount = discountAmount,
            ShippingFee = 0,
            TotalAmount = totalAmount,
            VoucherId = pending.VoucherId,
            LoyaltyPointsUsed = pending.UsePoints ? 1000 : 0,
            LoyaltyDiscount = loyaltyDiscount,
            Status = "Confirmed",
            PaymentStatus = "Paid",
            PaymentMethod = "SEPay",
            TransactionId = transactionId,
            PaymentDate = DateTime.UtcNow,
            ShippingAddress = pending.ShippingAddress,
            CustomerName = pending.CustomerName,
            CustomerPhone = pending.CustomerPhone,
            CustomerNote = pending.Note,
            OrderDate = DateTime.UtcNow
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        foreach (var cartItem in cartItems)
        {
            var orderItem = new OrderItem
            {
                OrderId = order.Id,
                ProductId = cartItem.ProductId,
                Quantity = cartItem.Quantity,
                UnitPrice = cartItem.Product.DiscountedPrice,
                TotalPrice = cartItem.Product.DiscountedPrice * cartItem.Quantity
            };
            db.OrderItems.Add(orderItem);

            cartItem.Product.StockQuantity -= cartItem.Quantity;
            cartItem.Product.SoldCount += cartItem.Quantity;
        }
        db.CartItems.RemoveRange(cartItems);

        if (!pending.UsePoints)
        {
            db.LoyaltyPoints.Add(new LoyaltyPoint
            {
                UserId = pending.UserId,
                Points = 100,
                Source = "Purchase",
                Description = $"Mua hàng đơn {orderCode}",
                ExpiryDate = DateTime.UtcNow.AddMonths(6)
            });
        }
        else
        {
            db.LoyaltyPoints.Add(new LoyaltyPoint
            {
                UserId = pending.UserId,
                Points = -1000,
                Source = "Redemption",
                Description = $"Đổi 1000 điểm - đơn {orderCode}",
                ExpiryDate = DateTime.UtcNow.AddMonths(6)
            });
        }

        var paymentTransaction = new PaymentTransaction
        {
            OrderId = order.Id,
            PaymentMethod = "SEPay",
            TransactionId = transactionId,
            BankCode = gateway,
            Amount = transferAmount > 0 ? transferAmount : totalAmount,
            Status = "Success",
            ResponseCode = "00",
            ResponseMessage = "Thanh toán thành công qua SEPay",
            CreatedAt = DateTime.UtcNow,
            PaidAt = DateTime.UtcNow
        };
        db.PaymentTransactions.Add(paymentTransaction);

        var customerNotif = new Notification
        {
            UserId = pending.UserId,
            Title = "Thanh toán thành công",
            Message = $"Đơn hàng #{orderCode} đã được thanh toán qua SEPay",
            Type = "Payment",
            RelatedUrl = $"/customer/orders/detail?id={order.Id}",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
        db.Notifications.Add(customerNotif);

        db.PendingCheckouts.Remove(pending);

        await db.SaveChangesAsync();

        var financeService = scope.ServiceProvider.GetRequiredService<FinanceService>();
        await financeService.AddOrderEarnings(order.Id);

        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<NotificationHub>>();
        await hubContext.Clients.Group($"user_{pending.UserId}")
            .SendAsync("ReceiveNotification", "Thanh toán thành công",
                $"Đơn hàng #{orderCode} đã được thanh toán qua SEPay", "");

        return Results.Ok(new { message = "OK" });
    }
    catch (Exception ex)
    {
        return Results.Ok(new { message = $"Error: {ex.Message}" });
    }
}

app.MapPost("/payment/sepay-webhook", HandleSepayWebhook).WithDisplayName("SEPayWebhook");
app.MapPost("/", HandleSepayWebhook).WithDisplayName("SEPayWebhookRoot");

app.MapGet("/api/payment/status/{orderCode}", async (string orderCode, ApplicationDbContext db) =>
{
    var order = await db.Orders.FirstOrDefaultAsync(o => o.OrderCode == orderCode);
    if (order == null) return Results.Ok(new { paid = false });
    return Results.Ok(new { paid = order.PaymentStatus == "Paid" });
});

app.MapGet("/image/product/{id:int}", async (int id, ApplicationDbContext db) =>
{
    var product = await db.Products.FindAsync(id);
    if (product?.ImageData == null) return Results.NotFound();
    return Results.File(product.ImageData, product.ImageContentType ?? "image/jpeg");
});

app.MapGet("/image/supermarket-logo/{id:int}", async (int id, ApplicationDbContext db) =>
{
    var supermarket = await db.Supermarkets.FindAsync(id);
    if (supermarket?.LogoData == null) return Results.NotFound();
    return Results.File(supermarket.LogoData, supermarket.LogoContentType ?? "image/jpeg");
});

app.MapGet("/image/supermarket-cover/{id:int}", async (int id, ApplicationDbContext db) =>
{
    var supermarket = await db.Supermarkets.FindAsync(id);
    if (supermarket?.CoverImageData == null) return Results.NotFound();
    return Results.File(supermarket.CoverImageData, supermarket.CoverImageContentType ?? "image/jpeg");
});

app.MapGet("/debug/images", async (ApplicationDbContext db) =>
{
    var products = await db.Products.Select(p => new { p.Id, p.ImageUrl, HasData = p.ImageData != null, p.ImageContentType }).ToListAsync();
    var supermarkets = await db.Supermarkets.Select(s => new { s.Id, HasLogo = s.LogoData != null, s.LogoContentType, s.LogoUrl, HasCover = s.CoverImageData != null, s.CoverImageContentType, s.CoverImageUrl }).ToListAsync();
    return Results.Json(new { products, supermarkets });
});

app.MapFallbackToPage("/NotFound");

try
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await context.Database.MigrateAsync();
    Log.Information("Database migrated successfully");

    await SeedData.Initialize(app.Services);
    Log.Information("Seed data initialized");

    // Migrate existing file-based images to database
    var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
    var wwwroot = env.WebRootPath;
    var migrated = 0;

    var productsToMigrate = await context.Products.Where(p => p.ImageData == null).ToListAsync();
    var productDir = Path.Combine(wwwroot, "uploads", "products");
    var productFiles = Directory.Exists(productDir) ? Directory.GetFiles(productDir) : Array.Empty<string>();

    var productFileIndex = 0;
    foreach (var p in productsToMigrate)
    {
        // Try matching by the URL in DB first
        var filePath = p.ImageUrl != null && p.ImageUrl.StartsWith("/uploads/")
            ? Path.Combine(wwwroot, p.ImageUrl.TrimStart('/'))
            : null;

        if (filePath != null && File.Exists(filePath))
        {
            p.ImageData = await File.ReadAllBytesAsync(filePath);
            p.ImageContentType = Path.GetExtension(filePath)?.ToLower() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                _ => "image/jpeg"
            };
        }
        else if (productFileIndex < productFiles.Length)
        {
            // Assign next available file on disk
            var fallbackPath = productFiles[productFileIndex++];
            p.ImageData = await File.ReadAllBytesAsync(fallbackPath);
            p.ImageContentType = Path.GetExtension(fallbackPath)?.ToLower() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                _ => "image/jpeg"
            };
        }

        if (p.ImageData != null)
        {
            p.ImageUrl = $"/image/product/{p.Id}";
            migrated++;
        }
    }

    var smDir = Path.Combine(wwwroot, "uploads", "supermarkets");
    var smFiles = Directory.Exists(smDir) ? Directory.GetFiles(smDir) : Array.Empty<string>();
    var smFileIndex = 0;

    var smToMigrate = await context.Supermarkets.Where(s => s.LogoData == null).ToListAsync();

    foreach (var sm in smToMigrate)
    {
        var filePath = sm.LogoUrl != null && sm.LogoUrl.StartsWith("/uploads/")
            ? Path.Combine(wwwroot, sm.LogoUrl.TrimStart('/'))
            : null;

        if (filePath != null && File.Exists(filePath))
        {
            sm.LogoData = await File.ReadAllBytesAsync(filePath);
            sm.LogoContentType = Path.GetExtension(filePath)?.ToLower() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                _ => "image/jpeg"
            };
        }
        else if (smFileIndex < smFiles.Length)
        {
            var fallbackPath = smFiles[smFileIndex++];
            sm.LogoData = await File.ReadAllBytesAsync(fallbackPath);
            sm.LogoContentType = Path.GetExtension(fallbackPath)?.ToLower() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                _ => "image/jpeg"
            };
        }

        if (sm.LogoData != null)
        {
            sm.LogoUrl = $"/image/supermarket-logo/{sm.Id}";
            migrated++;
        }

        // Cover image
        var coverPath = sm.CoverImageUrl != null && sm.CoverImageUrl.StartsWith("/uploads/")
            ? Path.Combine(wwwroot, sm.CoverImageUrl.TrimStart('/'))
            : null;

        if (coverPath != null && File.Exists(coverPath))
        {
            sm.CoverImageData = await File.ReadAllBytesAsync(coverPath);
            sm.CoverImageContentType = Path.GetExtension(coverPath)?.ToLower() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                _ => "image/jpeg"
            };
        }
        else if (smFileIndex < smFiles.Length)
        {
            var fallbackPath = smFiles[smFileIndex++];
            sm.CoverImageData = await File.ReadAllBytesAsync(fallbackPath);
            sm.CoverImageContentType = Path.GetExtension(fallbackPath)?.ToLower() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                _ => "image/jpeg"
            };
        }

        if (sm.CoverImageData != null)
        {
            sm.CoverImageUrl = $"/image/supermarket-cover/{sm.Id}";
            migrated++;
        }
    }

    if (migrated > 0)
    {
        await context.SaveChangesAsync();
        Log.Information("Migrated {Count} existing images to database", migrated);
    }
}
catch (Exception ex)
{
    Log.Fatal(ex, "Failed to initialize database");
    throw;
}

app.Run();
