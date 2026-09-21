using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using MySqlConnector;
using PersonalTools.Classes.ChessGames;
using PersonalTools.Hubs;
using PersonalTools.Security;

namespace PersonalTools.Controllers;

[Authorize(Policy = AppAuthorizationPolicies.RegisteredUser)]
[ApiController]
[Route("api/chess")]
public sealed class ChessController(ChessGameService games, ChessLessonService lessons, IHubContext<ChessHub> hub, ILogger<ChessController> logger) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet] public Task<ActionResult<List<ChessGameDto>>> List(CancellationToken ct) => Run(() => games.List(UserId, ct));
    [HttpGet("{id:guid}")] public Task<ActionResult<ChessGameDto>> Get(Guid id, CancellationToken ct) => Run(() => games.Get(id, UserId, ct));
    [HttpPost, EnableRateLimiting("case-battles-write")]
    public Task<ActionResult<ChessGameDto>> Create([FromBody] ChessCreateRequest request, CancellationToken ct) => Run(() => games.Create(UserId, request.Mode, request.Difficulty, ct));
    [HttpPost("{id:guid}/join"), EnableRateLimiting("case-battles-write")]
    public Task<ActionResult<ChessGameDto>> Join(Guid id, CancellationToken ct) => Run(async () => { var game = await games.Join(id, UserId, ct); await Notify(id, ct); return game; });
    [HttpPost("{id:guid}/move"), EnableRateLimiting("chess-move")]
    public Task<ActionResult<ChessGameDto>> Move(Guid id, [FromBody] ChessMoveRequest request, CancellationToken ct) => Run(async () =>
    { var game = await games.Move(id, UserId, request.From, request.To, request.Promotion, request.Version, request.Revision, ct); await Notify(id, ct); return game; });
    [HttpPost("{id:guid}/undo"), EnableRateLimiting("chess-move")]
    public Task<ActionResult<ChessGameDto>> Undo(Guid id, [FromBody] ChessUndoRequest request, CancellationToken ct) => Run(async () =>
    { var game = await games.Undo(id, UserId, request.Version, request.Revision, ct); await Notify(id, ct); return game; });
    [HttpGet("lessons/completed")]
    public Task<ActionResult<List<string>>> CompletedLessons(CancellationToken ct) => Run(() => lessons.Completed(UserId, ct));
    [HttpPost("lessons/{key}/complete"), EnableRateLimiting("chess-move")]
    public Task<ActionResult<ChessLessonCompletion>> CompleteLesson(string key, CancellationToken ct) => Run(() => lessons.Complete(UserId, key, ct));
    [HttpPost("{id:guid}/resign"), EnableRateLimiting("case-battles-write")]
    public Task<ActionResult<ChessGameDto>> Resign(Guid id, CancellationToken ct) => Run(async () => { var game = await games.Resign(id, UserId, ct); await Notify(id, ct); return game; });
    private Task Notify(Guid id, CancellationToken ct) => hub.Clients.Group(ChessHub.Group(id)).SendAsync("ChessChanged", ct);
    private async Task<ActionResult<T>> Run<T>(Func<Task<T>> operation)
    {
        try { return Ok(await operation()); }
        catch (KeyNotFoundException) { return NotFound(new { message = "Game not found." }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("Game changed.", StringComparison.Ordinal))
        { return Conflict(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (MySqlException ex) when (ex.SqlState == "45000" && ex.Message.StartsWith("Game changed.", StringComparison.Ordinal))
        { return Conflict(new { message = ex.Message }); }
        catch (MySqlException ex) when (ex.SqlState == "45000") { return BadRequest(new { message = ex.Message }); }
        catch (Exception ex) { logger.LogError(ex, "Chess request failed for {UserId}", UserId); return StatusCode(500, new { message = "Chess is unavailable right now." }); }
    }
}

public sealed record ChessCreateRequest(string Mode, int Difficulty);
public sealed record ChessMoveRequest(string From, string To, string? Promotion, int Version, int Revision);
public sealed record ChessUndoRequest(int Version, int Revision);
