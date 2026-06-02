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
        }
    }
}
