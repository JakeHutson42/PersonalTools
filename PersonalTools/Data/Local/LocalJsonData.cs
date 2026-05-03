using System.Text.Json;

namespace PersonalTools.Data.Local
{
    public interface ILocalJsonData
    {
        Task<List<T>> LoadList<T>(string fileName);
        Task SaveList<T>(string fileName, List<T> items);
    }

    public class LocalJsonData : ILocalJsonData
    {
        private readonly IWebHostEnvironment _environment;

        public LocalJsonData(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<List<T>> LoadList<T>(string fileName)
        {
            string folderPath = Path.Combine(_environment.ContentRootPath, "App_Data");
            string filePath = Path.Combine(folderPath, fileName);

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            if (!File.Exists(filePath))
            {
                return new List<T>();
            }

            string json = await File.ReadAllTextAsync(filePath);

            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<T>();
            }

            return JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();
        }

        public async Task SaveList<T>(string fileName, List<T> items)
        {
            string folderPath = Path.Combine(_environment.ContentRootPath, "App_Data");
            string filePath = Path.Combine(folderPath, fileName);

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string json = JsonSerializer.Serialize(items, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(filePath, json);
        }
    }
}