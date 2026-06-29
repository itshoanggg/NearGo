using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NearGo.Data;
using NearGo.Models;

namespace NearGo.Pages.Products
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Product> Products { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public string? CategorySlug { get; set; }
        public string? Area { get; set; }
        public string? CategoryName { get; set; }
        public string? Sort { get; set; }
        public string? SearchQuery { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        private const int PageSize = 20;

        public async Task OnGetAsync(string? category, string? area, string? sort, string? search, decimal? minPrice, decimal? maxPrice, int p = 1)
        {
            CategorySlug = category;
            Area = area;
            Sort = sort ?? "newest";
            SearchQuery = search;
            MinPrice = minPrice;
            MaxPrice = maxPrice;
            CurrentPage = Math.Max(1, p);

            Categories = await _context.Categories.Where(c => c.IsActive).OrderBy(c => c.SortOrder).ToListAsync();

            var now = DateTime.UtcNow;
            var query = _context.Products
                .Include(p => p.Supermarket)
                .Include(p => p.Category)
                .Where(p => p.IsActive && p.StockQuantity > 0 && p.ExpiryDate > now)
                .AsQueryable();

            if (!string.IsNullOrEmpty(category))
            {
                var cat = await _context.Categories.FirstOrDefaultAsync(c => c.Slug == category);
                if (cat != null)
                {
                    query = query.Where(p => p.CategoryId == cat.Id);
                    CategoryName = cat.Name;
                }
            }

            if (!string.IsNullOrEmpty(area))
            {
                var areaLower = area.ToLower();
                query = query.Where(p => p.Supermarket.Address != null && p.Supermarket.Address.ToLower().Contains(areaLower));
            }

            if (!string.IsNullOrEmpty(search))
            {
                var searchLower = search.ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(searchLower) || (p.Tags != null && p.Tags.ToLower().Contains(searchLower)));
            }

            if (minPrice.HasValue) query = query.Where(p => p.DiscountedPrice >= minPrice.Value);
            if (maxPrice.HasValue) query = query.Where(p => p.DiscountedPrice <= maxPrice.Value);

            if (Sort == "discount")
            {
                query = query.Where(p => p.DiscountEndDate > now)
                    .OrderByDescending(p => p.DiscountPercentage);
            }
            else if (Sort == "expiry")
            {
                query = query.Where(p => p.DiscountEndDate == null && p.ExpiryDate <= now.AddDays(30))
                    .OrderBy(p => p.ExpiryDate);
            }
            else
            {
                query = Sort switch
                {
                    "price-asc" => query.OrderBy(p => p.DiscountedPrice),
                    "price-desc" => query.OrderByDescending(p => p.DiscountedPrice),
                    _ => query.OrderByDescending(p => p.DealScore)
                };
            }

            TotalCount = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);

            Products = await query
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();
        }
    }
}
