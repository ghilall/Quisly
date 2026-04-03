using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Quisly.Models;

[Table("quizzes")]
public class Quiz : BaseModel
{
    [PrimaryKey("id")]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Column("creator_id")]
    public string CreatorId { get; set; } = string.Empty;

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("is_public")]
    public bool IsPublic { get; set; }

    [Column("question_count")]
    public int QuestionCount { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
