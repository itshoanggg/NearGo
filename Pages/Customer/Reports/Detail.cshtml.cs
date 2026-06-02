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
            var userId = _userManager.GetUserId(User)!;
            Report = await _context.Reports
                .Include(r => r.Supermarket)
                .FirstOrDefaultAsync(r => r.Id == id && r.ReporterId == userId);

            if (Report == null) return NotFound();
            return Page();
        }
    }
}
