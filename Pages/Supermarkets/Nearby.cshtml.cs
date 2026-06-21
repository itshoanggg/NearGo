using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NearGo.Data;

namespace NearGo.Pages.Supermarkets
{
    public class NearbyModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public NearbyModel(ApplicationDbContext context) => _context = context;

        public JsonResult OnGetSupermarkets()
        {
            var supermarkets = _context.Supermarkets
                .Where(s => s.IsActive && s.Latitude != null && s.Longitude != null)
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.Slug,
                    s.Address,
                    s.LogoUrl,
                    s.Rating,
                    s.ReviewCount,
                    lat = s.Latitude,
                    lng = s.Longitude
                })
                .ToList();

            return new JsonResult(supermarkets);
        }
    }
}
