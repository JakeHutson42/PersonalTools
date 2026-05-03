using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PersonalTools.Classes.Skins;
using PersonalTools.Entities.Skins;
using Microsoft.AspNetCore.Hosting;

namespace PersonalTools.Pages.Skins
{
    public class IndexModel : PageModel
    {
        private readonly ISkinFuncs _skinFuncs;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            ISkinFuncs skinFuncs,
            IWebHostEnvironment environment,
            ILogger<IndexModel> logger)
        {
            _skinFuncs = skinFuncs;
            _environment = environment;
            _logger = logger;
        }

        public List<SkinObj> Skins { get; set; } = new();

        [BindProperty]
        public SkinObj Skin { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string SortBy { get; set; } = "currentPrice";

        [BindProperty(SupportsGet = true)]
        public string SortDirection { get; set; } = "desc";

        [BindProperty]
        public string DeleteImageSkinId { get; set; } = string.Empty;


        //[BindProperty]
        //public IFormFile? SkinImage { get; set; }

        [BindProperty]
        public string ExistingImagePath { get; set; } = string.Empty;

        public decimal TotalPurchaseValue { get; set; }
        public decimal TotalCurrentValue { get; set; }
        public decimal TotalProfitLoss { get; set; }

        public string ImageMessage { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public string UploadErrorMessage { get; set; } = string.Empty;

        public async Task OnGet()
        {
            Skins = await _skinFuncs.GetSkins();

            Skins = SortBy switch
            {
                "name" => SortDirection == "asc"
                    ? Skins.OrderBy(x => x.Name).ToList()
                    : Skins.OrderByDescending(x => x.Name).ToList(),

                "purchasePrice" => SortDirection == "asc"
                    ? Skins.OrderBy(x => x.PurchasePrice).ToList()
                    : Skins.OrderByDescending(x => x.PurchasePrice).ToList(),

                "currentPrice" => SortDirection == "asc"
                    ? Skins.OrderBy(x => x.CurrentPrice ?? x.PurchasePrice).ToList()
                    : Skins.OrderByDescending(x => x.CurrentPrice ?? x.PurchasePrice).ToList(),

                _ => Skins.OrderByDescending(x => x.CurrentPrice ?? x.PurchasePrice).ToList()
            };

            TotalPurchaseValue = Skins.Sum(x => x.PurchasePrice);
            TotalCurrentValue = Skins.Sum(x => x.CurrentPrice ?? x.PurchasePrice);
            TotalProfitLoss = TotalCurrentValue - TotalPurchaseValue;
        }

        public async Task<IActionResult> OnPostCreate()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Skin.Name))
                {
                    TempData["ErrorMessage"] = "Please select or enter the skin name.";
                    return RedirectToPage();
                }

                if (string.IsNullOrWhiteSpace(Skin.Weapon))
                {
                    TempData["ErrorMessage"] = "Please enter the weapon.";
                    return RedirectToPage();
                }

                IFormFile? uploadedImage = GetUploadedSkinImage();
                string imagePath = await SaveSkinImage(uploadedImage);

                if (TempData["ErrorMessage"] != null)
                {
                    return RedirectToPage();
                }

                Skin.ImagePath = imagePath;

                await _skinFuncs.CreateSkin(Skin);

                TempData["SuccessMessage"] = !string.IsNullOrWhiteSpace(imagePath)
                    ? "Skin added and image uploaded successfully."
                    : "Skin added successfully.";

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Create skin failed");
                TempData["ErrorMessage"] = $"Create failed: {ex.Message}";
                return RedirectToPage();
            }
        }

        private IFormFile? GetUploadedSkinImage()
        {
            if (!Request.HasFormContentType)
            {
                return null;
            }

            return Request.Form.Files["SkinImage"];
        }

        public async Task<IActionResult> OnPostEdit()
        {
            if (string.IsNullOrWhiteSpace(Skin.SkinId))
            {
                TempData["ErrorMessage"] = "Could not find the skin to update.";
                return RedirectToPage();
            }

            if (string.IsNullOrWhiteSpace(Skin.Name))
            {
                TempData["ErrorMessage"] = "Please select or enter the skin name.";
                return RedirectToPage();
            }

            if (string.IsNullOrWhiteSpace(Skin.Weapon))
            {
                TempData["ErrorMessage"] = "Please enter the weapon.";
                return RedirectToPage();
            }

            IFormFile? uploadedImage = GetUploadedSkinImage();
            string newImagePath = await SaveSkinImage(uploadedImage);
            if (TempData["ErrorMessage"] != null)
            {
                return RedirectToPage();
            }

            Skin.ImagePath = !string.IsNullOrWhiteSpace(newImagePath)
                ? newImagePath
                : ExistingImagePath;

            await _skinFuncs.UpdateSkin(Skin);

            TempData["SuccessMessage"] = !string.IsNullOrWhiteSpace(newImagePath)
                ? "Skin updated and image uploaded successfully."
                : "Skin updated successfully.";

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDelete(string skinId)
        {
            if (!string.IsNullOrWhiteSpace(skinId))
            {
                await _skinFuncs.DeleteSkin(skinId);
                TempData["SuccessMessage"] = "Skin deleted successfully.";
            }

            return RedirectToPage();
        }

        private async Task<string> SaveSkinImage(IFormFile? image)
        {
            if (image == null || image.Length == 0)
            {
                return string.Empty;
            }

            string[] allowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
            string fileNameOnly = Path.GetFileName(image.FileName);
            string extension = Path.GetExtension(fileNameOnly).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                TempData["ErrorMessage"] = "Only JPG, PNG and WEBP images are allowed.";
                return string.Empty;
            }

            if (image.Length > 5 * 1024 * 1024)
            {
                TempData["ErrorMessage"] = "Image upload failed. The file must be 5MB or smaller.";
                return string.Empty;
            }

            string uploadFolder = Path.Combine(_environment.WebRootPath, "uploads", "skins");
            Directory.CreateDirectory(uploadFolder);

            string fileName = $"{Guid.NewGuid()}{extension}";
            string filePath = Path.Combine(uploadFolder, fileName);

            await using FileStream stream = new FileStream(filePath, FileMode.Create);
            await image.CopyToAsync(stream);

            return $"/uploads/skins/{fileName}";
        }

        public async Task<IActionResult> OnPostDeleteImage()
        {
            if (string.IsNullOrWhiteSpace(DeleteImageSkinId))
            {
                TempData["ErrorMessage"] = "Could not find the skin image to delete.";
                return RedirectToPage();
            }

            List<SkinObj> skins = await _skinFuncs.GetSkins();
            SkinObj? skin = skins.FirstOrDefault(x => x.SkinId == DeleteImageSkinId);

            if (skin == null || string.IsNullOrWhiteSpace(skin.ImagePath))
            {
                TempData["ErrorMessage"] = "No image was found for this skin.";
                return RedirectToPage();
            }

            string relativePath = skin.ImagePath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());
            string fullPath = Path.Combine(_environment.WebRootPath, relativePath);

            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }

            skin.ImagePath = string.Empty;

            await _skinFuncs.UpdateSkin(skin);

            TempData["SuccessMessage"] = "Skin image deleted successfully.";

            return RedirectToPage();
        }


        public async Task<IActionResult> OnPostUploadImage(string skinId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(skinId))
                {
                    TempData["ErrorMessage"] = "Could not find the skin to update.";
                    return RedirectToPage();
                }

                IFormFile? image = Request.Form.Files["image"];

                string imagePath = await SaveSkinImage(image);

                if (TempData["ErrorMessage"] != null)
                {
                    return RedirectToPage();
                }

                if (string.IsNullOrWhiteSpace(imagePath))
                {
                    TempData["ErrorMessage"] = "Please choose an image to upload.";
                    return RedirectToPage();
                }

                List<SkinObj> skins = await _skinFuncs.GetSkins();
                SkinObj? skin = skins.FirstOrDefault(x => x.SkinId == skinId);

                if (skin == null)
                {
                    TempData["ErrorMessage"] = "Skin not found.";
                    return RedirectToPage();
                }

                skin.ImagePath = imagePath;

                await _skinFuncs.UpdateSkin(skin);

                TempData["SuccessMessage"] = "Image uploaded successfully.";

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Image upload failed: {ex.Message}";
                return RedirectToPage();
            }
        }
    }
}