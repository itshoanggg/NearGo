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
        public List<Product> FeaturedProducts { get; set; } = new();
        public List<NearGo.Models.Supermarket> Supermarkets { get; set; } = new();
        public int TotalSupermarkets { get; set; }
        public int TotalProducts { get; set; }
        public double MaxDiscountPercentage { get; set; }

        public async Task OnGetAsync()
        {
            var now = DateTime.UtcNow;

            Categories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.SortOrder)
                .Take(16)
                .ToListAsync();

            FeaturedProducts = await _context.Products
                .Include(p => p.Supermarket)
                .Include(p => p.Category)
                .Where(p => p.IsActive && p.StockQuantity > 0 && p.ExpiryDate > now)
                .OrderByDescending(p => p.IsBoosted)
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
