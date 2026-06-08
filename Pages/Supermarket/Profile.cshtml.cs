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
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var user = await _userManager.GetUserAsync(User);
            if (user?.SupermarketId == null) return Forbid();

            var supermarket = await _context.Supermarkets.FindAsync(user.SupermarketId.Value);
            if (supermarket == null) return Forbid();

            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "supermarkets");
            Directory.CreateDirectory(uploadsDir);

            supermarket.Name = Input.Name;
            supermarket.Phone = Input.Phone;
            supermarket.Address = Input.Address;
            supermarket.TaxCode = Input.TaxCode;
            supermarket.Description = Input.Description;

            var logoUrl = await SaveImageAsync(Input.LogoFile, supermarket.LogoUrl, uploadsDir);
            if (logoUrl != null) supermarket.LogoUrl = logoUrl;

            var coverUrl = await SaveImageAsync(Input.CoverFile, supermarket.CoverImageUrl, uploadsDir);
            if (coverUrl != null) supermarket.CoverImageUrl = coverUrl;

            user.FullName = Input.Name;
            await _userManager.UpdateAsync(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật thông tin siêu thị thành công!";
            return RedirectToPage();
        }

        private static async Task<string?> SaveImageAsync(IFormFile? file, string? existingUrl, string uploadsDir)
        {
            if (file != null && file.Length > 0)
            {
                var ext = Path.GetExtension(file.FileName);
                var fileName = $"sm_{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploadsDir, fileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);
                return $"/uploads/supermarkets/{fileName}";
            }
            return null;
        }
    }
}
