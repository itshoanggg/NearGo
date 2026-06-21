using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NearGo.Data;
using NearGo.Models;
using System.ComponentModel.DataAnnotations;

namespace NearGo.Pages.Supermarket
{
    [Authorize(Roles = "Supermarket")]
    public class ProfileModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public ProfileModel(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public NearGo.Models.Supermarket? Supermarket { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Tên siêu thị là bắt buộc")]
            [StringLength(200)]
            public string Name { get; set; } = string.Empty;

            [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
            [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
            public string Phone { get; set; } = string.Empty;

            [Required(ErrorMessage = "Địa chỉ là bắt buộc")]
            public string Address { get; set; } = string.Empty;

            public string? TaxCode { get; set; }

            public string? Description { get; set; }

            public double? Latitude { get; set; }
            public double? Longitude { get; set; }

            public IFormFile? LogoFile { get; set; }

            public IFormFile? CoverFile { get; set; }
        }

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.SupermarketId == null) return;

            Supermarket = await _context.Supermarkets.FindAsync(user.SupermarketId.Value);
            if (Supermarket == null) return;

            Input.Name = Supermarket.Name;
            Input.Phone = Supermarket.Phone ?? "";
            Input.Address = Supermarket.Address ?? "";
            Input.TaxCode = Supermarket.TaxCode;
            Input.Description = Supermarket.Description;
            Input.Latitude = Supermarket.Latitude;
            Input.Longitude = Supermarket.Longitude;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var user = await _userManager.GetUserAsync(User);
            if (user?.SupermarketId == null) return Forbid();

            var supermarket = await _context.Supermarkets.FindAsync(user.SupermarketId.Value);
            if (supermarket == null) return Forbid();

            supermarket.Name = Input.Name;
            supermarket.Phone = Input.Phone;
            supermarket.TaxCode = Input.TaxCode;
            supermarket.Description = Input.Description;

            var addressChanged = supermarket.Address != Input.Address;
            supermarket.Address = Input.Address;

            if (Input.Latitude.HasValue || Input.Longitude.HasValue)
            {
                if (Input.Latitude.HasValue)
                    supermarket.Latitude = Input.Latitude;
                if (Input.Longitude.HasValue)
                    supermarket.Longitude = Input.Longitude;
            }
            else if (addressChanged || supermarket.Latitude == null || supermarket.Longitude == null)
            {
                var coords = await GeocodeAddressAsync(Input.Address);
                if (coords != null)
                {
                    supermarket.Latitude = coords.Value.lat;
                    supermarket.Longitude = coords.Value.lng;
                }
                else if (addressChanged)
                {
                    ModelState.AddModelError(string.Empty, "Không thể tự động lấy tọa độ từ địa chỉ. Vui lòng nhập thủ công tọa độ.");
                    return Page();
                }
            }

            if (Input.LogoFile != null && Input.LogoFile.Length > 0)
            {
                using var ms = new MemoryStream();
                await Input.LogoFile.CopyToAsync(ms);
                supermarket.LogoData = ms.ToArray();
                supermarket.LogoContentType = Input.LogoFile.ContentType;
                supermarket.LogoUrl = $"/image/supermarket-logo/{supermarket.Id}";
            }

            if (Input.CoverFile != null && Input.CoverFile.Length > 0)
            {
                using var ms = new MemoryStream();
                await Input.CoverFile.CopyToAsync(ms);
                supermarket.CoverImageData = ms.ToArray();
                supermarket.CoverImageContentType = Input.CoverFile.ContentType;
                supermarket.CoverImageUrl = $"/image/supermarket-cover/{supermarket.Id}";
            }

            user.FullName = Input.Name;
            await _userManager.UpdateAsync(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật thông tin siêu thị thành công!";
            return RedirectToPage();
        }

        private static async Task<(double lat, double lng)?> GeocodeAddressAsync(string address)
        {
            if (string.IsNullOrWhiteSpace(address)) return null;
            try
            {
                using var http = new HttpClient();
                http.DefaultRequestHeaders.Add("User-Agent", "NearGo/1.0");
                var url = $"https://nominatim.openstreetmap.org/search?format=json&limit=1&q={Uri.EscapeDataString(address + ", Việt Nam")}";
                var resp = await http.GetStringAsync(url);
                if (resp != "[]")
                {
                    using var json = System.Text.Json.JsonDocument.Parse(resp);
                    var root = json.RootElement[0];
                    var lat = double.Parse(root.GetProperty("lat").GetString()!);
                    var lng = double.Parse(root.GetProperty("lon").GetString()!);
                    return (lat, lng);
                }
            }
            catch { }
            return null;
        }

    }
}
