using PersonalTools.Data;
using PersonalTools.Entities;

namespace PersonalTools.Classes;

public interface ILiveWinnersFuncs
{
    Task<LiveWinnersSummaryObj> Get(Guid userId, bool isAdministrator, CancellationToken cancellationToken = default);
    Task SetVisibility(string visibility, CancellationToken cancellationToken = default);
}

public sealed class LiveWinnersFuncs : ILiveWinnersFuncs
{
    private readonly ILiveWinnersData _data;
    public LiveWinnersFuncs(ILiveWinnersData data) => _data = data;
    public async Task<LiveWinnersSummaryObj> Get(Guid userId, bool isAdministrator, CancellationToken cancellationToken = default)
    {
        await _data.TouchPresence(userId, cancellationToken);
        LiveWinnersSummaryObj summary = await _data.GetSummary(cancellationToken);
        if (summary.Visibility == "admins" && !isAdministrator) summary.Winners = [];
        return summary;
    }
    public Task SetVisibility(string visibility, CancellationToken cancellationToken = default)
    {
        if (visibility is not ("users" or "admins")) throw new InvalidOperationException("Choose whether the winners dock is visible to users or administrators.");
        return _data.SetVisibility(visibility, cancellationToken);
    }
}
