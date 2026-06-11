using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NearGo.Data;
using NearGo.Models;

namespace NearGo.Pages.Admin.Supermarkets
{
    [Authorize(Roles = "Admin")]
    public class DetailModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public NearGo.Models.Supermarket Supermarket { get; set; } = null!;
        public decimal TotalCommissionPaid { get; set; }
        public int TotalPaidOrders { get; set; }
        public List<Order> RecentOrders { get; set; } = new();
        public string RevenueChartLabels { get; set; } = "[]";
        public string RevenueChartData { get; set; } = "[]";

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Supermarket = await _context.Supermarkets
                .Include(s => s.Products)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (Supermarket == null) return NotFound();

            TotalCommissionPaid = await _context.PlatformFees
                .Where(f => f.SupermarketId == id && f.FeeType == "Commission" && f.Status == "Paid")
                .SumAsync(f => (decimal?)f.Amount) ?? 0;

            TotalPaidOrders = await _context.Orders
                .CountAsync(o => o.SupermarketId == id && o.PaymentStatus == "Paid" && o.Status != "Cancelled");

            RecentOrders = await _context.Orders
                .Include(o => o.Customer)
                .Where(o => o.SupermarketId == id)
                .OrderByDescending(o => o.OrderDate)
                .Take(10)
                .ToListAsync();

            var now = DateTime.UtcNow;
            var monthNames = new[] { "T1", "T2", "T3", "T4", "T5", "T6", "T7", "T8", "T9", "T10", "T11", "T12" };

            var monthlyRevenue = await _context.Orders
                .Where(o => o.SupermarketId == id && o.PaymentStatus == "Paid" && o.Status != "Cancelled"
                    && o.OrderDate.Year == now.Year)
                .GroupBy(o => o.OrderDate.Month)
                .Select(g => new { Month = g.Key, Total = g.Sum(o => o.TotalAmount) })
                .ToListAsync();

            var labels = new List<string>();
            var data = new List<decimal>();
            for (int m = 1; m <= now.Month; m++)
            {
                labels.Add(monthNames[m - 1]);
                data.Add(monthlyRevenue.FirstOrDefault(x => x.Month == m)?.Total ?? 0);
            }
            RevenueChartLabels = System.Text.Json.JsonSerializer.Serialize(labels);
            RevenueChartData = System.Text.Json.JsonSerializer.Serialize(data);

            return Page();
        }
    }
}
