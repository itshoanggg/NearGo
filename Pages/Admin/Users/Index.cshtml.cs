using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NearGo.Data;
using NearGo.Models;

namespace NearGo.Pages.Admin.Users
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public IndexModel(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public List<AppUser> Users { get; set; } = new();
        public Dictionary<string, string> UserRoles { get; set; } = new();
        public string? Filter { get; set; }
        public string? Search { get; set; }
        public string? RoleFilter { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public const int PageSize = 10;

        public async Task OnGetAsync(string? filter, string? search, string? role, int? p)
        {
            Filter = filter;
            Search = search;
            RoleFilter = role;
            CurrentPage = p ?? 1;

            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                var s = search.ToLower();
                    query = query.Where(x => (x.FullName ?? "").ToLower().Contains(s)
                    || x.Email.ToLower().Contains(s)
                    || (x.PhoneNumber ?? "").Contains(s));
            }

            query = filter switch
            {
                "active" => query.Where(x => x.IsActive),
                "inactive" => query.Where(x => !x.IsActive),
                _ => query
            };

            var roleUserIds = new List<string>();
            if (!string.IsNullOrEmpty(role))
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(role);
                roleUserIds = usersInRole.Select(u => u.Id).ToList();
                query = query.Where(u => roleUserIds.Contains(u.Id));
            }

            TotalCount = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);
            if (TotalPages < 1) TotalPages = 1;
            if (CurrentPage < 1) CurrentPage = 1;
            if (CurrentPage > TotalPages) CurrentPage = TotalPages;

            Users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            foreach (var user in Users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                UserRoles[user.Id] = roles.FirstOrDefault() ?? "Customer";
            }
        }
    }
}
