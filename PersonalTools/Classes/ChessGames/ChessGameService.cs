using Chess;
using PersonalTools.Data.ChessGames;

namespace PersonalTools.Classes.ChessGames;

public record ChessMoveDto(int Ply, string From, string To, string? Promotion, string? Captured, string San);
public record ChessLegalMoveDto(string From, string To, string? Promotion, bool Capture);
public record ChessGameDto(Guid GameId, Guid WhiteUserId, Guid? BlackUserId, string Mode, string Status,
    string Fen, string Pgn, int Version, string? Result, int Difficulty, DateTime UpdatedUtc,
    List<ChessMoveDto>? Moves = null, List<ChessLegalMoveDto>? LegalMoves = null, int Revision = 0);

public sealed class ChessGameService(IChessGameData data)
{
    public Task<List<ChessGameDto>> List(Guid userId, CancellationToken ct) => data.List(userId, ct);

    public async Task<ChessGameDto> Get(Guid id, Guid userId, CancellationToken ct)
    {
        // The game and its move list use separate stored procedures. Re-read the version so a
        // concurrent move cannot pair an old FEN with a newer list of moves (important for AI).
        for (int attempt = 0; attempt < 3; attempt++)
        {
            var game = await data.Get(id, userId, ct) ?? throw new KeyNotFoundException();
            var moves = await data.Moves(id, userId, ct);
            var latest = await data.Get(id, userId, ct) ?? throw new KeyNotFoundException();
            if (latest.Version == game.Version && latest.Revision == game.Revision && moves.Count == game.Version)
            {
                bool whiteTurn = latest.Fen.Split(' ')[1] == "w";
                bool mayMove = latest.Status == "active" && (whiteTurn ? latest.WhiteUserId == userId :
                    latest.Mode == "pvp" && latest.BlackUserId == userId);
                if (!mayMove) return latest with { Moves = moves, LegalMoves = [] };
                var board = LoadBoard(latest.Pgn);
                var legal = board.Moves().Select(move => new ChessLegalMoveDto(
                    move.OriginalPosition.ToString(), move.NewPosition.ToString(),
                    move.IsPromotion ? move.Promotion?.ToString().LastOrDefault().ToString().ToLowerInvariant() : null,
                    move.CapturedPiece is not null)).ToList();
                return latest with { Moves = moves, LegalMoves = legal };
            }
        }
        throw new InvalidOperationException("Game changed. Refresh the board.");
    }

    public async Task<ChessGameDto> Create(Guid userId, string mode, int difficulty, CancellationToken ct)
    {
        if (mode is not ("ai" or "pvp") ||
            !((difficulty >= 100 && difficulty <= 2400 && difficulty % 100 == 0) || difficulty is >= 1 and <= 4))
            throw new ArgumentException("Invalid game options.");
        var id = Guid.NewGuid();
        await data.Create(id, userId, mode, difficulty, new ChessBoard().ToFen(), ct);
        return await Get(id, userId, ct);
    }

    public async Task<ChessGameDto> Join(Guid id, Guid userId, CancellationToken ct)
    {
        await data.Join(id, userId, ct);
        return await Get(id, userId, ct);
    }

    public Task<ChessGameDto> Move(Guid id, Guid userId, string from, string to, string? promotion, int version, CancellationToken ct) =>
        Move(id, userId, from, to, promotion, version, version, ct);

    public async Task<ChessGameDto> Move(Guid id, Guid userId, string from, string to, string? promotion, int version, int revision, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to) ||
            !System.Text.RegularExpressions.Regex.IsMatch(from, "^[a-h][1-8]$") ||
            !System.Text.RegularExpressions.Regex.IsMatch(to, "^[a-h][1-8]$") ||
            (promotion is not null && promotion is not ("q" or "r" or "b" or "n"))) throw new ArgumentException("Invalid move coordinates.");
        var game = await data.Get(id, userId, ct) ?? throw new KeyNotFoundException();
        if (game.Status != "active" || game.Version != version || game.Revision != revision) throw new InvalidOperationException("Game changed. Refresh the board.");
        var board = LoadBoard(game.Pgn);
        bool whiteTurn = board.Turn == PieceColor.White;
        if (whiteTurn ? game.WhiteUserId != userId : game.Mode == "ai" ? game.WhiteUserId != userId : game.BlackUserId != userId)
            throw new InvalidOperationException("It is not your turn.");
        var move = board.Moves().FirstOrDefault(m => m.OriginalPosition.ToString() == from && m.NewPosition.ToString() == to &&
            (!m.IsPromotion ? promotion is null : string.Equals(m.Promotion?.ToString().LastOrDefault().ToString(), promotion ?? "q", StringComparison.OrdinalIgnoreCase)));
        if (move is null || !board.Move(move)) throw new InvalidOperationException("That move is not legal.");
        string? result = board.IsEndGame ? board.EndGame?.WonSide == PieceColor.White ? "white" : board.EndGame?.WonSide == PieceColor.Black ? "black" : "draw" : null;
        // The procedure locks and checks the old position/version again: two tabs cannot both commit a move.
        await data.ApplyMove(id, userId, version, revision, game.Fen, whiteTurn, from, to, promotion,
            move.CapturedPiece?.ToString(), move.San ?? "", board.ToFen(), board.ToPgn(), result, ct);
        return await Get(id, userId, ct);
    }

    public async Task<ChessGameDto> Undo(Guid id, Guid userId, int version, int revision, CancellationToken ct)
    {
        var game = await data.Get(id, userId, ct) ?? throw new KeyNotFoundException();
        if (game.Mode != "ai" || game.WhiteUserId != userId || game.Version == 0)
            throw new InvalidOperationException("Undo is available for your computer games after a move.");
        if (game.Version != version || game.Revision != revision)
            throw new InvalidOperationException("Game changed. Refresh the board.");
        var moves = await data.Moves(id, userId, ct);
        if (moves.Count != game.Version) throw new InvalidOperationException("Game changed. Refresh the board.");
        // Rewind the AI reply with the player's preceding move, or just the player's
        // move when the computer has not answered yet. Replaying avoids trusting a client FEN.
        int keep = game.Version - (game.Version % 2 == 0 ? 2 : 1);
        var board = LoadBoard("");
        foreach (var played in moves.Take(keep))
        {
            var legal = board.Moves().FirstOrDefault(m => m.OriginalPosition.ToString() == played.From &&
                m.NewPosition.ToString() == played.To &&
                (!m.IsPromotion || string.Equals(m.Promotion?.ToString().LastOrDefault().ToString(), played.Promotion ?? "q", StringComparison.OrdinalIgnoreCase)));
            if (legal is null || !board.Move(legal)) throw new InvalidOperationException("Saved moves could not be replayed.");
        }
        await data.Undo(id, userId, version, revision, keep, board.ToFen(), board.ToPgn(), ct);
        return await Get(id, userId, ct);
    }

    public async Task<ChessGameDto> Resign(Guid id, Guid userId, CancellationToken ct)
    {
        await data.Resign(id, userId, ct);
        return await Get(id, userId, ct);
    }
    public Task Delete(Guid id, Guid userId, CancellationToken ct) => data.Delete(id, userId, ct);

    private static ChessBoard LoadBoard(string pgn) => string.IsNullOrWhiteSpace(pgn)
        ? new ChessBoard { AutoEndgameRules = AutoEndgameRules.All }
        : ChessBoard.LoadFromPgn(pgn, AutoEndgameRules.All);
}
