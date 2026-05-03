using System.Text.Json;
using PersonalTools.Entities.Skins;

namespace PersonalTools.Data.Skins
{
    public interface ICs2SkinData
    {
        Task<List<Cs2ApiSkinObj>> GetApiSkins();
        Task SaveLocalSkins(List<Cs2LocalSkinObj> skins);
    }

    public class Cs2SkinData : ICs2SkinData
    {
        private const string ApiUrl = "https://raw.githubusercontent.com/ByMykel/CSGO-API/main/public/api/en/skins_not_grouped.json";
        private const string LocalFileName = "cs2-skins.json";

        private readonly IWebHostEnvironment _environment;
        private readonly HttpClient _httpClient;

        public Cs2SkinData(IWebHostEnvironment environment, HttpClient httpClient)
        {
            _environment = environment;
            _httpClient = httpClient;
        }

        public async Task<List<Cs2ApiSkinObj>> GetApiSkins()
        {
            string json = await _httpClient.GetStringAsync(ApiUrl);

            return JsonSerializer.Deserialize<List<Cs2ApiSkinObj>>(json) ?? new List<Cs2ApiSkinObj>();
        }

        public async Task SaveLocalSkins(List<Cs2LocalSkinObj> skins)
        {
            string folderPath = Path.Combine(_environment.WebRootPath, "data");

            Directory.CreateDirectory(folderPath);

            string filePath = Path.Combine(folderPath, LocalFileName);

            string json = JsonSerializer.Serialize(skins, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(filePath, json);
        }
    }
}