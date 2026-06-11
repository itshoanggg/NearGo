using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NearGo.Data;
using NearGo.Models;

namespace NearGo.Pages.Supermarket
{
    [Authorize(Roles = "Supermarket")]
    public class RevenueModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public RevenueModel(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public decimal TotalRevenue { get; set; }
        public decimal GrossRevenue { get; set; }
        public int PaidOrders { get; set; }
        public decimal TotalCommission { get; set; }
        public string RevenueChartData { get; set; } = "[]";
        public string RevenueChartLabels { get; set; } = "[]";

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.SupermarketId == null) return;

            var smId = user.SupermarketId.Value;
            GrossRevenue = await _context.Orders
                .Where(o => o.SupermarketId == smId && o.PaymentStatus == "Paid" && o.Status != "Cancelled")
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            TotalCommission = await _context.PlatformFees
                .Where(f => f.SupermarketId == smId && f.FeeType == "Commission" && f.Status == "Paid")
                .SumAsync(f => (decimal?)f.Amount) ?? 0;

            TotalRevenue = GrossRevenue - TotalCommission;

            PaidOrders = await _context.Orders
                .CountAsync(o => o.SupermarketId == smId && o.PaymentStatus == "Paid");

            var now = DateTime.UtcNow;
            var monthlyRevenue = await _context.Orders
                .Where(o => o.SupermarketId == smId && o.PaymentStatus == "Paid" && o.Status != "Cancelled"
                    && o.OrderDate.Year == now.Year)
                .GroupBy(o => o.OrderDate.Month)
                .Select(g => new { Month = g.Key, Total = g.Sum(o => o.TotalAmount) })
                .ToListAsync();

            var monthNames = new[] { "T1", "T2", "T3", "T4", "T5", "T6", "T7", "T8", "T9", "T10", "T11", "T12" };
            var labels = new List<string>();
            var data = new List<decimal>();

            for (int m = 1; m <= now.Month; m++)
            {
                labels.Add(monthNames[m - 1]);
                data.Add(monthlyRevenue.FirstOrDefault(x => x.Month == m)?.Total ?? 0);
            }

            RevenueChartLabels = System.Text.Json.JsonSerializer.Serialize(labels);
            RevenueChartData = System.Text.Json.JsonSerializer.Serialize(data);
        }
    }
}
