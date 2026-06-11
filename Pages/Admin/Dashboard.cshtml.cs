using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NearGo.Data;
using NearGo.Services;

namespace NearGo.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly FinanceService _financeService;

        public DashboardModel(ApplicationDbContext context, FinanceService financeService)
        {
            _context = context;
            _financeService = financeService;
        }

        public int TotalUsers { get; set; }
        public int TotalSupermarkets { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalPlatformBalance { get; set; }
        public decimal TotalSupermarketBalance { get; set; }
        public int PendingWithdrawals { get; set; }
        public List<NearGo.Models.Supermarket> TopSupermarkets { get; set; } = new();
        public Dictionary<string, decimal> RevenueBySupermarket { get; set; } = new();
        public string RevenueChartLabels { get; set; } = "[]";
        public string RevenueChartData { get; set; } = "[]";
        public string UsersChartLabels { get; set; } = "[]";
        public string UsersChartData { get; set; } = "[]";

        public async Task OnGetAsync()
        {
            TotalUsers = await _context.Users.CountAsync();
            TotalSupermarkets = await _context.Supermarkets.CountAsync();
            TotalOrders = await _context.Orders.CountAsync();
            TotalRevenue = await _financeService.GetTotalPlatformRevenue();
            TotalPlatformBalance = TotalRevenue;
            TotalSupermarketBalance = await _context.Supermarkets.SumAsync(s => (decimal?)s.Balance) ?? 0;
            PendingWithdrawals = await _context.WithdrawalRequests.CountAsync(w => w.Status == "Pending");

            TopSupermarkets = await _context.Supermarkets
                .OrderByDescending(s => s.TotalOrders)
                .Take(5)
                .ToListAsync();

            RevenueBySupermarket = await _financeService.GetRevenueBySupermarket();

            var now = DateTime.UtcNow;
            var monthNames = new[] { "T1", "T2", "T3", "T4", "T5", "T6", "T7", "T8", "T9", "T10", "T11", "T12" };

            var monthlyRevenue = await _context.PlatformFees
                .Where(f => f.FeeType == "Commission" && f.Status == "Paid" && f.CreatedAt.Year == now.Year)
                .GroupBy(f => f.CreatedAt.Month)
                .Select(g => new { Month = g.Key, Total = g.Sum(f => f.Amount) })
                .ToListAsync();

            var revenueLabels = new List<string>();
            var revenueData = new List<decimal>();
            for (int m = 1; m <= now.Month; m++)
            {
                revenueLabels.Add(monthNames[m - 1]);
                revenueData.Add(monthlyRevenue.FirstOrDefault(x => x.Month == m)?.Total ?? 0);
            }
            RevenueChartLabels = System.Text.Json.JsonSerializer.Serialize(revenueLabels);
            RevenueChartData = System.Text.Json.JsonSerializer.Serialize(revenueData);

            var monthlyUsers = await _context.Users
                .Where(u => u.CreatedAt.Year == now.Year)
                .GroupBy(u => u.CreatedAt.Month)
                .Select(g => new { Month = g.Key, Count = g.Count() })
                .ToListAsync();

            var userLabels = new List<string>();
            var userData = new List<int>();
            for (int m = 1; m <= now.Month; m++)
            {
                userLabels.Add(monthNames[m - 1]);
                userData.Add(monthlyUsers.FirstOrDefault(x => x.Month == m)?.Count ?? 0);
            }
            UsersChartLabels = System.Text.Json.JsonSerializer.Serialize(userLabels);
            UsersChartData = System.Text.Json.JsonSerializer.Serialize(userData);
        }
    }
}
