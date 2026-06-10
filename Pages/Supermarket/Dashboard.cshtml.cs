using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NearGo.Data;
using NearGo.Models;
using NearGo.Services;

namespace NearGo.Pages.Supermarket
{
    [Authorize(Roles = "Supermarket")]
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly FinanceService _financeService;

        public DashboardModel(ApplicationDbContext context, UserManager<AppUser> userManager, FinanceService financeService)
        {
            _context = context;
            _userManager = userManager;
            _financeService = financeService;
        }

        public NearGo.Models.Supermarket? Supermarket { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal Balance { get; set; }
        public int TotalOrders { get; set; }
        public int TotalProducts { get; set; }
        public decimal CommissionPercent { get; set; }
        public List<Order> RecentOrders { get; set; } = new();
        public List<Product> TopProducts { get; set; } = new();
        public List<decimal> DailyRevenue { get; set; } = new();
        public List<string> DailyRevenueLabels { get; set; } = new();

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.SupermarketId == null) return;

            Supermarket = await _context.Supermarkets.FindAsync(user.SupermarketId.Value);
            if (Supermarket == null) return;

            Balance = Supermarket.Balance;
            CommissionPercent = Supermarket.SubscriptionTier == "Premium" ? 5m : 10m;

            var totalGross = await _context.Orders
                .Where(o => o.SupermarketId == Supermarket.Id && o.PaymentStatus == "Paid" && o.Status != "Cancelled")
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            var totalCommission = await _context.PlatformFees
                .Where(f => f.SupermarketId == Supermarket.Id && f.FeeType == "Commission" && f.Status == "Paid")
                .SumAsync(f => (decimal?)f.Amount) ?? 0;

            TotalRevenue = totalGross - totalCommission;

            TotalOrders = await _context.Orders
                .CountAsync(o => o.SupermarketId == Supermarket.Id && o.Status != "Cancelled");

            TotalProducts = await _context.Products
                .CountAsync(p => p.SupermarketId == Supermarket.Id);

            RecentOrders = await _context.Orders
                .Include(o => o.Customer)
                .Where(o => o.SupermarketId == Supermarket.Id)
                .OrderByDescending(o => o.OrderDate)
                .Take(10)
                .ToListAsync();

            TopProducts = await _context.Products
                .Where(p => p.SupermarketId == Supermarket.Id)
                .OrderByDescending(p => p.SoldCount)
                .Take(10)
                .ToListAsync();

            var today = DateTime.UtcNow.Date;
            var vietnamTz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            DailyRevenue = new List<decimal>();
            DailyRevenueLabels = new List<string>();
            var dayNames = new[] { "CN", "T2", "T3", "T4", "T5", "T6", "T7" };
            for (int i = 6; i >= 0; i--)
            {
                var dayStart = today.AddDays(-i);
                var dayEnd = dayStart.AddDays(1);
                var dayGross = await _context.Orders
                    .Where(o => o.SupermarketId == Supermarket.Id
                        && o.PaymentStatus == "Paid"
                        && o.Status != "Cancelled"
                        && o.PaymentDate >= dayStart
                        && o.PaymentDate < dayEnd)
                    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
                DailyRevenue.Add(dayGross);
                var localDay = TimeZoneInfo.ConvertTimeFromUtc(dayStart, vietnamTz);
                DailyRevenueLabels.Add(dayNames[(int)localDay.DayOfWeek]);
            }
        }
    }
}
