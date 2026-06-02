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
    public class WalletModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly FinanceService _financeService;

        public WalletModel(ApplicationDbContext context, UserManager<AppUser> userManager, FinanceService financeService)
        {
            _context = context;
            _userManager = userManager;
            _financeService = financeService;
        }

        public decimal Balance { get; set; }
        public decimal TotalEarned { get; set; }
        public decimal TotalCommission { get; set; }
        public bool HasBankInfo { get; set; }
        public string? BankName { get; set; }
        public string? AccountNumber { get; set; }
        public string? AccountHolder { get; set; }
        public decimal DefaultCommissionPercent { get; set; }
        public List<WithdrawalRequest> Withdrawals { get; set; } = new();

        [BindProperty]
        public decimal WithdrawAmount { get; set; }

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.SupermarketId == null) return;

            var smId = user.SupermarketId.Value;
            var supermarket = await _context.Supermarkets.FindAsync(smId);
            if (supermarket == null) return;

            Balance = supermarket.Balance;
            DefaultCommissionPercent = _financeService.DefaultCommissionPercent;

            var bankInfo = await _context.SupermarketBankInfos.FirstOrDefaultAsync(b => b.SupermarketId == smId);
            HasBankInfo = bankInfo != null;
            if (bankInfo != null)
            {
                BankName = bankInfo.BankName;
                AccountNumber = bankInfo.AccountNumber;
                AccountHolder = bankInfo.AccountHolder;
            }

            TotalEarned = Balance + await _context.WithdrawalRequests
                .Where(w => w.SupermarketId == smId && w.Status == "Paid")
                .SumAsync(w => (decimal?)w.Amount) ?? 0;

            TotalCommission = await _context.PlatformFees
                .Where(f => f.SupermarketId == smId && f.Status == "Paid")
                .SumAsync(f => (decimal?)f.Amount) ?? 0;

            Withdrawals = await _financeService.GetSupermarketWithdrawals(smId);
        }

        public async Task<IActionResult> OnPostRequestAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.SupermarketId == null)
                return NotFound();

            if (WithdrawAmount <= 0)
            {
                TempData["Error"] = "Số tiền không hợp lệ";
                return RedirectToPage();
            }

            try
            {
                await _financeService.CreateWithdrawalRequest(user.SupermarketId.Value, WithdrawAmount);
                TempData["Success"] = $"Yêu cầu rút {WithdrawAmount:N0}đ đã được gửi, chờ admin xét duyệt";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToPage();
        }
    }
}
