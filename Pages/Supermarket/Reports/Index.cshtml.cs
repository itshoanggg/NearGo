using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NearGo.Data;
using NearGo.Models;

namespace NearGo.Pages.Supermarket.Reports
{
    [Authorize(Roles = "Supermarket")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public IndexModel(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public List<NearGo.Models.Report> Reports { get; set; } = new();

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.SupermarketId == null) return;

            Reports = await _context.Reports
                .Where(r => r.SupermarketId == user.SupermarketId.Value)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostSubmitExplanationAsync(int id, string explanation)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.SupermarketId == null) return Unauthorized();

            var report = await _context.Reports
                .FirstOrDefaultAsync(r => r.Id == id && r.SupermarketId == user.SupermarketId.Value);

            if (report == null) return NotFound();
            if (!string.IsNullOrEmpty(report.SupermarketExplanation)) return BadRequest("Đã giải trình rồi");

            report.SupermarketExplanation = explanation?.Trim();
            report.ExplanationProvidedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã gửi giải trình!";
            return RedirectToPage();
        }
    }
}
