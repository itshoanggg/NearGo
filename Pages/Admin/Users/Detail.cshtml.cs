using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NearGo.Data;
using NearGo.Models;

namespace NearGo.Pages.Admin.Users
{
    [Authorize(Roles = "Admin")]
    public class DetailModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public DetailModel(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public AppUser UserDetail { get; set; } = default!;
        public string Role { get; set; } = "Customer";
        public int TotalOrders { get; set; }
        public decimal TotalSpent { get; set; }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            UserDetail = await _context.Users
                .Include(u => u.Orders)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (UserDetail == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(UserDetail);
            Role = roles.FirstOrDefault() ?? "Customer";

            TotalOrders = UserDetail.Orders.Count;
            TotalSpent = UserDetail.Orders
                .Where(o => o.PaymentStatus == "Paid")
                .Sum(o => o.TotalAmount);

            return Page();
        }

        public async Task<IActionResult> OnPostToggleActiveAsync(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.IsActive = !user.IsActive;

            if (user.IsActive)
            {
                user.LockoutEnabled = false;
                user.LockoutEnd = null;
            }
            else
            {
                user.LockoutEnabled = true;
                user.LockoutEnd = DateTimeOffset.MaxValue;
            }

            await _userManager.UpdateAsync(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = user.IsActive
                ? $"Đã mở khóa tài khoản \"{user.FullName}\""
                : $"Đã khóa tài khoản \"{user.FullName}\"";

            return RedirectToPage("Detail", new { id });
        }
    }
}
