namespace NearGo.Models
{
    public class PendingCheckout
    {
        public int Id { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public int SupermarketId { get; set; }
        public string ShippingAddress { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string? Note { get; set; }
        public int? VoucherId { get; set; }
        public bool UsePoints { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
