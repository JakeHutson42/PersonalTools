using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PersonalTools.Classes.Skins;
using PersonalTools.Entities.Skins;

namespace PersonalTools.Pages.Skins
{
    public class IndexModel : PageModel
    {
        private readonly ISkinFuncs _skinFuncs;

        public IndexModel(ISkinFuncs skinFuncs)
        {
            _skinFuncs = skinFuncs;
        }

        public List<SkinObj> Skins { get; set; } = new();

        [BindProperty]
        public SkinObj Skin { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string SortBy { get; set; } = "currentPrice";

        [BindProperty(SupportsGet = true)]
        public string SortDirection { get; set; } = "desc";

        public decimal TotalPurchaseValue { get; set; }
        public decimal TotalCurrentValue { get; set; }
        public decimal TotalProfitLoss { get; set; }

        public string ErrorMessage { get; set; } = string.Empty;

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
            if (string.IsNullOrWhiteSpace(Skin.Name))
            {
                TempData["ErrorMessage"] = "Please select a skin.";
                return RedirectToPage();
            }

            await _skinFuncs.CreateSkin(Skin);

            TempData["SuccessMessage"] = "Skin added successfully.";

            return RedirectToPage();
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
                TempData["ErrorMessage"] = "Please select a skin.";
                return RedirectToPage();
            }

            await _skinFuncs.UpdateSkin(Skin);

            TempData["SuccessMessage"] = "Skin updated successfully.";

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

        public async Task<IActionResult> OnPostRefreshSkinData()
        {
            try
            {
                int count = await _skinFuncs.RefreshCs2SkinData();

                if (count <= 0)
                {
                    TempData["ErrorMessage"] = "No CS2 skin data was loaded.";
                    return RedirectToPage();
                }

                TempData["SuccessMessage"] = $"CS2 skin data refreshed. {count} skins loaded.";

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Could not refresh CS2 skin data: {ex.Message}";
                return RedirectToPage();
            }
        }
    }
}