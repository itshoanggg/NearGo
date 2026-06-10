using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NearGo.Data;
using NearGo.Models;

namespace NearGo.Pages.Customer
{
    [Authorize(Roles = "Customer")]
    public class RateSupermarketModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public RateSupermarketModel(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public int OrderId { get; set; }
        public string SupermarketName { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            var order = await _context.Orders
                .Include(o => o.Supermarket)
                .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == userId);

            if (order == null) return NotFound();
            if (order.Status != "Received") return BadRequest("Đơn hàng chưa được nhận");

            var alreadyRated = await _context.SupermarketRatings.AnyAsync(r => r.OrderId == id);
            if (alreadyRated) return BadRequest("Bạn đã đánh giá đơn hàng này rồi");

            OrderId = id;
            SupermarketName = order.Supermarket.Name;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int orderId, int rating, string? comment)
        {
            var userId = _userManager.GetUserId(User)!;
            var order = await _context.Orders
                .Include(o => o.Supermarket)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.CustomerId == userId);

            if (order == null) return NotFound();
            if (order.Status != "Received") return BadRequest("Đơn hàng chưa được nhận");
            if (rating < 1 || rating > 5) return BadRequest("Sao không hợp lệ");

            var alreadyRated = await _context.SupermarketRatings.AnyAsync(r => r.OrderId == orderId);
            if (alreadyRated) return BadRequest("Bạn đã đánh giá đơn hàng này rồi");

            var supermarketRating = new SupermarketRating
            {
                OrderId = orderId,
                UserId = userId,
                SupermarketId = order.SupermarketId,
                Rating = rating,
                Comment = comment,
                CreatedAt = DateTime.UtcNow
            };
            _context.SupermarketRatings.Add(supermarketRating);
            await _context.SaveChangesAsync();

            var ratingsList = await _context.SupermarketRatings
                .Where(r => r.SupermarketId == order.SupermarketId)
                .ToListAsync();

            var supermarket = await _context.Supermarkets.FindAsync(order.SupermarketId);
            if (supermarket != null)
            {
                supermarket.Rating = ratingsList.Any() ? ratingsList.Average(r => (double)r.Rating) : 0;
                supermarket.ReviewCount = ratingsList.Count;
                await _context.SaveChangesAsync();
            }
            TempData["Success"] = "Cảm ơn bạn đã đánh giá!";
            return RedirectToPage("/Customer/Orders/Detail", new { id = orderId });
        }
    }
}
