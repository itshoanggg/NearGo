using Microsoft.EntityFrameworkCore;
using NearGo.Data;
using NearGo.Models;
using System.Text.Json;

namespace NearGo.Services
{
    public class FinanceService
    {
        private readonly ApplicationDbContext _context;

        public FinanceService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<decimal> GetCommissionPercentForSupermarket(int supermarketId)
        {
            var supermarket = await _context.Supermarkets
                .Where(s => s.Id == supermarketId)
                .Select(s => s.SubscriptionTier)
                .FirstOrDefaultAsync();
            return supermarket == "Premium" ? 5m : 10m;
        }

        public async Task AddOrderEarnings(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.Supermarket)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null || order.PaymentMethod != "SEPay" || order.PaymentStatus != "Paid")
                return;

            var commissionPercent = order.Supermarket.SubscriptionTier == "Premium" ? 5m : 10m;
            var commissionAmount = order.TotalAmount * commissionPercent / 100;
            var supermarketEarned = order.TotalAmount - commissionAmount;

            order.Supermarket.Balance += supermarketEarned;

            _context.PlatformFees.Add(new PlatformFee
            {
                SupermarketId = order.SupermarketId,
                FeeType = "Commission",
                Amount = commissionAmount,
                Description = $"Phí hoa hồng {commissionPercent}% đơn hàng #{order.OrderCode}",
                Status = "Paid",
                CreatedAt = DateTime.UtcNow,
                PaidAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }

        public async Task<WithdrawalRequest> CreateWithdrawalRequest(int supermarketId, decimal amount)
        {
            var supermarket = await _context.Supermarkets
                .Include(s => s.BankInfo)
                .FirstOrDefaultAsync(s => s.Id == supermarketId);

            if (supermarket == null)
                throw new InvalidOperationException("Supermarket not found");

            if (supermarket.Balance < amount)
                throw new InvalidOperationException("Insufficient balance");

            if (supermarket.BankInfo == null)
                throw new InvalidOperationException("Please set up bank info first");

            var bankInfoJson = JsonSerializer.Serialize(new
            {
                supermarket.BankInfo.BankName,
                supermarket.BankInfo.AccountNumber,
                supermarket.BankInfo.AccountHolder
            });

            var request = new WithdrawalRequest
            {
                SupermarketId = supermarketId,
                Amount = amount,
                CommissionPercent = 0,
                CommissionAmount = 0,
                FinalAmount = amount,
                Status = "Pending",
                BankInfoJson = bankInfoJson,
                RequestedAt = DateTime.UtcNow,
                IsAutoPayout = false
            };

            _context.WithdrawalRequests.Add(request);
            await _context.SaveChangesAsync();
            return request;
        }

        public async Task<bool> ApproveWithdrawalRequest(int requestId, string adminId)
        {
            var request = await _context.WithdrawalRequests
                .Include(w => w.Supermarket)
                .FirstOrDefaultAsync(w => w.Id == requestId);

            if (request == null || request.Status != "Pending")
                return false;

            var otherApprovedAmount = await _context.WithdrawalRequests
                .Where(w => w.SupermarketId == request.SupermarketId
                    && w.Status == "Approved"
                    && w.Id != requestId)
                .SumAsync(w => (decimal?)w.Amount) ?? 0;

            var availableBalance = request.Supermarket.Balance - otherApprovedAmount;
            if (availableBalance < request.Amount)
                return false;

            request.Status = "Approved";
            request.ProcessedAt = DateTime.UtcNow;
            request.ApprovedById = adminId;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RejectWithdrawalRequest(int requestId, string adminId, string? reason = null)
        {
            var request = await _context.WithdrawalRequests.FindAsync(requestId);
            if (request == null || request.Status != "Pending")
                return false;

            request.Status = "Rejected";
            request.ProcessedAt = DateTime.UtcNow;
            request.ApprovedById = adminId;
            request.AdminNote = reason;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkAsPaid(int requestId, string adminId)
        {
            var request = await _context.WithdrawalRequests
                .Include(w => w.Supermarket)
                .FirstOrDefaultAsync(w => w.Id == requestId);

            if (request == null || request.Status != "Approved")
                return false;

            if (request.Supermarket.Balance < request.Amount)
                throw new InvalidOperationException($"Số dư siêu thị không đủ ({request.Supermarket.Balance:N0}đ < {request.Amount:N0}đ)");

            request.Supermarket.Balance -= request.Amount;
            request.Status = "Paid";
            request.ProcessedAt = DateTime.UtcNow;
            request.ApprovedById = adminId;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task ProcessAutoPayouts()
        {
            var now = DateTime.UtcNow;
            var lastWeek = now.AddDays(-7);
            var weekNumber = System.Globalization.CultureInfo.InvariantCulture.Calendar
                .GetWeekOfYear(lastWeek, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            var year = lastWeek.Year;

            var supermarkets = await _context.Supermarkets
                .Include(s => s.BankInfo)
                .Where(s => s.Balance > 0 && s.BankInfo != null)
                .ToListAsync();

            foreach (var supermarket in supermarkets)
            {
                var bankInfoJson = JsonSerializer.Serialize(new
                {
                    supermarket.BankInfo!.BankName,
                    supermarket.BankInfo.AccountNumber,
                    supermarket.BankInfo.AccountHolder
                });

                var request = new WithdrawalRequest
                {
                    SupermarketId = supermarket.Id,
                    Amount = supermarket.Balance,
                    CommissionPercent = 0,
                    CommissionAmount = 0,
                    FinalAmount = supermarket.Balance,
                    Status = "Pending",
                    BankInfoJson = bankInfoJson,
                    RequestedAt = now,
                    IsAutoPayout = true,
                    PeriodMonth = weekNumber,
                    PeriodYear = year
                };

                _context.WithdrawalRequests.Add(request);
            }

            await _context.SaveChangesAsync();
        }

        public async Task<decimal> GetBalance(int supermarketId)
        {
            var supermarket = await _context.Supermarkets.FindAsync(supermarketId);
            return supermarket?.Balance ?? 0;
        }

        public async Task<List<WithdrawalRequest>> GetPendingWithdrawals()
        {
            return await _context.WithdrawalRequests
                .Include(w => w.Supermarket)
                .Where(w => w.Status == "Pending")
                .OrderByDescending(w => w.RequestedAt)
                .ToListAsync();
        }

        public async Task<List<WithdrawalRequest>> GetApprovedWithdrawals()
        {
            return await _context.WithdrawalRequests
                .Include(w => w.Supermarket)
                .Where(w => w.Status == "Approved")
                .OrderByDescending(w => w.RequestedAt)
                .ToListAsync();
        }

        public async Task<List<WithdrawalRequest>> GetPaidWithdrawals()
        {
            return await _context.WithdrawalRequests
                .Include(w => w.Supermarket)
                .Where(w => w.Status == "Paid")
                .OrderByDescending(w => w.RequestedAt)
                .ToListAsync();
        }

        public async Task<List<WithdrawalRequest>> GetAllWithdrawals()
        {
            return await _context.WithdrawalRequests
                .Include(w => w.Supermarket)
                .OrderByDescending(w => w.RequestedAt)
                .ToListAsync();
        }

        public async Task<List<WithdrawalRequest>> GetSupermarketWithdrawals(int supermarketId)
        {
            return await _context.WithdrawalRequests
                .Where(w => w.SupermarketId == supermarketId)
                .OrderByDescending(w => w.RequestedAt)
                .ToListAsync();
        }

        public async Task<WithdrawalRequest?> GetWithdrawalById(int id)
        {
            return await _context.WithdrawalRequests
                .Include(w => w.Supermarket)
                .Include(w => w.ApprovedBy)
                .FirstOrDefaultAsync(w => w.Id == id);
        }

        public async Task<decimal> GetTotalPlatformRevenue()
        {
            return await _context.PlatformFees
                .Where(f => f.Status == "Paid")
                .SumAsync(f => (decimal?)f.Amount) ?? 0;
        }

        public async Task<decimal> GetSupermarketBalance(int supermarketId)
        {
            return await _context.Supermarkets
                .Where(s => s.Id == supermarketId)
                .Select(s => s.Balance)
                .FirstOrDefaultAsync();
        }

        public async Task<Dictionary<string, decimal>> GetRevenueBySupermarket()
        {
            return await _context.Supermarkets
                .Where(s => s.Balance > 0)
                .ToDictionaryAsync(s => s.Name, s => s.Balance);
        }
    }
}
