namespace PersonalTools.Entities;

public sealed class LiveWinnersSummaryObj
{
    public int LiveUserCount { get; set; }
    public List<LiveWinnerObj> Winners { get; set; } = [];
}

public sealed class LiveWinnerObj
{
    public Guid OpeningId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string RarityColor { get; set; } = "#e4ae39";
    public decimal EstimatedPrice { get; set; }
    public DateTime ReceivedUtc { get; set; }
    public string Source { get; set; } = "Case opening";
}
