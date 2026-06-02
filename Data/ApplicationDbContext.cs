using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NearGo.Models;

namespace NearGo.Data
{
    public class ApplicationDbContext : IdentityDbContext<AppUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Product> Products => Set<Product>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Supermarket> Supermarkets => Set<Supermarket>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<CartItem> CartItems => Set<CartItem>();
        public DbSet<Voucher> Vouchers => Set<Voucher>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<Review> Reviews => Set<Review>();
        public DbSet<Wishlist> Wishlists => Set<Wishlist>();
        public DbSet<LoyaltyPoint> LoyaltyPoints => Set<LoyaltyPoint>();
        public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
        public DbSet<RecentlyViewed> RecentlyVieweds => Set<RecentlyViewed>();
        public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
        public DbSet<Subscription> Subscriptions => Set<Subscription>();
        public DbSet<PlatformFee> PlatformFees => Set<PlatformFee>();
        public DbSet<PendingCheckout> PendingCheckouts => Set<PendingCheckout>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(decimal) || property.ClrType == typeof(decimal?))
                    {
                        property.SetPrecision(18);
                        property.SetScale(2);
                    }
                }
            }

            builder.Entity<Product>(e =>
            {
                e.HasOne(p => p.Category).WithMany(c => c.Products).HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(p => p.Supermarket).WithMany(s => s.Products).HasForeignKey(p => p.SupermarketId).OnDelete(DeleteBehavior.Restrict);
                e.HasIndex(p => p.Slug);
                e.HasIndex(p => p.IsActive);
                e.HasIndex(p => p.ExpiryDate);
            });

            builder.Entity<Order>(e =>
            {
                e.HasOne(o => o.Customer).WithMany(u => u.Orders).HasForeignKey(o => o.CustomerId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(o => o.Supermarket).WithMany(s => s.Orders).HasForeignKey(o => o.SupermarketId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(o => o.Voucher).WithMany().HasForeignKey(o => o.VoucherId).OnDelete(DeleteBehavior.SetNull);
                e.HasIndex(o => o.OrderCode).IsUnique();
                e.HasIndex(o => o.Status);
            });

            builder.Entity<OrderItem>(e =>
            {
                e.HasOne(oi => oi.Order).WithMany(o => o.OrderItems).HasForeignKey(oi => oi.OrderId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(oi => oi.Product).WithMany(p => p.OrderItems).HasForeignKey(oi => oi.ProductId).OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<CartItem>(e =>
            {
                e.HasOne(c => c.User).WithMany(u => u.CartItems).HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(c => c.Product).WithMany(p => p.CartItems).HasForeignKey(c => c.ProductId).OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Review>(e =>
            {
                e.HasOne(r => r.User).WithMany(u => u.Reviews).HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(r => r.Product).WithMany(p => p.Reviews).HasForeignKey(r => r.ProductId).OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Wishlist>(e =>
            {
                e.HasOne(w => w.User).WithMany(u => u.Wishlists).HasForeignKey(w => w.UserId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(w => w.Product).WithMany(p => p.Wishlists).HasForeignKey(w => w.ProductId).OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(w => new { w.UserId, w.ProductId }).IsUnique();
            });

            builder.Entity<Notification>(e =>
            {
                e.HasOne(n => n.User).WithMany(u => u.Notifications).HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(n => n.IsRead);
            });

            builder.Entity<LoyaltyPoint>(e =>
            {
                e.HasOne(lp => lp.User).WithMany(u => u.LoyaltyPoints).HasForeignKey(lp => lp.UserId).OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<RecentlyViewed>(e =>
            {
                e.HasOne(rv => rv.User).WithMany(u => u.RecentlyViewed).HasForeignKey(rv => rv.UserId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(rv => rv.Product).WithMany(p => p.RecentlyViewed).HasForeignKey(rv => rv.ProductId).OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<PaymentTransaction>(e =>
            {
                e.HasOne(pt => pt.Order).WithOne(o => o.PaymentTransaction).HasForeignKey<PaymentTransaction>(pt => pt.OrderId).OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Subscription>(e =>
            {
                e.HasOne(s => s.Supermarket).WithMany(sm => sm.Subscriptions).HasForeignKey(s => s.SupermarketId).OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<PlatformFee>(e =>
            {
                e.HasOne(pf => pf.Supermarket).WithMany(s => s.PlatformFees).HasForeignKey(pf => pf.SupermarketId).OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<PendingCheckout>(e =>
            {
                e.HasIndex(p => p.OrderCode).IsUnique();
            });

            builder.Entity<Voucher>(e =>
            {
                e.HasOne(v => v.Supermarket).WithMany().HasForeignKey(v => v.SupermarketId).OnDelete(DeleteBehavior.SetNull);
                e.HasIndex(v => v.Code).IsUnique();
            });

            builder.Entity<ChatMessage>(e =>
            {
                e.HasOne(cm => cm.User).WithMany().HasForeignKey(cm => cm.UserId).OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<AppUser>(e =>
            {
                e.HasOne(u => u.Supermarket)
                    .WithMany()
                    .HasForeignKey(u => u.SupermarketId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<AppUser>()
                .HasMany(u => u.FollowedSupermarkets)
                .WithMany(s => s.Followers)
                .UsingEntity<Dictionary<string, object>>(
                    "UserFollowedSupermarkets",
                    j => j.HasOne<Supermarket>().WithMany().HasForeignKey("SupermarketId").OnDelete(DeleteBehavior.Cascade),
                    j => j.HasOne<AppUser>().WithMany().HasForeignKey("UserId").OnDelete(DeleteBehavior.Cascade));
        }
    }
}
