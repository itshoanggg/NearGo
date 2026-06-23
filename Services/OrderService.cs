using Microsoft.EntityFrameworkCore;
using NearGo.Data;
using NearGo.Models;

namespace NearGo.Services
{
    public class OrderService
    {
        private readonly ApplicationDbContext _context;

        public OrderService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Order>> GetUserOrders(string userId)
        {
            return await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .Include(o => o.Supermarket)
                .Include(o => o.PaymentTransaction)
                .Where(o => o.CustomerId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<List<Order>> GetSupermarketOrders(int supermarketId)
        {
            return await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .Include(o => o.Customer)
                .Include(o => o.PaymentTransaction)
                .Where(o => o.SupermarketId == supermarketId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<Order?> GetOrderById(int orderId)
        {
            return await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product).ThenInclude(p => p.Category)
                .Include(o => o.Customer)
                .Include(o => o.Supermarket)
                .Include(o => o.PaymentTransaction)
                .Include(o => o.Voucher)
                .Include(o => o.SupermarketRating)
                .FirstOrDefaultAsync(o => o.Id == orderId);
        }

        public async Task<bool> UpdateOrderStatus(int orderId, string status)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return false;

            order.Status = status;
            if (status == "Received") order.DeliveredDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdatePaymentStatus(int orderId, string paymentStatus, string? transactionId = null)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return false;

            order.PaymentStatus = paymentStatus;
            order.PaymentDate = paymentStatus == "Paid" ? DateTime.UtcNow : order.PaymentDate;
            if (transactionId != null) order.TransactionId = transactionId;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<decimal> GetTotalRevenue(int supermarketId)
        {
            return await _context.Orders
                .Where(o => o.SupermarketId == supermarketId && o.PaymentStatus == "Paid" && o.Status != "Cancelled")
                .SumAsync(o => o.TotalAmount);
        }

        public async Task<int> GetTotalOrders(int supermarketId)
        {
            return await _context.Orders
                .Where(o => o.SupermarketId == supermarketId && o.Status != "Cancelled")
                .CountAsync();
        }

        public async Task UpdateOrderTransaction(int orderId, string? sessionId, string paymentMethod)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null)
            {
                order.TransactionId = sessionId;
                order.PaymentMethod = paymentMethod;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<Order>> GetOrdersBySessionId(string sessionId)
        {
            return await _context.Orders
                .Where(o => o.TransactionId == sessionId)
                .ToListAsync();
        }

        public async Task CreatePaymentTransaction(int orderId, string paymentMethod, string? transactionId, string? bankCode, decimal amount, string status, string? responseCode, string? responseMessage)
        {
            var transaction = new PaymentTransaction
            {
                OrderId = orderId,
                PaymentMethod = paymentMethod,
                TransactionId = transactionId,
                BankCode = bankCode,
                Amount = amount,
                Status = status,
                ResponseCode = responseCode,
                ResponseMessage = responseMessage,
                CreatedAt = DateTime.UtcNow,
                PaidAt = status == "Success" ? DateTime.UtcNow : null
            };
            _context.PaymentTransactions.Add(transaction);
            await _context.SaveChangesAsync();
        }
    }
}
