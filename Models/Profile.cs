using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Quisly.Models;

[Table("profiles")]
public class Profile : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    public string Id { get; set; } = string.Empty;

    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [Column("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [Column("avatar_url")]
    public string? AvatarUrl { get; set; }

    [Column("account_type")]
    public string AccountType { get; set; } = "Normal";

    [Column("total_games_played")]
    public int TotalGamesPlayed { get; set; }

    [Column("total_games_hosted")]
    public int TotalGamesHosted { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
