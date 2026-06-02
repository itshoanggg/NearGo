using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NearGo.Data;
using NearGo.Models;

namespace NearGo.Pages.Customer.Reports
{
    [Authorize(Roles = "Customer")]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public CreateModel(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public List<NearGo.Models.Supermarket> Supermarkets { get; set; } = new();

        public async Task OnGetAsync()
        {
            Supermarkets = await _context.Supermarkets
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync(int? supermarketId, string title, string description)
        {
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(description))
            {
                ModelState.AddModelError("", "Vui lòng điền đầy đủ thông tin");
                Supermarkets = await _context.Supermarkets.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync();
                return Page();
            }

            var userId = _userManager.GetUserId(User)!;
            var report = new NearGo.Models.Report
            {
                ReporterId = userId,
                SupermarketId = supermarketId,
                Title = title.Trim(),
                Description = description.Trim(),
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };
            _context.Reports.Add(report);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Báo cáo đã được gửi thành công!";
            return RedirectToPage("Index");
        }
    }
}
