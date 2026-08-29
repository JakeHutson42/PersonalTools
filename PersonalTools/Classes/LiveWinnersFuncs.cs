using PersonalTools.Data;
using PersonalTools.Entities;

namespace PersonalTools.Classes;

public interface ILiveWinnersFuncs
{
    Task<LiveWinnersSummaryObj> Get(Guid userId, CancellationToken cancellationToken = default);
}

public sealed class LiveWinnersFuncs : ILiveWinnersFuncs
{
    private readonly ILiveWinnersData _data;
    public LiveWinnersFuncs(ILiveWinnersData data) => _data = data;
    public async Task<LiveWinnersSummaryObj> Get(Guid userId, CancellationToken cancellationToken = default)
    {
        await _data.TouchPresence(userId, cancellationToken);
        return await _data.GetSummary(cancellationToken);
    }
}
