using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NearGo.Data;
using NearGo.Models;

namespace NearGo.Pages.Supermarket
{
    [Authorize(Roles = "Supermarket")]
    public class BankInfoModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public BankInfoModel(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public string BankName { get; set; } = string.Empty;

        [BindProperty]
        public string AccountNumber { get; set; } = string.Empty;

        [BindProperty]
        public string AccountHolder { get; set; } = string.Empty;

        [BindProperty]
        public bool AgreedToTerms { get; set; }

        public bool HasBankInfo { get; set; }

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.SupermarketId == null) return;

            var bankInfo = await _context.SupermarketBankInfos
                .FirstOrDefaultAsync(b => b.SupermarketId == user.SupermarketId.Value);

            if (bankInfo != null)
            {
                HasBankInfo = true;
                BankName = bankInfo.BankName;
                AccountNumber = bankInfo.AccountNumber;
                AccountHolder = bankInfo.AccountHolder;
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            if (!AgreedToTerms)
            {
                TempData["Error"] = "Bạn cần đồng ý với cam kết trước khi lưu thông tin";
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user?.SupermarketId == null)
                return NotFound();

            var bankInfo = await _context.SupermarketBankInfos
                .FirstOrDefaultAsync(b => b.SupermarketId == user.SupermarketId.Value);

            if (bankInfo == null)
            {
                bankInfo = new SupermarketBankInfo
                {
                    SupermarketId = user.SupermarketId.Value,
                    BankName = BankName,
                    AccountNumber = AccountNumber,
                    AccountHolder = AccountHolder,
                    CreatedAt = DateTime.UtcNow
                };
                _context.SupermarketBankInfos.Add(bankInfo);
                TempData["Success"] = "Đã lưu thông tin tài khoản ngân hàng";
            }
            else
            {
                bankInfo.BankName = BankName;
                bankInfo.AccountNumber = AccountNumber;
                bankInfo.AccountHolder = AccountHolder;
                bankInfo.UpdatedAt = DateTime.UtcNow;
                TempData["Success"] = "Đã cập nhật thông tin tài khoản ngân hàng";
            }

            await _context.SaveChangesAsync();
            HasBankInfo = true;
            return RedirectToPage();
        }
    }
}
