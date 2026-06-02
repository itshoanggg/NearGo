using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NearGo.Data;
using NearGo.Models;

namespace NearGo.Pages.Admin.Reports
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

        public NearGo.Models.Report Report { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Report = await _context.Reports
                .Include(r => r.Reporter)
                .Include(r => r.Supermarket)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (Report == null) return NotFound();
            return Page();
        }

        public async Task<IActionResult> OnPostRespondAsync(int id, string action, string? adminResponse)
        {
            var report = await _context.Reports.FindAsync(id);
            if (report == null) return NotFound();
            if (report.Status != "Pending") return BadRequest("Báo cáo đã được xử lý");

            var adminId = _userManager.GetUserId(User)!;
            report.AdminId = adminId;
            report.AdminResponse = adminResponse?.Trim();
            report.Status = action == "resolve" ? "Resolved" : "Rejected";
            report.ResolvedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã cập nhật báo cáo!";
            return RedirectToPage("Detail", new { id });
        }

        public async Task<IActionResult> OnPostRequestExplanationAsync(int id)
        {
            var report = await _context.Reports.Include(r => r.Supermarket).FirstOrDefaultAsync(r => r.Id == id);
            if (report == null) return NotFound();
            if (!report.SupermarketId.HasValue) return BadRequest("Báo cáo không có siêu thị liên quan");

            report.RequestedExplanation = true;
            report.ExplanationRequestedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã yêu cầu siêu thị \"{report.Supermarket?.Name}\" giải trình!";
            return RedirectToPage("Detail", new { id });
        }
    }
}
