using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NearGo.Data;
using NearGo.Models;

namespace NearGo.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class ProductsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ProductsModel(ApplicationDbContext context) => _context = context;

        public List<Product> Products { get; set; } = new();
        public List<NearGo.Models.Supermarket> Supermarkets { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; } = 1;
        public string? Search { get; set; }
        public string? ProductStatus { get; set; }
        public int? SupermarketId { get; set; }
        public int? CategoryId { get; set; }
        private const int PageSize = 20;

        public async Task OnGetAsync(string? search, string? productStatus, int? supermarketId, int? categoryId, int p = 1)
        {
            Search = search;
            ProductStatus = productStatus;
            SupermarketId = supermarketId;
            CategoryId = categoryId;
            CurrentPage = Math.Max(1, p);

            Supermarkets = await _context.Supermarkets.OrderBy(s => s.Name).ToListAsync();
            Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();

            var query = _context.Products
                .Include(p => p.Supermarket)
                .Include(p => p.Category)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                var lower = search.ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(lower));
            }
            if (supermarketId.HasValue && supermarketId > 0)
                query = query.Where(p => p.SupermarketId == supermarketId.Value);
            if (categoryId.HasValue && categoryId > 0)
                query = query.Where(p => p.CategoryId == categoryId.Value);
            if (!string.IsNullOrEmpty(productStatus))
            {
                if (productStatus == "Active")
                    query = query.Where(p => p.IsActive && p.ExpiryDate > DateTime.UtcNow);
                else if (productStatus == "Expired")
                    query = query.Where(p => p.ExpiryDate <= DateTime.UtcNow);
                else if (productStatus == "OutOfStock")
                    query = query.Where(p => p.StockQuantity <= 0);
                else if (productStatus == "Inactive")
                    query = query.Where(p => !p.IsActive);
            }

            query = query.OrderByDescending(p => p.CreatedAt);
            TotalCount = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);

            Products = await query.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToListAsync();
        }
    }
}
