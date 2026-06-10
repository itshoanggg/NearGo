using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NearGo.Data;
using Microsoft.AspNetCore.SignalR;
using NearGo.Models;

namespace NearGo.Pages.Supermarket
{
    [Authorize(Roles = "Supermarket")]
    public class OrdersModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IHubContext<Hubs.NotificationHub> _hubContext;

        public OrdersModel(ApplicationDbContext context, UserManager<AppUser> userManager,
            IHubContext<Hubs.NotificationHub> hubContext)
        {
            _context = context;
            _userManager = userManager;
            _hubContext = hubContext;
        }

        public List<NearGo.Models.Order> Orders { get; set; } = new();
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; } = 1;
        public string? Search { get; set; }
        public string? Status { get; set; }
        public string? Payment { get; set; }
        private const int PageSize = 15;

        public async Task OnGetAsync(string? search, string? status, string? payment, int p = 1)
        {
            Search = search;
            Status = status;
            Payment = payment;
            CurrentPage = Math.Max(1, p);

            var user = await _userManager.GetUserAsync(User);
            if (user?.SupermarketId == null) return;

            var query = _context.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.Customer)
                .Where(o => o.SupermarketId == user.SupermarketId.Value)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                var lower = search.ToLower();
                query = query.Where(o => o.OrderCode.ToLower().Contains(lower)
                    || (o.Customer != null && o.Customer.FullName.ToLower().Contains(lower)));
            }
            if (!string.IsNullOrEmpty(status))
                query = query.Where(o => o.Status == status);
            if (!string.IsNullOrEmpty(payment))
                query = query.Where(o => o.PaymentStatus == payment);

            query = query.OrderByDescending(o => o.OrderDate);
            TotalCount = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);

            Orders = await query.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToListAsync();
        }

        public async Task<IActionResult> OnGetUpdateStatusAsync(int id, string status)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.SupermarketId == null) return Forbid();

            var order = await _context.Orders
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.Id == id && o.SupermarketId == user.SupermarketId.Value);
            if (order == null) return NotFound();

            if (status == "Confirmed" && order.Status == "Pending")
            {
                order.Status = "Confirmed";
            }
            else if (status == "Received" && order.Status == "Confirmed")
            {
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
            }
            else if (status == "Cancelled" && order.Status != "Received")
            {
                order.Status = "Cancelled";
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
            else
            {
                TempData["Error"] = "Không thể cập nhật trạng thái";
                return RedirectToPage("Orders");
            }

            await _context.SaveChangesAsync();

            try
            {
                await _hubContext.Clients.Group($"user_{order.CustomerId}")
                    .SendAsync("ReceiveNotification", "Cập nhật đơn hàng",
                        $"Đơn hàng #{order.OrderCode} đã chuyển sang trạng thái: {status}", "");
            }
            catch { }

            TempData["Success"] = $"Đã cập nhật đơn hàng #{order.OrderCode}";
            return RedirectToPage("Orders");
        }

        public async Task<IActionResult> OnGetReceiveByQrAsync(string code)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.SupermarketId == null) return Forbid();

            var order = await _context.Orders
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.OrderCode == code && o.SupermarketId == user.SupermarketId.Value);
            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng với mã này";
                return RedirectToPage("ScanQR");
            }

            if (order.Status != "Confirmed")
            {
                TempData["Error"] = "Đơn hàng chưa được xác nhận hoặc đã nhận hàng";
                return RedirectToPage("ScanQR");
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
