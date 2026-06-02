namespace NearGo.Models
{
    public class Supermarket
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? LogoUrl { get; set; }
        public string? CoverImageUrl { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? TaxCode { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsVerified { get; set; }
        public string SubscriptionTier { get; set; } = "Free";
        public DateTime? SubscriptionExpiry { get; set; }
        public double Rating { get; set; }
        public int ReviewCount { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<Product> Products { get; set; } = new List<Product>();
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
        public ICollection<PlatformFee> PlatformFees { get; set; } = new List<PlatformFee>();
        public ICollection<AppUser> Followers { get; set; } = new List<AppUser>();
        public ICollection<SupermarketRating> SupermarketRatings { get; set; } = new List<SupermarketRating>();
        public ICollection<Report> Reports { get; set; } = new List<Report>();
    }
}
