using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Quisly.Models;

[Table("players")]
public class Player : BaseModel
{
    [PrimaryKey("id")]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Column("session_id")]
    public string SessionId { get; set; } = string.Empty;

    [Column("user_id")]
    public string? UserId { get; set; }

    [Column("nickname")]
    public string Nickname { get; set; } = string.Empty;

    [Column("avatar_id")]
    public string AvatarId { get; set; } = "cat_lady";

    [Column("score")]
    public int Score { get; set; }

    [Column("current_streak")]
    public int CurrentStreak { get; set; }

    [Column("last_answer")]
    public string? LastAnswer { get; set; }

    [Column("last_answer_time_ms")]
    public int? LastAnswerTimeMs { get; set; }

    [Column("last_answer_correct")]
    public bool? LastAnswerCorrect { get; set; }

    [Column("joined_at")]
    public DateTime JoinedAt { get; set; }
}
