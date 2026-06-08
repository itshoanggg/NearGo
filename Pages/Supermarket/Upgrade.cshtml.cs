using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NearGo.Data;
using NearGo.Models;
using NearGo.Services;

namespace NearGo.Pages.Supermarket
{
    [Authorize(Roles = "Supermarket")]
    public class UpgradeModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly SEPayService _sePayService;

        public UpgradeModel(ApplicationDbContext context, UserManager<AppUser> userManager, SEPayService sePayService)
        {
            _context = context;
            _userManager = userManager;
            _sePayService = sePayService;
        }

        public NearGo.Models.Supermarket? Supermarket { get; set; }

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.SupermarketId == null) return;

            Supermarket = await _context.Supermarkets.FindAsync(user.SupermarketId.Value);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.SupermarketId == null) return Forbid();

            var supermarket = await _context.Supermarkets.FindAsync(user.SupermarketId.Value);
            if (supermarket == null) return Forbid();

            if (supermarket.SubscriptionTier == "Premium")
            {
                TempData["Info"] = "Siêu thị của bạn đã ở gói Premium.";
                return RedirectToPage("/Supermarket/Upgrade");
            }

            var orderCode = $"SUB-{supermarket.Id}";
            return RedirectToPage("/Payment/SEPayReturn", new { orderCode, amount = 199000m });
        }
    }
}
