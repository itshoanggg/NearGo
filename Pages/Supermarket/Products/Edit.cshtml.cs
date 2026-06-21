using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NearGo.Data;
using NearGo.Models;
using System.ComponentModel.DataAnnotations;

namespace NearGo.Pages.Supermarket.Products
{
    [Authorize(Roles = "Supermarket")]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public EditModel(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();
        public List<Category> Categories { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "Tên sản phẩm là bắt buộc")]
            public string Name { get; set; } = string.Empty;
            public string? Description { get; set; }
            [Required]
            public int CategoryId { get; set; }
            [Required, Range(1000, 100000000)]
            public decimal OriginalPrice { get; set; }
            [Required, Range(0, 100000000)]
            public decimal DiscountedPrice { get; set; }
            [Required, Range(0, 100000)]
            public int StockQuantity { get; set; }
            [Required]
            public DateTime ExpiryDate { get; set; }
            public string? ImageUrl { get; set; }
            public IFormFile? ImageFile { get; set; }
            public string? Unit { get; set; } = "cái";
            public string? Origin { get; set; }
            public bool IsActive { get; set; } = true;
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.SupermarketId == null) return Forbid();

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.SupermarketId == user.SupermarketId.Value);
            if (product == null) return NotFound();

            Categories = await _context.Categories.Where(c => c.IsActive).OrderBy(c => c.SortOrder).ToListAsync();

            Input.Name = product.Name;
            Input.Description = product.Description;
            Input.CategoryId = product.CategoryId;
            Input.OriginalPrice = product.OriginalPrice;
            Input.DiscountedPrice = product.DiscountedPrice;
            Input.StockQuantity = product.StockQuantity;
            Input.ExpiryDate = product.ExpiryDate;
            Input.ImageUrl = product.ImageUrl;
            Input.Unit = product.Unit;
            Input.Origin = product.Origin;
            Input.IsActive = product.IsActive;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            if (!ModelState.IsValid)
            {
                Categories = await _context.Categories.Where(c => c.IsActive).OrderBy(c => c.SortOrder).ToListAsync();
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user?.SupermarketId == null) return Forbid();

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.SupermarketId == user.SupermarketId.Value);
            if (product == null) return NotFound();

            var discountPct = Input.OriginalPrice > 0
                ? (double)((Input.OriginalPrice - Input.DiscountedPrice) / Input.OriginalPrice * 100)
                : 0;

            product.Name = Input.Name;
            product.Description = Input.Description;
            product.CategoryId = Input.CategoryId;
            product.OriginalPrice = Input.OriginalPrice;
            product.DiscountedPrice = Input.DiscountedPrice;
            product.DiscountPercentage = Math.Round(discountPct, 1);
            product.StockQuantity = Input.StockQuantity;
            product.ExpiryDate = Input.ExpiryDate;
            if (Input.ImageFile != null && Input.ImageFile.Length > 0)
            {
                using var ms = new MemoryStream();
                await Input.ImageFile.CopyToAsync(ms);
                product.ImageData = ms.ToArray();
                product.ImageContentType = Input.ImageFile.ContentType;
                product.ImageUrl = $"/image/product/{product.Id}";
            }
            else
            {
                product.ImageUrl = Input.ImageUrl ?? product.ImageUrl;
            }
            product.Unit = Input.Unit ?? "cái";
            product.Origin = Input.Origin;
            product.IsActive = Input.IsActive;
            product.SmartExpiryScore = Math.Round(100 - ((DateTime.UtcNow.AddDays(60) - Input.ExpiryDate).TotalDays / 60 * 100), 1);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật sản phẩm thành công!";
            return RedirectToPage("/Supermarket/Products");
        }
    }
}
