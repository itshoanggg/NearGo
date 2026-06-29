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
    public class UpgradeModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public UpgradeModel(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public NearGo.Models.Supermarket? Supermarket { get; set; }
        public decimal CommissionPercent { get; set; } = 10m;
        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalCommission { get; set; }

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.SupermarketId == null) return;

            Supermarket = await _context.Supermarkets.FindAsync(user.SupermarketId.Value);
            if (Supermarket == null) return;

            TotalProducts = await _context.Products
                .CountAsync(p => p.SupermarketId == Supermarket.Id);

            TotalOrders = await _context.Orders
                .CountAsync(o => o.SupermarketId == Supermarket.Id && o.Status != "Cancelled");

            TotalRevenue = await _context.Orders
                .Where(o => o.SupermarketId == Supermarket.Id && o.PaymentStatus == "Paid" && o.Status != "Cancelled")
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            TotalCommission = await _context.PlatformFees
                .Where(f => f.SupermarketId == Supermarket.Id && f.FeeType == "Commission" && f.Status == "Paid")
                .SumAsync(f => (decimal?)f.Amount) ?? 0;
        }
    }
}
