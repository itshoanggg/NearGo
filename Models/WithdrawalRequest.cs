namespace NearGo.Models
{
    public class WithdrawalRequest
    {
        public int Id { get; set; }
        public int SupermarketId { get; set; }
        public decimal Amount { get; set; }
        public decimal CommissionPercent { get; set; }
        public decimal CommissionAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public string Status { get; set; } = "Pending";
        public string? BankInfoJson { get; set; }
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }
        public string? ApprovedById { get; set; }
        public string? AdminNote { get; set; }
        public bool IsAutoPayout { get; set; }
        public int? PeriodMonth { get; set; }
        public int? PeriodYear { get; set; }
        public Supermarket Supermarket { get; set; } = null!;
        public AppUser? ApprovedBy { get; set; }
    }
}
