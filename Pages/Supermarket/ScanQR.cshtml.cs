using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NearGo.Data;
using NearGo.Models;

namespace NearGo.Pages.Supermarket
{
    [Authorize(Roles = "Supermarket")]
    public class ScanQRModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IHubContext<Hubs.NotificationHub> _hubContext;

        public ScanQRModel(ApplicationDbContext context, UserManager<AppUser> userManager,
            IHubContext<Hubs.NotificationHub> hubContext)
        {
            _context = context;
            _userManager = userManager;
            _hubContext = hubContext;
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

        public async Task<IActionResult> OnPostReceiveOrderAsync(int id, bool? confirmPayment)
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

            if (order.PaymentStatus == "Unpaid" && confirmPayment != true)
            {
                TempData["ConfirmPayment"] = order.Id.ToString();
                TempData["ConfirmAmount"] = order.TotalAmount.ToString("N0");
                return RedirectToPage("ScanQR", new { code = order.OrderCode });
            }

            if (order.PaymentStatus == "Unpaid")
            {
                order.PaymentStatus = "Paid";
                order.PaymentDate = DateTime.UtcNow;
                var sm = await _context.Supermarkets.FindAsync(order.SupermarketId);
                if (sm != null)
                {
                    sm.TotalOrders = await _context.Orders
                        .CountAsync(o => o.SupermarketId == order.SupermarketId && o.Status != "Cancelled");
                    sm.TotalRevenue = await _context.Orders
                        .Where(o => o.SupermarketId == order.SupermarketId && o.PaymentStatus == "Paid" && o.Status != "Cancelled")
                        .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
                }
            }

            order.Status = "Received";
            order.DeliveredDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

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
