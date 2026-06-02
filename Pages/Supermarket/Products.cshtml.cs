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
    public class ProductsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public ProductsModel(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public List<NearGo.Models.Product> Products { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; } = 1;
        public string? Search { get; set; }
        public int? CategoryId { get; set; }
        public string? Status { get; set; }
        private const int PageSize = 15;

        public async Task OnGetAsync(string? search, int? categoryId, string? status, int p = 1)
        {
            Search = search;
            CategoryId = categoryId;
            Status = status;
            CurrentPage = Math.Max(1, p);

            Categories = await _context.Categories.Where(c => c.IsActive).OrderBy(c => c.SortOrder).ToListAsync();

            var user = await _userManager.GetUserAsync(User);
            if (user?.SupermarketId == null) return;

            var query = _context.Products
                .Include(p => p.Category)
                .Where(p => p.SupermarketId == user.SupermarketId.Value)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                var lower = search.ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(lower));
            }
            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            var now = DateTime.UtcNow;
            query = Status switch
            {
                "active" => query.Where(p => p.IsActive && p.StockQuantity > 0 && p.ExpiryDate > now),
                "hidden" => query.Where(p => !p.IsActive),
                "out" => query.Where(p => p.StockQuantity <= 0),
                "expired" => query.Where(p => p.ExpiryDate <= now),
                _ => query
            };

            query = query.OrderByDescending(p => p.CreatedAt);

            TotalCount = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);

            Products = await query.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToListAsync();
        }

        public async Task<IActionResult> OnGetDeleteAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.SupermarketId == null) return Forbid();

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.SupermarketId == user.SupermarketId.Value);
            if (product == null) return NotFound();

            product.IsActive = false;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã ẩn sản phẩm";
            return RedirectToPage("Products");
        }

        public async Task<IActionResult> OnGetUnhideAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.SupermarketId == null) return Forbid();

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.SupermarketId == user.SupermarketId.Value);
            if (product == null) return NotFound();

            product.IsActive = true;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã hiện sản phẩm";
            return RedirectToPage("Products");
        }
    }
}
