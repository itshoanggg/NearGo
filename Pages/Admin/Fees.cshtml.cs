using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NearGo.Data;
using NearGo.Models;

namespace NearGo.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class FeesModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public FeesModel(ApplicationDbContext context) => _context = context;

        public List<PlatformFee> Fees { get; set; } = new();
        public List<NearGo.Models.Supermarket> Supermarkets { get; set; } = new();
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; } = 1;
        public string? Search { get; set; }
        public string? Status { get; set; }
        public string? FeeType { get; set; }
        public int? SupermarketId { get; set; }
        private const int PageSize = 20;

        public async Task OnGetAsync(string? search, string? status, string? feeType, int? supermarketId, int p = 1)
        {
            Search = search;
            Status = status;
            FeeType = feeType;
            SupermarketId = supermarketId;
            CurrentPage = Math.Max(1, p);

            Supermarkets = await _context.Supermarkets.OrderBy(s => s.Name).ToListAsync();

            var query = _context.PlatformFees
                .Include(f => f.Supermarket)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                var lower = search.ToLower();
                query = query.Where(f => (f.Description != null && f.Description.ToLower().Contains(lower)));
            }
            if (!string.IsNullOrEmpty(status))
                query = query.Where(f => f.Status == status);
            if (!string.IsNullOrEmpty(feeType))
                query = query.Where(f => f.FeeType == feeType);
            if (supermarketId.HasValue && supermarketId > 0)
                query = query.Where(f => f.SupermarketId == supermarketId.Value);

            query = query.OrderByDescending(f => f.CreatedAt);
            TotalCount = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);

            Fees = await query.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToListAsync();
        }
    }
}
