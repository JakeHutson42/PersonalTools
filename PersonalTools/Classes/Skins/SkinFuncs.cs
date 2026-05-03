using PersonalTools.Data.Local;
using PersonalTools.Data.Skins;
using PersonalTools.Entities.Skins;

namespace PersonalTools.Classes.Skins
{
    public interface ISkinFuncs
    {
        Task<List<SkinObj>> GetSkins();
        Task CreateSkin(SkinObj skin);
        Task UpdateSkin(SkinObj skin);
        Task DeleteSkin(string skinId);
        Task<int> RefreshCs2SkinData();
    }

    public class SkinFuncs : ISkinFuncs
    {
        private const string FileName = "skins.json";

        private readonly ILocalJsonData _localJsonData;
        private readonly ICs2SkinData _cs2SkinData;

        public SkinFuncs(ILocalJsonData localJsonData, ICs2SkinData cs2SkinData)
        {
            _localJsonData = localJsonData;
            _cs2SkinData = cs2SkinData;
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

        public async Task<int> RefreshCs2SkinData()
        {
            List<Cs2ApiSkinObj> apiSkins = await _cs2SkinData.GetApiSkins();

            List<Cs2LocalSkinObj> localSkins = apiSkins
                .Where(x => !string.IsNullOrWhiteSpace(x.MarketHashName))
                .Select(x => new Cs2LocalSkinObj
                {
                    Name = x.Name,
                    Weapon = x.Weapon?.Name ?? string.Empty,
                    Exterior = x.Wear?.Name ?? string.Empty,
                    MarketHashName = x.MarketHashName,
                    Image = x.Image
                })
                .OrderBy(x => x.MarketHashName)
                .ToList();

            await _cs2SkinData.SaveLocalSkins(localSkins);

            return localSkins.Count;
        }
    }
}