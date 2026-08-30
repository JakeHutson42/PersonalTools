using MySqlConnector;
using PersonalTools.Entities;

namespace PersonalTools.Data;

public interface ILiveWinnersData
{
    Task TouchPresence(Guid userId, CancellationToken cancellationToken = default);
    Task<LiveWinnersSummaryObj> GetSummary(CancellationToken cancellationToken = default);
}

public sealed class LiveWinnersData : ILiveWinnersData
{
    private readonly IMariaDbDataAccess _database;
    public LiveWinnersData(IMariaDbDataAccess database) => _database = database;
    public Task TouchPresence(Guid userId, CancellationToken cancellationToken = default) => _database.ExecuteSP("sp_live_winners_presence_touch", [new MySqlParameter("p_user_id", userId.ToString("D"))], cancellationToken);
    public async Task<LiveWinnersSummaryObj> GetSummary(CancellationToken cancellationToken = default)
    {
        LiveWinnersSummaryObj summary = await _database.GetDataSP("sp_live_winners_summary_get", reader => new LiveWinnersSummaryObj { LiveUserCount = reader.GetInt32("LiveUserCount") }, cancellationToken: cancellationToken) ?? new();
        summary.Winners = await _database.GetBulkDataSP("sp_live_winners_top_get", ReadWinner, cancellationToken: cancellationToken);
        summary.BattleWinners = await _database.GetBulkDataSP("sp_live_winners_case_battles_top_get", reader => new LiveCaseBattleWinnerObj
        {
            BattleId = reader.GetGuid("BattleId"), DisplayName = reader.GetString("DisplayName"), AwardedValue = reader.GetDecimal("AwardedValue"),
            CaseCount = reader.GetInt32("CaseCount"), SettledUtc = DateTime.SpecifyKind(reader.GetDateTime("SettledUtc"), DateTimeKind.Utc)
        }, cancellationToken: cancellationToken);
        return summary;
    }
    private static LiveWinnerObj ReadWinner(MySqlDataReader reader) => new()
    {
        OpeningId = reader.GetGuid("OpeningId"), DisplayName = reader.GetString("DisplayName"), ItemName = reader.GetString("ItemName"), ImageUrl = reader.GetString("ImageUrl"),
        RarityColor = reader.GetString("RarityColor"), EstimatedPrice = reader.GetDecimal("EstimatedPrice"),
        ReceivedUtc = DateTime.SpecifyKind(reader.GetDateTime("OpenedUtc"), DateTimeKind.Utc), Source = reader.GetString("Source")
    };
}
