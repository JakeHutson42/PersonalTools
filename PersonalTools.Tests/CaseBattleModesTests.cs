using PersonalTools.Entities.CaseBattles;

namespace PersonalTools.Tests;

public sealed class CaseBattleModesTests
{
    [Theory]
    [InlineData(CaseBattleModes.Duel, 2)]
    [InlineData(CaseBattleModes.FreeForAll3, 3)]
    [InlineData(CaseBattleModes.FreeForAll4, 4)]
    [InlineData(CaseBattleModes.Teams2v2, 4)]
    public void PlayerCount_ReturnsTheConfiguredSeatCount(string mode, int expected)
    {
        Assert.Equal(expected, CaseBattleModes.PlayerCount(mode));
    }

    [Fact]
    public void InitialRollout_EnablesOnlyTheSettledDuelMode()
    {
        Assert.True(CaseBattleModes.IsEnabled(CaseBattleModes.Duel));
        Assert.False(CaseBattleModes.IsEnabled(CaseBattleModes.FreeForAll3));
        Assert.False(CaseBattleModes.IsEnabled(CaseBattleModes.Teams2v2));
    }
}
