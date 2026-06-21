namespace NearGo.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal DiscountedPrice { get; set; }
        public double DiscountPercentage { get; set; }
        public int StockQuantity { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string? ImageUrl { get; set; }
        public byte[]? ImageData { get; set; }
        public string? ImageContentType { get; set; }
        public string? ImageUrls { get; set; }
        public string? Unit { get; set; } = "cái";
        public double Weight { get; set; }
        public string? Origin { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsBoosted { get; set; }
        public DateTime? BoostExpiry { get; set; }
        public int ViewCount { get; set; }
        public int SoldCount { get; set; }
        public double SmartExpiryScore { get; set; }
        public bool AutoDiscountApplied { get; set; }
        public string? Tags { get; set; }
        public int CategoryId { get; set; }
        public int SupermarketId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Category Category { get; set; } = null!;
        public Supermarket Supermarket { get; set; } = null!;
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
        public ICollection<RecentlyViewed> RecentlyViewed { get; set; } = new List<RecentlyViewed>();
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    }
}
