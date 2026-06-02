using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NearGo.Data;
using NearGo.Models;

namespace NearGo.Pages.Admin.Reports
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<NearGo.Models.Report> Reports { get; set; } = new();
        public string? Filter { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public const int PageSize = 10;

        public async Task OnGetAsync(string? filter, int? p)
        {
            Filter = filter;
            CurrentPage = p ?? 1;

            var query = _context.Reports
                .Include(r => r.Reporter)
                .Include(r => r.Supermarket)
                .AsQueryable();

            query = filter switch
            {
                "pending" => query.Where(r => r.Status == "Pending"),
                "resolved" => query.Where(r => r.Status == "Resolved"),
                _ => query
            };

            TotalCount = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);
            if (TotalPages < 1) TotalPages = 1;
            if (CurrentPage < 1) CurrentPage = 1;
            if (CurrentPage > TotalPages) CurrentPage = TotalPages;

            Reports = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();
        }
    }
}
