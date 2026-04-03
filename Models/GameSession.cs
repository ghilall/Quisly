using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Quisly.Models;

[Table("sessions")]
public class GameSession : BaseModel
{
    [PrimaryKey("id")]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Column("host_id")]
    public string HostId { get; set; } = string.Empty;

    [Column("quiz_id")]
    public string QuizId { get; set; } = string.Empty;

    [Column("pin")]
    public string Pin { get; set; } = string.Empty;

    [Column("status")]
    public string Status { get; set; } = "lobby";

    [Column("current_question_index")]
    public int CurrentQuestionIndex { get; set; }

    [Column("started_at")]
    public DateTime? StartedAt { get; set; }

    [Column("ended_at")]
    public DateTime? EndedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}

public static class SessionStatus
{
    public const string Lobby = "lobby";
    public const string Playing = "playing";
    public const string ShowingQuestion = "showing_question";
    public const string ShowingResults = "showing_results";
    public const string Leaderboard = "leaderboard";
    public const string Finished = "finished";
}
