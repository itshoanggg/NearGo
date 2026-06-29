using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NearGo.Data;
using NearGo.Hubs;
using NearGo.Models;

namespace NearGo.Services
{
    public class DealNotificationService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DealNotificationService> _logger;

        public DealNotificationService(IServiceScopeFactory scopeFactory, ILogger<DealNotificationService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DealNotificationService started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessDeals();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing deals");
                }

                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            }
        }

        private async Task ProcessDeals()
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<NotificationHub>>();

            var now = DateTime.UtcNow;
            var hasChanges = false;

            // 1. Auto-move: products with DiscountEndDate but ExpiryDate <= 30 days → giảm sâu
            var toMove = await context.Products
                .Where(p => p.IsActive && p.StockQuantity > 0
                    && p.DiscountEndDate != null
                    && p.ExpiryDate > now
                    && p.ExpiryDate <= now.AddDays(30))
                .ToListAsync();

            foreach (var product in toMove)
            {
                product.DiscountEndDate = null;
                hasChanges = true;
            }

            if (toMove.Count > 0)
                _logger.LogInformation("Moved {Count} products to giảm sâu", toMove.Count);

            // 2. Auto-discount: giảm sâu products (no DiscountEndDate, ExpiryDate <= 30 days)
            var giamSau = await context.Products
                .Where(p => p.IsActive && p.StockQuantity > 0
                    && p.DiscountEndDate == null
                    && p.ExpiryDate > now
                    && p.ExpiryDate <= now.AddDays(30))
                .ToListAsync();

            foreach (var product in giamSau)
            {
                var daysLeft = Math.Max(1, (int)(product.ExpiryDate - now).TotalDays);
                double rate;
                if (daysLeft <= 7) rate = 0.50;
                else if (daysLeft <= 14) rate = 0.30;
                else rate = 0.20;

                product.DiscountedPrice = Math.Round(product.OriginalPrice * (decimal)(1 - rate) / 100) * 100;
                product.DiscountPercentage = Math.Round(rate * 100, 1);
                product.DealScore = Math.Round(rate * 10000, 1);
                hasChanges = true;
            }

            if (giamSau.Count > 0)
                _logger.LogInformation("Auto-discounted {Count} giảm sâu products", giamSau.Count);

            // 3. Notify about deals ending soon (DiscountEndDate within 24h)
            var endingDeals = await context.Products
                .Include(p => p.Supermarket)
                .Where(p => p.IsActive && p.StockQuantity > 0
                    && p.DiscountEndDate > now
                    && p.DiscountEndDate <= now.AddHours(24))
                .ToListAsync();

            foreach (var product in endingDeals)
            {
                var followerIds = await context.Database
                    .SqlQuery<string>($"SELECT UserId FROM UserFollowedSupermarkets WHERE SupermarketId = {product.SupermarketId}")
                    .ToListAsync();

                if (followerIds.Count == 0) continue;

                var hoursLeft = (int)(product.DiscountEndDate!.Value - now).TotalHours;
                var title = $"Giảm giá sắp kết thúc - {product.Supermarket.Name}";
                var message = $"{product.Name} giảm {product.DiscountPercentage}% - chỉ còn {hoursLeft} giờ!";
                var url = $"/products/detail?id={product.Id}";

                foreach (var fid in followerIds)
                {
                    context.Notifications.Add(new Notification
                    {
                        UserId = fid,
                        Title = title,
                        Message = message,
                        Type = "Discount",
                        RelatedUrl = url,
                        ImageUrl = product.ImageUrl,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                await context.SaveChangesAsync();

                foreach (var fid in followerIds)
                {
                    try
                    {
                        await hubContext.Clients.Group($"user_{fid}")
                            .SendAsync("ReceiveNotification", title, message, url);
                    }
                    catch { }
                }

                hasChanges = true;
            }

            // 4. Expire discounts: DiscountEndDate has passed → reset DealScore
            var expiredDeals = await context.Products
                .Where(p => p.IsActive && p.DiscountEndDate < now)
                .ToListAsync();

            foreach (var product in expiredDeals)
            {
                product.DealScore = 0;
                hasChanges = true;
            }

            if (expiredDeals.Count > 0)
                _logger.LogInformation("Expired {Count} deals", expiredDeals.Count);

            // 5. Fully expired: ExpiryDate has passed → hide product
            var fullyExpired = await context.Products
                .Where(p => p.IsActive && p.ExpiryDate < now)
                .ToListAsync();

            foreach (var product in fullyExpired)
            {
                product.IsActive = false;
                hasChanges = true;
            }

            if (fullyExpired.Count > 0)
                _logger.LogInformation("Auto-hidden {Count} expired products", fullyExpired.Count);

            if (hasChanges)
            {
                await context.SaveChangesAsync();
            }
        }
    }
}
