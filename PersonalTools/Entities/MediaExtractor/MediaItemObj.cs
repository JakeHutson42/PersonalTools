namespace PersonalTools.Entities.MediaExtractor
{
    public class MediaItemObj
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Name { get; set; } = "";
        public string Url { get; set; } = "";

        public string Type { get; set; } = "";
        public string Extension { get; set; } = "";

        public long SizeBytes { get; set; }
        public string SizeFormatted { get; set; } = "";

        // NEW
        public bool Selected { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }

        public string Dimensions =>
            Width > 0 && Height > 0 ? $"{Width} x {Height}" : "";

        public double DurationSeconds { get; set; }
        public string DurationFormatted { get; set; } = "";
    }
}