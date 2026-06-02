namespace NearGo.Models
{
    public class SupermarketBankInfo
    {
        public int Id { get; set; }
        public int SupermarketId { get; set; }
        public string BankName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountHolder { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public Supermarket Supermarket { get; set; } = null!;
    }
}
