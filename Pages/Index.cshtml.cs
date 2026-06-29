using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NearGo.Data;
using NearGo.Models;

namespace NearGo.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Category> Categories { get; set; } = new();
        public List<Product> DealProducts { get; set; } = new();
        public List<NearGo.Models.Supermarket> Supermarkets { get; set; } = new();
        public int TotalSupermarkets { get; set; }
        public int TotalProducts { get; set; }
        public double MaxDiscountPercentage { get; set; }
        public string? Filter { get; set; }

        public async Task OnGetAsync(string? filter)
        {
            var now = DateTime.UtcNow;
            Filter = filter;

            Categories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.SortOrder)
                .Take(16)
                .ToListAsync();

            var query = _context.Products
                .Include(p => p.Supermarket)
                .Include(p => p.Category)
                .Where(p => p.IsActive && p.StockQuantity > 0 && p.ExpiryDate > now);

            query = filter switch
            {
                "discount" => query.Where(p => p.DiscountEndDate > now),
                "expiry" => query.Where(p => p.DiscountEndDate == null && p.ExpiryDate <= now.AddDays(30)),
                _ => query
            };

            DealProducts = await query
                .OrderByDescending(p => p.DealScore)
                .ThenByDescending(p => p.DiscountPercentage)
                .ThenByDescending(p => p.ViewCount)
                .Take(15)
                .ToListAsync();

            Supermarkets = await _context.Supermarkets
                .Where(s => s.IsActive)
                .OrderByDescending(s => s.Rating)
                .Take(12)
                .ToListAsync();

            TotalSupermarkets = await _context.Supermarkets.CountAsync(s => s.IsActive);
            TotalProducts = await _context.Products.CountAsync(p => p.IsActive && p.StockQuantity > 0 && p.ExpiryDate > now);
            MaxDiscountPercentage = await _context.Products
                .Where(p => p.IsActive && p.StockQuantity > 0 && p.ExpiryDate > now)
                .MaxAsync(p => (double?)p.DiscountPercentage) ?? 0;
        }
    }
}
