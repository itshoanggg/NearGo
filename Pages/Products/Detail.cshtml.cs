using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NearGo.Data;
using NearGo.Models;

namespace NearGo.Pages.Products
{
    public class DetailModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Product? Product { get; set; }
        public List<Product> RelatedProducts { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Product = await _context.Products
                .Include(p => p.Supermarket)
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (Product == null || Product.StockQuantity <= 0 || Product.ExpiryDate <= DateTime.UtcNow)
            {
                Product = null;
                return Page();
            }

            Product.ViewCount++;
            await _context.SaveChangesAsync();

            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = _context.Users.Where(u => u.UserName == User.Identity.Name).Select(u => u.Id).FirstOrDefault();
                if (userId != null)
                {
                    var existing = await _context.RecentlyVieweds
                        .FirstOrDefaultAsync(rv => rv.UserId == userId && rv.ProductId == id);
                    if (existing != null)
                    {
                        existing.ViewedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        _context.RecentlyVieweds.Add(new RecentlyViewed
                        {
                            UserId = userId,
                            ProductId = id,
                            ViewedAt = DateTime.UtcNow
                        });
                    }
                    await _context.SaveChangesAsync();
                }
            }

            var now = DateTime.UtcNow;
            RelatedProducts = await _context.Products
                .Include(p => p.Supermarket)
                .Where(p => p.CategoryId == Product.CategoryId && p.Id != id && p.IsActive && p.StockQuantity > 0 && p.ExpiryDate > now)
                .OrderByDescending(p => p.DealScore)
                .Take(6)
                .ToListAsync();

            return Page();
        }
    }
}
