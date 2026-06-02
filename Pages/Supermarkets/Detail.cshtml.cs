using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NearGo.Data;
using NearGo.Models;

namespace NearGo.Pages.Supermarkets
{
    public class DetailModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public DetailModel(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public NearGo.Models.Supermarket? Supermarket { get; set; }
        public List<Product> Products { get; set; } = new();
        public List<SupermarketRating> Ratings { get; set; } = new();
        public bool IsFollowing { get; set; }

        public async Task<IActionResult> OnGetAsync(string slug)
        {
            Supermarket = await _context.Supermarkets
                .Include(s => s.Followers)
                .FirstOrDefaultAsync(s => s.Slug == slug);
            if (Supermarket == null) return NotFound();

            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = _userManager.GetUserId(User)!;
                IsFollowing = Supermarket.Followers.Any(f => f.Id == userId);
            }

            var now = DateTime.UtcNow;
            Products = await _context.Products
                .Where(p => p.SupermarketId == Supermarket.Id && p.IsActive && p.StockQuantity > 0 && p.ExpiryDate > now)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            Ratings = await _context.SupermarketRatings
                .Include(r => r.User)
                .Where(r => r.SupermarketId == Supermarket.Id)
                .OrderByDescending(r => r.CreatedAt)
                .Take(20)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostToggleFollowAsync(string slug)
        {
            if (!User.Identity?.IsAuthenticated ?? true)
                return Unauthorized();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var supermarket = await _context.Supermarkets
                .Include(s => s.Followers)
                .FirstOrDefaultAsync(s => s.Slug == slug);
            if (supermarket == null) return NotFound();

            if (user.SupermarketId == supermarket.Id)
                return RedirectToPage(new { slug });

            var follower = supermarket.Followers.FirstOrDefault(f => f.Id == user.Id);
            if (follower != null)
            {
                supermarket.Followers.Remove(follower);
            }
            else
            {
                supermarket.Followers.Add(user);
            }

            await _context.SaveChangesAsync();
            return RedirectToPage(new { slug });
        }
    }
}
