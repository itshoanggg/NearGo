using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NearGo.Data;
using NearGo.Models;
using NearGo.Services;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace NearGo.Pages.Auth
{
    public class RegisterSupermarketModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public RegisterSupermarketModel(
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            IEmailService emailService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _emailService = emailService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "Tên siêu thị là bắt buộc")]
            [StringLength(200, ErrorMessage = "Tên siêu thị không được quá 200 ký tự")]
            public string SupermarketName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Email là bắt buộc")]
            [EmailAddress(ErrorMessage = "Email không hợp lệ")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
            [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
            public string Phone { get; set; } = string.Empty;

            [Required(ErrorMessage = "Địa chỉ là bắt buộc")]
            public string Address { get; set; } = string.Empty;

            public string? TaxCode { get; set; }

            public string? Description { get; set; }

            public IFormFile? LogoFile { get; set; }

            public IFormFile? CoverFile { get; set; }

            [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
            [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6 ký tự")]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [Required(ErrorMessage = "Xác nhận mật khẩu là bắt buộc")]
            [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
            [DataType(DataType.Password)]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var existingUser = await _userManager.FindByEmailAsync(Input.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError(string.Empty, "Email này đã được đăng ký");
                return Page();
            }

            var user = new AppUser
            {
                UserName = Input.Email,
                Email = Input.Email,
                FullName = Input.SupermarketName,
                PhoneNumber = Input.Phone,
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user, Input.Password);
            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return Page();
            }

            if (!await _roleManager.RoleExistsAsync("Supermarket"))
                await _roleManager.CreateAsync(new IdentityRole("Supermarket"));

            await _userManager.AddToRoleAsync(user, "Supermarket");

            var slug = GenerateSlug(Input.SupermarketName);

            byte[]? logoBytes = null, coverBytes = null;
            string? logoContentType = null, coverContentType = null;

            if (Input.LogoFile != null && Input.LogoFile.Length > 0)
            {
                using var ms = new MemoryStream();
                await Input.LogoFile.CopyToAsync(ms);
                logoBytes = ms.ToArray();
                logoContentType = Input.LogoFile.ContentType;
            }

            if (Input.CoverFile != null && Input.CoverFile.Length > 0)
            {
                using var ms = new MemoryStream();
                await Input.CoverFile.CopyToAsync(ms);
                coverBytes = ms.ToArray();
                coverContentType = Input.CoverFile.ContentType;
            }

            var supermarket = new NearGo.Models.Supermarket
            {
                Name = Input.SupermarketName,
                Slug = slug,
                Email = Input.Email,
                Phone = Input.Phone,
                Address = Input.Address,
                TaxCode = Input.TaxCode,
                Description = Input.Description,
                LogoData = logoBytes,
                LogoContentType = logoContentType,
                CoverImageData = coverBytes,
                CoverImageContentType = coverContentType,
                IsActive = true,
                IsVerified = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Supermarkets.Add(supermarket);
            await _context.SaveChangesAsync();

            if (supermarket.LogoData != null)
            {
                supermarket.LogoUrl = $"/image/supermarket-logo/{supermarket.Id}";
            }
            if (supermarket.CoverImageData != null)
            {
                supermarket.CoverImageUrl = $"/image/supermarket-cover/{supermarket.Id}";
            }
            await _context.SaveChangesAsync();

            user.SupermarketId = supermarket.Id;
            await _userManager.UpdateAsync(user);

            await SendNotificationEmail(Input.Email, Input.SupermarketName);

            TempData["Success"] = "Đăng ký siêu thị thành công!";
            return RedirectToPage("/Auth/Login");
        }

        private string GenerateSlug(string name)
        {
            var slug = name.ToLower()
                .Replace(" ", "-")
                .Replace(",", "")
                .Replace(".", "")
                .Replace("&", "va")
                .Replace("/", "-");
            slug = Regex.Replace(slug, @"[^a-z0-9-]", "");
            slug += "-" + Guid.NewGuid().ToString().Substring(0, 6);
            return slug;
        }

        private async Task SendNotificationEmail(string email, string supermarketName)
        {
            var subject = "Đăng ký tài khoản siêu thị NearGo thành công";
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <div style='background: #0284c7; color: white; padding: 24px; text-align: center; border-radius: 12px 12px 0 0;'>
                        <h2 style='margin: 0;'>NearGo</h2>
                    </div>
                    <div style='padding: 24px; border: 1px solid #e5e7eb; border-top: none; border-radius: 0 0 12px 12px;'>
                        <h3>Xin chào {supermarketName},</h3>
                        <p>Tài khoản siêu thị của bạn đã được tạo thành công trên NearGo.</p>
                        <p><strong>Thông tin đăng nhập:</strong></p>
                        <p>Email: {email}</p>
                        <a href='https://localhost:5001/auth/login' style='display: inline-block; background: #0284c7; color: white; padding: 12px 24px; border-radius: 8px; text-decoration: none; margin-top: 12px;'>Đăng nhập ngay</a>
                    </div>
                </div>";

            await _emailService.SendEmailAsync(email, subject, body);
        }
    }
}
