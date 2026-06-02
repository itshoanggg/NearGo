namespace NearGo.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public int SupermarketId { get; set; }
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal TotalAmount { get; set; }
        public int? VoucherId { get; set; }
        public int LoyaltyPointsUsed { get; set; }
        public decimal LoyaltyDiscount { get; set; }
        public string Status { get; set; } = "Pending";
        public string PaymentStatus { get; set; } = "Unpaid";
        public string? PaymentMethod { get; set; }
        public string? TransactionId { get; set; }
        public string? ShippingAddress { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerNote { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public DateTime? PaymentDate { get; set; }
        public DateTime? DeliveredDate { get; set; }
        public AppUser Customer { get; set; } = null!;
        public Supermarket Supermarket { get; set; } = null!;
        public Voucher? Voucher { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public PaymentTransaction? PaymentTransaction { get; set; }
        public SupermarketRating? SupermarketRating { get; set; }
    }
}
