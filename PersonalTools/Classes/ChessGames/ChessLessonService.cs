using System.Text.Json;
using PersonalTools.Data.ChessGames;

namespace PersonalTools.Classes.ChessGames;

public sealed record ChessLessonCompletion(string LessonKey, bool Completed);

public sealed class ChessLessonService(IChessGameData data, IWebHostEnvironment environment)
{
    private readonly Lazy<HashSet<string>> _knownLessons = new(() =>
    {
        var path = Path.Combine(environment.WebRootPath, "vendor", "chess", "tutorials", "lessons.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.GetProperty("categories").EnumerateArray()
            .SelectMany(category => category.GetProperty("lessons").EnumerateArray())
            .Select(lesson => lesson.GetProperty("id").GetString()!)
            .ToHashSet(StringComparer.Ordinal);
    });

    public Task<List<string>> Completed(Guid userId, CancellationToken ct) => data.CompletedLessons(userId, ct);

    public async Task<ChessLessonCompletion> Complete(Guid userId, string lessonKey, CancellationToken ct)
    {
        if (!_knownLessons.Value.Contains(lessonKey)) throw new ArgumentException("Unknown chess lesson.");
        await data.CompleteLesson(userId, lessonKey, ct);
        return new ChessLessonCompletion(lessonKey, true);
    }
}
