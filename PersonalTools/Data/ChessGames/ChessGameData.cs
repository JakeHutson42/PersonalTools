using MySqlConnector;
using PersonalTools.Classes.ChessGames;

namespace PersonalTools.Data.ChessGames;

public interface IChessGameData
{
    Task<List<ChessGameDto>> List(Guid userId, CancellationToken ct);
    Task<ChessGameDto?> Get(Guid id, Guid userId, CancellationToken ct);
    Task<List<ChessMoveDto>> Moves(Guid id, Guid userId, CancellationToken ct);
    Task Create(Guid id, Guid userId, string mode, int difficulty, string fen, CancellationToken ct);
    Task Join(Guid id, Guid userId, CancellationToken ct);
    Task ApplyMove(Guid id, Guid userId, int version, int revision, string oldFen, bool whiteTurn, string from, string to,
        string? promotion, string? captured, string san, string fen, string pgn, string? result, CancellationToken ct);
    Task Undo(Guid id, Guid userId, int version, int revision, int keep, string fen, string pgn, CancellationToken ct);
    Task<List<string>> CompletedLessons(Guid userId, CancellationToken ct);
    Task CompleteLesson(Guid userId, string lessonKey, CancellationToken ct);
    Task Resign(Guid id, Guid userId, CancellationToken ct);
}

public sealed class ChessGameData(IMariaDbDataAccess db) : IChessGameData
{
    public Task<List<ChessGameDto>> List(Guid userId, CancellationToken ct) =>
        db.GetBulkDataSP("sp_chess_games_list", MapGame, P(("p_user_id", userId)), ct);
    public Task<ChessGameDto?> Get(Guid id, Guid userId, CancellationToken ct) =>
        db.GetDataSP("sp_chess_game_get", MapGame, P(("p_game_id", id), ("p_user_id", userId)), ct);
    public Task<List<ChessMoveDto>> Moves(Guid id, Guid userId, CancellationToken ct) =>
        db.GetBulkDataSP("sp_chess_moves_list", r => new ChessMoveDto(r.GetInt32("Ply"), S(r,"FromSquare"), S(r,"ToSquare"),
            N(r,"Promotion") ? null : S(r,"Promotion"),
            N(r,"CapturedPiece") ? null : S(r,"CapturedPiece"), S(r,"San")),
            P(("p_game_id", id), ("p_user_id", userId)), ct);
    public async Task Create(Guid id, Guid userId, string mode, int difficulty, string fen, CancellationToken ct) =>
        await db.ExecuteSP("sp_chess_game_create", P(("p_game_id", id), ("p_user_id", userId), ("p_mode", mode), ("p_difficulty", difficulty), ("p_fen", fen)), ct);
    public async Task Join(Guid id, Guid userId, CancellationToken ct) =>
        await db.ExecuteSP("sp_chess_game_join", P(("p_game_id", id), ("p_user_id", userId)), ct);
    public async Task ApplyMove(Guid id, Guid userId, int version, int revision, string oldFen, bool whiteTurn, string from, string to,
        string? promotion, string? captured, string san, string fen, string pgn, string? result, CancellationToken ct) =>
        await db.ExecuteSP("sp_chess_move_apply", P(("p_game_id", id), ("p_user_id", userId), ("p_version", version), ("p_revision", revision),
            ("p_old_fen", oldFen), ("p_turn", whiteTurn ? "w" : "b"), ("p_from", from), ("p_to", to),
            ("p_promotion", promotion), ("p_captured", captured), ("p_san", san), ("p_fen", fen), ("p_pgn", pgn), ("p_result", result)), ct);
    public async Task Undo(Guid id, Guid userId, int version, int revision, int keep, string fen, string pgn, CancellationToken ct) =>
        await db.ExecuteSP("sp_chess_game_undo", P(("p_game_id", id), ("p_user_id", userId), ("p_version", version),
            ("p_revision", revision), ("p_new_version", keep), ("p_fen", fen), ("p_pgn", pgn)), ct);
    public Task<List<string>> CompletedLessons(Guid userId, CancellationToken ct) =>
        db.GetBulkDataSP("sp_chess_lessons_completed", r => S(r, "LessonKey"), P(("p_user_id", userId)), ct);
    public async Task CompleteLesson(Guid userId, string lessonKey, CancellationToken ct) =>
        await db.ExecuteSP("sp_chess_lesson_complete", P(("p_user_id", userId), ("p_lesson_key", lessonKey)), ct);
    public async Task Resign(Guid id, Guid userId, CancellationToken ct) =>
        await db.ExecuteSP("sp_chess_game_resign", P(("p_game_id", id), ("p_user_id", userId)), ct);

    private static ChessGameDto MapGame(MySqlDataReader r) => new(r.GetGuid("GameId"), r.GetGuid("WhiteUserId"),
        N(r,"BlackUserId") ? null : r.GetGuid("BlackUserId"), S(r,"Mode"), S(r,"Status"),
        S(r,"Fen"), S(r,"Pgn"), r.GetInt32("Version"), N(r,"Result") ? null : S(r,"Result"),
        r.GetInt32("Difficulty"), r.GetDateTime("UpdatedUtc"), Revision: r.GetInt32("Revision"));
    private static string S(MySqlDataReader reader, string name) => reader.GetString(reader.GetOrdinal(name));
    private static bool N(MySqlDataReader reader, string name) => reader.IsDBNull(reader.GetOrdinal(name));
    private static IEnumerable<MySqlParameter> P(params (string Name, object? Value)[] values) =>
        values.Select(v => new MySqlParameter(v.Name, v.Value is Guid id ? id.ToString("D") : v.Value ?? DBNull.Value));
}
