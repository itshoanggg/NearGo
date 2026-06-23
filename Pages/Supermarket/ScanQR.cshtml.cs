using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NearGo.Data;
using NearGo.Models;
using NearGo.Services;

namespace NearGo.Pages.Supermarket
{
    [Authorize(Roles = "Supermarket")]
    public class ScanQRModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IHubContext<Hubs.NotificationHub> _hubContext;
        private readonly FinanceService _financeService;

        public ScanQRModel(ApplicationDbContext context, UserManager<AppUser> userManager,
            IHubContext<Hubs.NotificationHub> hubContext, FinanceService financeService)
        {
            _context = context;
            _userManager = userManager;
            _hubContext = hubContext;
            _financeService = financeService;
        }

        public List<NearGo.Models.Order> ConfirmedOrders { get; set; } = new();
        public NearGo.Models.Order? ScannedOrder { get; set; }

        public async Task OnGetAsync(string? code)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.SupermarketId == null) return;

            var query = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Where(o => o.SupermarketId == user.SupermarketId.Value && o.Status == "Confirmed");

            if (!string.IsNullOrEmpty(code))
            {
                var order = await query.FirstOrDefaultAsync(o => o.OrderCode == code);
                if (order != null)
                    ScannedOrder = order;
            }

            ConfirmedOrders = await query
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostReceiveOrderAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.SupermarketId == null) return Forbid();

            var order = await _context.Orders
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.Id == id && o.SupermarketId == user.SupermarketId.Value);
            if (order == null) return NotFound();

            if (order.Status != "Confirmed")
            {
                TempData["Error"] = "Đơn hàng chưa được xác nhận hoặc đã nhận hàng";
                return RedirectToPage("ScanQR");
            }

            order.Status = "Received";
            order.DeliveredDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _financeService.AddOrderEarnings(order.Id);

            try
            {
                await _hubContext.Clients.Group($"user_{order.CustomerId}")
                    .SendAsync("ReceiveNotification", "Nhận hàng thành công",
                        $"Đơn hàng #{order.OrderCode} đã được nhận tại siêu thị", "");
            }
            catch { }

            TempData["Success"] = $"Đã xác nhận nhận hàng cho đơn #{order.OrderCode}";
            return RedirectToPage("ScanQR");
        }
    }
}
