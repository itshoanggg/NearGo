using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NearGo.Data;
using NearGo.Models;
using NearGo.Services;

namespace NearGo.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class WithdrawalsModel : PageModel
    {
        private readonly FinanceService _financeService;
        private readonly UserManager<AppUser> _userManager;

        public WithdrawalsModel(FinanceService financeService, UserManager<AppUser> userManager)
        {
            _financeService = financeService;
            _userManager = userManager;
        }

        public List<WithdrawalRequest> Withdrawals { get; set; } = new();
        public string Filter { get; set; } = "all";

        [BindProperty]
        public int RequestId { get; set; }

        [BindProperty]
        public string? RejectReason { get; set; }

        [BindProperty]
        public string RequestFilter { get; set; } = "all";

        public async Task OnGetAsync(string filter = "all")
        {
            Filter = filter;
            Withdrawals = filter switch
            {
                "pending" => await _financeService.GetPendingWithdrawals(),
                "approved" => await _financeService.GetApprovedWithdrawals(),
                "paid" => await _financeService.GetPaidWithdrawals(),
                _ => await _financeService.GetAllWithdrawals()
            };
        }

        public async Task<IActionResult> OnPostApproveAsync()
        {
            var admin = await _userManager.GetUserAsync(User);
            if (admin == null) return NotFound();

            var success = await _financeService.ApproveWithdrawalRequest(RequestId, admin.Id);

            if (success)
                TempData["Success"] = "Đã duyệt yêu cầu rút tiền";
            else
                TempData["Error"] = "Không thể duyệt yêu cầu";

            return RedirectToPage(new { filter = RequestFilter });
        }

        public async Task<IActionResult> OnPostRejectAsync()
        {
            var admin = await _userManager.GetUserAsync(User);
            if (admin == null) return NotFound();

            var success = await _financeService.RejectWithdrawalRequest(RequestId, admin.Id, RejectReason);

            if (success)
                TempData["Success"] = "Đã từ chối yêu cầu rút tiền";
            else
                TempData["Error"] = "Không thể từ chối yêu cầu";

            return RedirectToPage(new { filter = RequestFilter });
        }

        public async Task<IActionResult> OnPostPaidAsync()
        {
            var admin = await _userManager.GetUserAsync(User);
            if (admin == null) return NotFound();

            if (RequestId <= 0)
            {
                TempData["Error"] = "Mã yêu cầu không hợp lệ";
                return RedirectToPage(new { filter = RequestFilter });
            }

            try
            {
                var success = await _financeService.MarkAsPaid(RequestId, admin.Id);
                if (success)
                    TempData["Success"] = "Đã xác nhận đã chuyển tiền";
                else
                    TempData["Error"] = "Không thể xác nhận";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToPage(new { filter = RequestFilter });
        }
    }
}
