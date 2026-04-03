using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Quisly.Models;

[Table("questions")]
public class Question : BaseModel
{
    [PrimaryKey("id")]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Column("quiz_id")]
    public string QuizId { get; set; } = string.Empty;

    [Column("question_text")]
    public string QuestionText { get; set; } = string.Empty;

    [Column("image_url")]
    public string? ImageUrl { get; set; }

    [Column("option_a")]
    public string OptionA { get; set; } = string.Empty;

    [Column("option_b")]
    public string OptionB { get; set; } = string.Empty;

    [Column("option_c")]
    public string OptionC { get; set; } = string.Empty;

    [Column("option_d")]
    public string OptionD { get; set; } = string.Empty;

    [Column("correct_option")]
    public string CorrectOption { get; set; } = "A";

    [Column("time_limit_seconds")]
    public int TimeLimitSeconds { get; set; } = 20;

    [Column("points")]
    public int Points { get; set; } = 1000;

    [Column("order_index")]
    public int OrderIndex { get; set; }
}
