using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NearGo.Data;
using NearGo.Models;
using NearGo.Services;
using System.ComponentModel.DataAnnotations;

namespace NearGo.Pages.Checkout
{
    [Authorize(Roles = "Customer")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly CartService _cartService;
        private readonly UserManager<AppUser> _userManager;

        public IndexModel(ApplicationDbContext context, CartService cartService, UserManager<AppUser> userManager)
        {
            _context = context;
            _cartService = cartService;
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public List<NearGo.Models.CartItem> CartItems { get; set; } = new();
        public decimal SubTotal { get; set; }
        public int PointsBalance { get; set; }
        public bool CanUsePoints => PointsBalance >= 1000;

        public class InputModel
        {
            [Required(ErrorMessage = "Tên người nhận là bắt buộc")]
            public string CustomerName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
            [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
            public string CustomerPhone { get; set; } = string.Empty;

            [Required(ErrorMessage = "Địa chỉ là bắt buộc")]
            public string ShippingAddress { get; set; } = string.Empty;

            public string? Note { get; set; }

            public string PaymentMethod { get; set; } = "SEPay";

            public bool UsePoints { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = _userManager.GetUserId(User)!;
            CartItems = await _cartService.GetCartItems(userId);
            if (!CartItems.Any())
            {
                return RedirectToPage("/Cart/Index");
            }
            SubTotal = _cartService.CalculateCartTotal(CartItems);

            PointsBalance = (await _context.LoyaltyPoints
                .Where(lp => lp.UserId == userId && lp.ExpiryDate > DateTime.UtcNow)
                .SumAsync(lp => (int?)lp.Points)) ?? 0;

            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                Input.CustomerName = user.FullName;
                Input.CustomerPhone = user.PhoneNumber ?? "";
                Input.ShippingAddress = user.Address ?? "";
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return await OnGetAsync();

            var userId = _userManager.GetUserId(User)!;
            CartItems = await _cartService.GetCartItems(userId);
            if (!CartItems.Any())
            {
                return RedirectToPage("/Cart/Index");
            }
            SubTotal = _cartService.CalculateCartTotal(CartItems);

            var supermarketIds = CartItems.Select(c => c.Product.SupermarketId).Distinct();
            var firstSmId = supermarketIds.FirstOrDefault();
            var orderCode = $"NG{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(100, 999)}";

            var pending = new PendingCheckout
            {
                OrderCode = orderCode,
                UserId = userId,
                SupermarketId = firstSmId,
                ShippingAddress = Input.ShippingAddress,
                CustomerName = Input.CustomerName,
                CustomerPhone = Input.CustomerPhone,
                Note = Input.Note,
                UsePoints = Input.UsePoints,
                CreatedAt = DateTime.UtcNow
            };
            _context.PendingCheckouts.Add(pending);
            await _context.SaveChangesAsync();

            return RedirectToPage("/Payment/SEPayReturn", new { orderCode });
        }
    }
}
