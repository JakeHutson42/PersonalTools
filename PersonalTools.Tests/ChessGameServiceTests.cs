using PersonalTools.Classes.ChessGames;
using PersonalTools.Data.ChessGames;

namespace PersonalTools.Tests;

public sealed class ChessGameServiceTests
{
    [Fact]
    public async Task ComputerStrengthAcceptsHundredPointSteps()
    {
        var store = new FakeData("ai"); var service = new ChessGameService(store);
        await Assert.ThrowsAsync<ArgumentException>(() => service.Create(store.White, "ai", 150, default));
        await Assert.ThrowsAsync<ArgumentException>(() => service.Create(store.White, "ai", 2500, default));
        var low = await service.Create(store.White, "ai", 100, default);
        Assert.Equal(100, low.Difficulty);
        var high = await service.Create(store.White, "ai", 2400, default);
        Assert.Equal(2400, high.Difficulty);
    }

    [Fact]
    public async Task LegalMoveAdvancesSavedPositionAndVersion()
    {
        var store = new FakeData("pvp"); var service = new ChessGameService(store);
        var result = await service.Move(store.Game.GameId, store.White, "e2", "e4", null, 0, default);
        Assert.Equal(1, result.Version);
        Assert.StartsWith("1. e4", result.Pgn);
        Assert.Contains("4P3", result.Fen);
    }

    [Fact]
    public async Task CapturesStoreTheTakenPieceForMoveHistory()
    {
        var store = new FakeData("pvp"); var service = new ChessGameService(store); var id = store.Game.GameId;
        await service.Move(id, store.White, "e2", "e4", null, 0, 0, default);
        await service.Move(id, store.Black, "d7", "d5", null, 1, 1, default);
        var game = await service.Move(id, store.White, "e4", "d5", null, 2, 2, default);
        Assert.Equal("bp", game.Moves!.Last().Captured);
    }

    [Fact]
    public async Task RejectsIllegalWrongTurnAndStaleMoves()
    {
        var store = new FakeData("pvp"); var service = new ChessGameService(store);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.Move(store.Game.GameId, store.Black, "e2", "e4", null, 0, default));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.Move(store.Game.GameId, store.White, "e2", "e5", null, 0, default));
        await service.Move(store.Game.GameId, store.White, "e2", "e4", null, 0, default);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.Move(store.Game.GameId, store.White, "d2", "d4", null, 0, default));
    }

    [Fact]
    public async Task DetectsCheckmateAfterReloadingMoveHistory()
    {
        var store = new FakeData("pvp"); var service = new ChessGameService(store); var id = store.Game.GameId;
        await service.Move(id, store.White, "f2", "f3", null, 0, default);
        await service.Move(id, store.Black, "e7", "e5", null, 1, default);
        await service.Move(id, store.White, "g2", "g4", null, 2, default);
        var result = await service.Move(id, store.Black, "d8", "h4", null, 3, default);
        Assert.Equal("finished", result.Status);
        Assert.Equal("black", result.Result);
        Assert.EndsWith("#", result.Moves!.Last().San);
    }

    [Fact]
    public async Task AiTurnCanBeSubmittedByOwningPlayer()
    {
        var store = new FakeData("ai"); var service = new ChessGameService(store); var id = store.Game.GameId;
        await service.Move(id, store.White, "e2", "e4", null, 0, default);
        var result = await service.Move(id, store.White, "e7", "e5", null, 1, default);
        Assert.Equal(2, result.Version);
    }

    [Fact]
    public async Task LegalDestinationsBelongOnlyToPlayerWhoseTurnItIs()
    {
        var store = new FakeData("pvp"); var service = new ChessGameService(store);
        var whiteView = await service.Get(store.Game.GameId, store.White, default);
        var blackView = await service.Get(store.Game.GameId, store.Black, default);
        Assert.Contains(whiteView.LegalMoves!, move => move.From == "e2" && move.To == "e4");
        Assert.Empty(blackView.LegalMoves!);

        await service.Move(store.Game.GameId, store.White, "e2", "e4", null, 0, default);
        whiteView = await service.Get(store.Game.GameId, store.White, default);
        blackView = await service.Get(store.Game.GameId, store.Black, default);
        Assert.Empty(whiteView.LegalMoves!);
        Assert.Contains(blackView.LegalMoves!, move => move.From == "e7" && move.To == "e5");
    }

    [Fact]
    public async Task AiUndoCanRewindEveryTurnAndRejectsStaleRevision()
    {
        var store = new FakeData("ai"); var service = new ChessGameService(store); var id = store.Game.GameId;
        await service.Move(id, store.White, "e2", "e4", null, 0, 0, default);
        await service.Move(id, store.White, "e7", "e5", null, 1, 1, default);
        await service.Move(id, store.White, "g1", "f3", null, 2, 2, default);
        await service.Move(id, store.White, "b8", "c6", null, 3, 3, default);
        var first = await service.Undo(id, store.White, 4, 4, default);
        Assert.Equal(2, first.Version);
        Assert.Equal(5, first.Revision);
        Assert.Equal(2, first.Moves!.Count);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.Move(id, store.White, "g1", "f3", null, 2, 2, default));
        var second = await service.Undo(id, store.White, 2, 5, default);
        Assert.Equal(0, second.Version);
        Assert.Equal(6, second.Revision);
        Assert.Empty(second.Moves!);
    }

    [Fact]
    public async Task AiUndoRewindsPendingWhiteMoveButNotFriendGames()
    {
        var ai = new FakeData("ai"); var aiService = new ChessGameService(ai);
        await aiService.Move(ai.Game.GameId, ai.White, "e2", "e4", null, 0, 0, default);
        var undone = await aiService.Undo(ai.Game.GameId, ai.White, 1, 1, default);
        Assert.Equal(0, undone.Version);

        var friend = new FakeData("pvp"); var friendService = new ChessGameService(friend);
        await friendService.Move(friend.Game.GameId, friend.White, "e2", "e4", null, 0, 0, default);
        await Assert.ThrowsAsync<InvalidOperationException>(() => friendService.Undo(friend.Game.GameId, friend.White, 1, 1, default));
    }

    private sealed class FakeData : IChessGameData
    {
        public Guid White { get; } = Guid.NewGuid();
        public Guid Black { get; } = Guid.NewGuid();
        public ChessGameDto Game { get; private set; }
        private readonly List<ChessMoveDto> _moves = [];
        public FakeData(string mode) => Game = new(Guid.NewGuid(), White, mode == "pvp" ? Black : null, mode, "active",
            "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", "", 0, null, 2, DateTime.UtcNow);
        public Task<List<ChessGameDto>> List(Guid userId, CancellationToken ct) => Task.FromResult(new List<ChessGameDto> { Game });
        public Task<ChessGameDto?> Get(Guid id, Guid userId, CancellationToken ct) =>
            Task.FromResult<ChessGameDto?>(id == Game.GameId && (userId == White || userId == Black) ? Game : null);
        public Task<List<ChessMoveDto>> Moves(Guid id, Guid userId, CancellationToken ct) => Task.FromResult(_moves.ToList());
        public Task Create(Guid id, Guid userId, string mode, int difficulty, string fen, CancellationToken ct)
        {
            Game = Game with { GameId = id, WhiteUserId = userId, Mode = mode, Difficulty = difficulty, Fen = fen,
                Version = 0, Revision = 0, Pgn = "", Moves = null, LegalMoves = null };
            _moves.Clear();
            return Task.CompletedTask;
        }
        public Task Join(Guid id, Guid userId, CancellationToken ct) => throw new NotImplementedException();
        public Task Resign(Guid id, Guid userId, CancellationToken ct) => throw new NotImplementedException();
        public Task ApplyMove(Guid id, Guid userId, int version, int revision, string oldFen, bool whiteTurn, string from, string to,
            string? promotion, string? captured, string san, string fen, string pgn, string? result, CancellationToken ct)
        {
            Assert.Equal(Game.Version, version);
            Assert.Equal(Game.Revision, revision);
            Assert.Equal(Game.Fen, oldFen);
            _moves.Add(new(version + 1, from, to, promotion, captured, san));
            Game = Game with { Version = version + 1, Revision = revision + 1, Fen = fen, Pgn = pgn, Result = result, Status = result is null ? "active" : "finished" };
            return Task.CompletedTask;
        }
        public Task Undo(Guid id, Guid userId, int version, int revision, int keep, string fen, string pgn, CancellationToken ct)
        {
            Assert.Equal(Game.Version, version);
            Assert.Equal(Game.Revision, revision);
            _moves.RemoveRange(keep, _moves.Count - keep);
            Game = Game with { Version = keep, Revision = revision + 1, Fen = fen, Pgn = pgn, Result = null, Status = "active" };
            return Task.CompletedTask;
        }
        public Task<List<string>> CompletedLessons(Guid userId, CancellationToken ct) => Task.FromResult(new List<string>());
        public Task CompleteLesson(Guid userId, string lessonKey, CancellationToken ct) => Task.CompletedTask;
    }
}
