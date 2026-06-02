namespace NearGo.Models
{
    public class Report
    {
        public int Id { get; set; }
        public string ReporterId { get; set; } = string.Empty;
        public int? SupermarketId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public string? AdminResponse { get; set; }
        public string? AdminId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }
        public bool RequestedExplanation { get; set; }
        public string? SupermarketExplanation { get; set; }
        public DateTime? ExplanationRequestedAt { get; set; }
        public DateTime? ExplanationProvidedAt { get; set; }
        public AppUser Reporter { get; set; } = null!;
        public Supermarket? Supermarket { get; set; }
        public AppUser? Admin { get; set; }
    }
}
