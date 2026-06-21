using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NearGo.Data;
using NearGo.Hubs;
using NearGo.Models;

namespace NearGo.Services
{
    public class ExpiryDiscountService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ExpiryDiscountService> _logger;

        public ExpiryDiscountService(IServiceScopeFactory scopeFactory, ILogger<ExpiryDiscountService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ExpiryDiscountService started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessExpiringProducts();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing expiring products");
                }

                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        private async Task ProcessExpiringProducts()
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<NotificationHub>>();

            var now = DateTime.UtcNow;
            var threeDaysFromNow = now.AddDays(3);

            var premiumProducts = await context.Products
                .Include(p => p.Supermarket)
                .Where(p => p.Supermarket.SubscriptionTier == "Premium"
                    && p.IsActive
                    && p.StockQuantity > 0
                    && p.ExpiryDate > now
                    && p.ExpiryDate <= threeDaysFromNow
                    && !p.AutoDiscountApplied)
                .ToListAsync();

            if (!premiumProducts.Any())
            {
                _logger.LogInformation("No premium products nearing expiry found");
                return;
            }

            foreach (var product in premiumProducts)
            {
                var extraDiscount = 0.2;
                var newDiscountedPrice = product.DiscountedPrice * (decimal)(1 - extraDiscount);
                if (newDiscountedPrice < 1000) newDiscountedPrice = 1000;

                product.DiscountedPrice = Math.Round(newDiscountedPrice / 1000) * 1000;
                product.DiscountPercentage = product.OriginalPrice > 0
                    ? Math.Round((double)((product.OriginalPrice - product.DiscountedPrice) / product.OriginalPrice * 100), 1)
                    : 0;
                product.AutoDiscountApplied = true;

                var followerIds = await context.Database
                    .SqlQuery<string>($"SELECT UserId FROM UserFollowedSupermarkets WHERE SupermarketId = {product.SupermarketId}")
                    .ToListAsync();

                if (followerIds.Count > 0)
                {
                    var title = $"🔥 Giảm giá sốc - {product.Supermarket.Name}";
                    var message = $"{product.Name} sắp hết hạn - giảm còn {product.DiscountedPrice:N0}đ";
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
                }
            }

            await context.SaveChangesAsync();
            _logger.LogInformation("Applied auto-discount to {Count} premium products", premiumProducts.Count);
        }
    }
}
