using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NearGo.Data;
using NearGo.Models;
using NearGo.Services;

namespace NearGo.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class CommissionSettingsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly FinanceService _financeService;
        private readonly UserManager<AppUser> _userManager;

        public CommissionSettingsModel(ApplicationDbContext context, FinanceService financeService, UserManager<AppUser> userManager)
        {
            _context = context;
            _financeService = financeService;
            _userManager = userManager;
        }

        [BindProperty]
        public decimal CommissionPercent { get; set; }

        public decimal CurrentPercent { get; set; }
        public DateTime? LastUpdated { get; set; }
        public string? UpdatedByName { get; set; }

        public async Task OnGetAsync()
        {
            CommissionPercent = await _financeService.GetCommissionPercent();
            CurrentPercent = CommissionPercent;

            var setting = await _context.PlatformSettings.FirstOrDefaultAsync(s => s.Key == "commission_percent");
            if (setting != null)
            {
                LastUpdated = setting.UpdatedAt;
                if (!string.IsNullOrEmpty(setting.UpdatedBy))
                {
                    var admin = await _userManager.FindByIdAsync(setting.UpdatedBy);
                    UpdatedByName = admin?.FullName ?? admin?.Email;
                }
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (CommissionPercent < 0 || CommissionPercent > 100)
            {
                TempData["Error"] = "Phần trăm hoa hồng phải từ 0 đến 100";
                return RedirectToPage();
            }

            var user = await _userManager.GetUserAsync(User);
            var setting = await _context.PlatformSettings.FirstOrDefaultAsync(s => s.Key == "commission_percent");

            if (setting == null)
            {
                setting = new PlatformSetting
                {
                    Key = "commission_percent",
                    Value = CommissionPercent.ToString(),
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = user?.Id
                };
                _context.PlatformSettings.Add(setting);
            }
            else
            {
                setting.Value = CommissionPercent.ToString();
                setting.UpdatedAt = DateTime.UtcNow;
                setting.UpdatedBy = user?.Id;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã cập nhật hoa hồng thành {CommissionPercent}%";
            return RedirectToPage();
        }
    }
}
