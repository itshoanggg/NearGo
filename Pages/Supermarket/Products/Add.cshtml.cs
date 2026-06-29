using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NearGo.Data;
using NearGo.Hubs;
using NearGo.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace NearGo.Pages.Supermarket.Products
{
    [Authorize(Roles = "Supermarket")]
    public class AddModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IHubContext<NotificationHub> _hubContext;

        public AddModel(ApplicationDbContext context, UserManager<AppUser> userManager,
            IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _userManager = userManager;
            _hubContext = hubContext;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();
        public List<Category> Categories { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "Tên sản phẩm là bắt buộc")]
            [StringLength(200)]
            public string Name { get; set; } = string.Empty;

            public string? Description { get; set; }

            [Required(ErrorMessage = "Vui lòng chọn danh mục")]
            public int CategoryId { get; set; }

            [Required(ErrorMessage = "Giá gốc là bắt buộc")]
            [Range(1000, 100000000)]
            public decimal OriginalPrice { get; set; }

            [Required(ErrorMessage = "Giá bán là bắt buộc")]
            [Range(0, 100000000)]
            public decimal DiscountedPrice { get; set; }

            [Required(ErrorMessage = "Số lượng là bắt buộc")]
            [Range(0, 100000)]
            public int StockQuantity { get; set; }

            [Required(ErrorMessage = "Hạn sử dụng là bắt buộc")]
            public DateTime ExpiryDate { get; set; } = DateTime.UtcNow.AddDays(7);

            public string DealType { get; set; } = "giam-gia";
            public DateTime? DiscountEndDate { get; set; }

            public string? ImageUrl { get; set; }
            public IFormFile? ImageFile { get; set; }
            public string? Unit { get; set; } = "cái";
            public string? Origin { get; set; }
            public string? Tags { get; set; }
        }

        private async Task<byte[]?> ReadFileBytesAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0) return null;
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            return ms.ToArray();
        }

        public async Task OnGetAsync()
        {
            Categories = await _context.Categories.Where(c => c.IsActive).OrderBy(c => c.SortOrder).ToListAsync();
        }

        [RequestSizeLimit(30 * 1024 * 1024)]
        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    Categories = await _context.Categories.Where(c => c.IsActive).OrderBy(c => c.SortOrder).ToListAsync();
                    return Page();
                }

                var user = await _userManager.GetUserAsync(User);
                if (user?.SupermarketId == null) return Forbid();

                var supermarket = await _context.Supermarkets.FindAsync(user.SupermarketId.Value);
                if (supermarket == null) return Forbid();

                var isGiamSau = Input.DealType == "giam-sau";
                var discountedPrice = isGiamSau ? Input.OriginalPrice : (Input.DiscountedPrice > 0 ? Input.DiscountedPrice : Input.OriginalPrice);
                var discountPct = isGiamSau ? 0 : (Input.OriginalPrice > 0 ? (double)((Input.OriginalPrice - discountedPrice) / Input.OriginalPrice * 100) : 0);

                var imageBytes = await ReadFileBytesAsync(Input.ImageFile);

                var slug = string.IsNullOrWhiteSpace(Input.Name) ? "san-pham" : Input.Name.ToLower().Replace(" ", "-").Replace(",", "").Replace(".", "").Replace("&", "va").Replace("/", "-");
                slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9-]", "") + "-" + Guid.NewGuid().ToString().Substring(0, 6);

                var product = new NearGo.Models.Product
                {
                    Name = Input.Name,
                    Slug = slug,
                    Description = Input.Description,
                    CategoryId = Input.CategoryId,
                    SupermarketId = user.SupermarketId.Value,
                    OriginalPrice = Input.OriginalPrice,
                    DiscountedPrice = discountedPrice,
                    DiscountPercentage = Math.Round(discountPct, 1),
                    StockQuantity = Input.StockQuantity,
                    ExpiryDate = Input.ExpiryDate,
                    DiscountEndDate = isGiamSau ? null : Input.DiscountEndDate,
                    ImageUrl = Input.ImageUrl ?? "https://images.unsplash.com/photo-1542838132-92c53300491e?w=400",
                    ImageData = imageBytes,
                    ImageContentType = Input.ImageFile?.ContentType,
                    Unit = Input.Unit ?? "cái",
                    Origin = Input.Origin,
                    Tags = Input.Tags,
                    IsActive = true,
                    DealScore = Math.Round(discountPct * 100, 1)
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                if (product.ImageData != null)
                {
                    product.ImageUrl = $"/image/product/{product.Id}";
                    await _context.SaveChangesAsync();
                }

                var supermarketName = supermarket.Name;
                var ownerId = user.Id;

                var followerIds = await _context.Database
                    .SqlQuery<string>($"SELECT UserId FROM UserFollowedSupermarkets WHERE SupermarketId = {user.SupermarketId.Value}")
                    .ToListAsync();

                if (followerIds.Count > 0)
                {
                    var productUrl = $"/products/detail?id={product.Id}";
                    var title = $"Sản phẩm mới từ {supermarketName}";
                    var message = $"{product.Name} - chỉ {product.DiscountedPrice:N0}đ";

                    foreach (var fid in followerIds)
                    {
                        if (fid == ownerId) continue;

                        _context.Notifications.Add(new Notification
                        {
                            UserId = fid,
                            Title = title,
                            Message = message,
                            Type = "NewProduct",
                            RelatedUrl = productUrl,
                            ImageUrl = product.ImageUrl,
                            IsRead = false,
                            CreatedAt = DateTime.UtcNow
                        });
                    }

                    await _context.SaveChangesAsync();

                    foreach (var fid in followerIds)
                    {
                        if (fid == ownerId) continue;

                        try
                        {
                            await _hubContext.Clients.Group($"user_{fid}")
                                .SendAsync("ReceiveNotification", title, message, productUrl);
                        }
                        catch { }
                    }
                }

                TempData["Success"] = "Thêm sản phẩm thành công!";
                return RedirectToPage("/Supermarket/Products");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra khi thêm sản phẩm: " + ex.Message;
                Categories = await _context.Categories.Where(c => c.IsActive).OrderBy(c => c.SortOrder).ToListAsync();
                return Page();
            }
        }
    }
}
