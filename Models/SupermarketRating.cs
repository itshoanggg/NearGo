namespace NearGo.Models
{
    public class SupermarketRating
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int SupermarketId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Order Order { get; set; } = null!;
        public AppUser User { get; set; } = null!;
        public Supermarket Supermarket { get; set; } = null!;
    }
}
