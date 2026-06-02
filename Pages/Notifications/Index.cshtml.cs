using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NearGo.Data;
using NearGo.Helpers;
using NearGo.Models;

namespace NearGo.Pages.Notifications
{
    [Authorize]
    [IgnoreAntiforgeryToken]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public IndexModel(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<JsonResult> OnGetUnreadCountAsync()
        {
            var userId = _userManager.GetUserId(User)!;
            var count = await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
            return new JsonResult(new { count });
        }

        public async Task<JsonResult> OnGetListAsync()
        {
            var userId = _userManager.GetUserId(User)!;
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(20)
                .Select(n => new
                {
                    n.Id,
                    n.Title,
                    n.Message,
                    n.Type,
                    n.RelatedUrl,
                    n.ImageUrl,
                    n.IsRead,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();

            var result = notifications.Select(n => new
            {
                n.Id,
                n.Title,
                n.Message,
                n.Type,
                n.RelatedUrl,
                n.ImageUrl,
                n.IsRead,
                CreatedAt = DateTimeHelper.ToVietnamTime(n.CreatedAt)
            });

            return new JsonResult(result);
        }

        public async Task<JsonResult> OnPostMarkReadAsync(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
            return new JsonResult(new { success = true });
        }

        public async Task<JsonResult> OnPostMarkAllReadAsync()
        {
            var userId = _userManager.GetUserId(User)!;
            await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
            return new JsonResult(new { success = true });
        }
    }
}
