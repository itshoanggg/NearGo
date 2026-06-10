using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NearGo.Data;
using NearGo.Models;

namespace NearGo.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class OrdersModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public OrdersModel(ApplicationDbContext context) => _context = context;

        public List<Order> Orders { get; set; } = new();
        public List<NearGo.Models.Supermarket> Supermarkets { get; set; } = new();
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; } = 1;
        public string? Search { get; set; }
        public string? Status { get; set; }
        public int? SupermarketId { get; set; }
        private const int PageSize = 20;

        public async Task OnGetAsync(string? search, string? status, int? supermarketId, int p = 1)
        {
            Search = search;
            Status = status;
            SupermarketId = supermarketId;
            CurrentPage = Math.Max(1, p);

            Supermarkets = await _context.Supermarkets.OrderBy(s => s.Name).ToListAsync();

            var query = _context.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.Customer)
                .Include(o => o.Supermarket)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                var lower = search.ToLower();
                query = query.Where(o => o.OrderCode.ToLower().Contains(lower)
                    || (o.Customer != null && o.Customer.FullName.ToLower().Contains(lower)));
            }
            if (!string.IsNullOrEmpty(status))
                query = query.Where(o => o.Status == status);
            if (supermarketId.HasValue && supermarketId > 0)
                query = query.Where(o => o.SupermarketId == supermarketId.Value);

            query = query.OrderByDescending(o => o.OrderDate);
            TotalCount = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);

            Orders = await query.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToListAsync();
        }
    }
}
