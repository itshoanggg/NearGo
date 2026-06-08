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

            var rng = new Random();
            var data = Enumerable.Range(0, Math.Max(1, DateTime.UtcNow.Month)).Select(_ => rng.Next(1, 10) * 1000000).ToArray();
            RevenueChartData = System.Text.Json.JsonSerializer.Serialize(data);
        }
    }
}
