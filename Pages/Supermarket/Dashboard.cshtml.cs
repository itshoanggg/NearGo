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
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public DashboardModel(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public NearGo.Models.Supermarket? Supermarket { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal Balance { get; set; }
        public int TotalOrders { get; set; }
        public int TotalProducts { get; set; }
        public int ProductsThisMonth { get; set; }
        public List<Order> RecentOrders { get; set; } = new();
        public List<Product> TopProducts { get; set; } = new();

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.SupermarketId == null) return;

            Supermarket = await _context.Supermarkets.FindAsync(user.SupermarketId.Value);
            if (Supermarket == null) return;

            Balance = Supermarket.Balance;

            TotalRevenue = await _context.Orders
                .Where(o => o.SupermarketId == Supermarket.Id && o.PaymentStatus == "Paid" && o.Status != "Cancelled")
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            TotalOrders = await _context.Orders
                .CountAsync(o => o.SupermarketId == Supermarket.Id && o.Status != "Cancelled");

            TotalProducts = await _context.Products
                .CountAsync(p => p.SupermarketId == Supermarket.Id);

            var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            ProductsThisMonth = await _context.Products
                .CountAsync(p => p.SupermarketId == Supermarket.Id && p.CreatedAt >= startOfMonth);

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
        }
    }
}
