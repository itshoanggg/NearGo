using Microsoft.AspNetCore.Identity;

namespace NearGo.Models
{
    public class AppUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public string? Avatar { get; set; }
        public string? Address { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        public int? SupermarketId { get; set; }
        public Supermarket? Supermarket { get; set; }
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public ICollection<LoyaltyPoint> LoyaltyPoints { get; set; } = new List<LoyaltyPoint>();
        public ICollection<RecentlyViewed> RecentlyViewed { get; set; } = new List<RecentlyViewed>();
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public ICollection<Supermarket> FollowedSupermarkets { get; set; } = new List<Supermarket>();
        public ICollection<SupermarketRating> SupermarketRatings { get; set; } = new List<SupermarketRating>();
        public ICollection<Report> Reports { get; set; } = new List<Report>();
    }
}
