using PersonalTools.Data.Local;
using PersonalTools.Entities.Skins;

namespace PersonalTools.Classes.Skins
{
    public interface ISkinFuncs
    {
        Task<List<SkinObj>> GetSkins();
        Task CreateSkin(SkinObj skin);
        Task UpdateSkin(SkinObj skin);
        Task DeleteSkin(string skinId);
    }

    public class SkinFuncs : ISkinFuncs
    {
        private const string FileName = "skins.json";

        private readonly ILocalJsonData _localJsonData;

        public SkinFuncs(ILocalJsonData localJsonData)
        {
            _localJsonData = localJsonData;
        }

        public async Task<List<SkinObj>> GetSkins()
        {
            List<SkinObj> skins = await _localJsonData.LoadList<SkinObj>(FileName);

            return skins
                .OrderByDescending(x => x.Updated)
                .ToList();
        }

        public async Task CreateSkin(SkinObj skin)
        {
            List<SkinObj> skins = await _localJsonData.LoadList<SkinObj>(FileName);

            skin.SkinId = Guid.NewGuid().ToString();

            skin.Name = skin.Name?.Trim() ?? string.Empty;
            skin.Weapon = skin.Weapon?.Trim() ?? string.Empty;
            skin.Exterior = skin.Exterior?.Trim() ?? string.Empty;
            skin.MarketHashName = skin.MarketHashName?.Trim() ?? string.Empty;
            skin.ExternalImageUrl = skin.ExternalImageUrl?.Trim() ?? string.Empty;
            skin.Notes = skin.Notes?.Trim() ?? string.Empty;
            skin.ImagePath = skin.ImagePath?.Trim() ?? string.Empty;

            skin.Created = DateTime.Now;
            skin.Updated = DateTime.Now;

            skins.Add(skin);

            await _localJsonData.SaveList(FileName, skins);
        }

        public async Task UpdateSkin(SkinObj skin)
        {
            List<SkinObj> skins = await _localJsonData.LoadList<SkinObj>(FileName);

            SkinObj? existingSkin = skins.FirstOrDefault(x => x.SkinId == skin.SkinId);

            if (existingSkin == null)
            {
                return;
            }

            existingSkin.Name = skin.Name?.Trim() ?? string.Empty;
            existingSkin.Weapon = skin.Weapon?.Trim() ?? string.Empty;
            existingSkin.Exterior = skin.Exterior?.Trim() ?? string.Empty;
            existingSkin.MarketHashName = skin.MarketHashName?.Trim() ?? string.Empty;
            existingSkin.ExternalImageUrl = skin.ExternalImageUrl?.Trim() ?? string.Empty;
            existingSkin.PurchasePrice = skin.PurchasePrice;
            existingSkin.CurrentPrice = skin.CurrentPrice;
            existingSkin.PurchaseDate = skin.PurchaseDate;
            existingSkin.Notes = skin.Notes?.Trim() ?? string.Empty;
            existingSkin.ImagePath = skin.ImagePath?.Trim() ?? string.Empty;
            existingSkin.Updated = DateTime.Now;

            await _localJsonData.SaveList(FileName, skins);
        }

        public async Task DeleteSkin(string skinId)
        {
            List<SkinObj> skins = await _localJsonData.LoadList<SkinObj>(FileName);

            SkinObj? skin = skins.FirstOrDefault(x => x.SkinId == skinId);

            if (skin == null)
            {
                return;
            }

            skins.Remove(skin);

            await _localJsonData.SaveList(FileName, skins);
        }
    }
}