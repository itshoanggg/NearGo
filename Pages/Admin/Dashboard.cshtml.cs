using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NearGo.Data;
using NearGo.Models;
using NearGo.Services;

namespace NearGo.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly FinanceService _financeService;
        private readonly UserManager<AppUser> _userManager;

        public DashboardModel(ApplicationDbContext context, FinanceService financeService, UserManager<AppUser> userManager)
        {
            _context = context;
            _financeService = financeService;
            _userManager = userManager;
        }

        public int TotalUsers { get; set; }
        public int TotalSupermarkets { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalPlatformBalance { get; set; }
        public decimal TotalSupermarketBalance { get; set; }
        public int PendingWithdrawals { get; set; }
        public List<NearGo.Models.Supermarket> TopSupermarkets { get; set; } = new();
        public Dictionary<int, int> SupermarketOrderCounts { get; set; } = new();
        public Dictionary<string, decimal> RevenueBySupermarket { get; set; } = new();
        public List<NearGo.Models.AppUser> RecentUsers { get; set; } = new();

        public async Task OnGetAsync()
        {
            var customers = await _userManager.GetUsersInRoleAsync("Customer");
            TotalUsers = customers.Count;
            TotalSupermarkets = await _context.Supermarkets.CountAsync();
            TotalOrders = await _context.Orders.CountAsync();
            TotalRevenue = await _financeService.GetTotalPlatformRevenue();
            TotalPlatformBalance = TotalRevenue;
            TotalSupermarketBalance = await _context.Supermarkets.SumAsync(s => (decimal?)s.Balance) ?? 0;
            PendingWithdrawals = await _context.WithdrawalRequests.CountAsync(w => w.Status == "Pending");

            var topSupermarketData = await _context.Orders
                .Where(o => o.Status != "Cancelled")
                .GroupBy(o => o.SupermarketId)
                .Select(g => new { SupermarketId = g.Key, OrderCount = g.Count() })
                .OrderByDescending(x => x.OrderCount)
                .Take(5)
                .ToListAsync();

            var topIds = topSupermarketData.Select(x => x.SupermarketId).ToList();
            TopSupermarkets = await _context.Supermarkets
                .Where(s => topIds.Contains(s.Id))
                .ToListAsync();

            SupermarketOrderCounts = topSupermarketData.ToDictionary(x => x.SupermarketId, x => x.OrderCount);
            TopSupermarkets = TopSupermarkets.OrderByDescending(s => SupermarketOrderCounts.GetValueOrDefault(s.Id, 0)).ToList();

            RevenueBySupermarket = await _financeService.GetRevenueBySupermarket();

            RecentUsers = customers
                .OrderByDescending(u => u.CreatedAt)
                .Take(5)
                .ToList();
        }
    }
}
