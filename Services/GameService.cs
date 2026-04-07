using Quisly.Models;
using Supabase.Postgrest;

namespace Quisly.Services;

public class GameService
{
    private readonly SupabaseService _supabase;
    private readonly AuthService _auth;

    public GameService(SupabaseService supabase, AuthService auth)
    {
        _supabase = supabase;
        _auth = auth;
    }

    public async Task<GameSession> CreateSession(string quizId)
    {
        if (_auth.CurrentUser?.Id == null)
            throw new InvalidOperationException("You must be signed in to host a game.");

        var pin = GeneratePin();
        var session = new GameSession
        {
            HostId = _auth.CurrentUser.Id,
            QuizId = quizId,
            Pin = pin,
            Status = SessionStatus.Lobby,
            CurrentQuestionIndex = 0,
            CreatedAt = DateTime.UtcNow
        };

        await _supabase.Client.From<GameSession>().Insert(session);
        return session;
    }

    public async Task<GameSession?> GetSessionById(string sessionId)
    {
        var response = await _supabase.Client
            .From<GameSession>()
            .Where(s => s.Id == sessionId)
            .Get();

        return response.Models.FirstOrDefault();
    }

    public async Task<GameSession?> JoinByPin(string pin)
    {
        var response = await _supabase.Client
            .From<GameSession>()
            .Where(s => s.Pin == pin)
            .Where(s => s.Status == SessionStatus.Lobby)
            .Get();

        return response.Models.FirstOrDefault();
    }

    public async Task<Player> AddPlayer(string sessionId, string nickname, string? avatarId = null)
    {
        var player = new Player
        {
            SessionId = sessionId,
            UserId = _auth.CurrentUser?.Id,
            Nickname = nickname,
            AvatarId = string.IsNullOrWhiteSpace(avatarId) ? "cat_lady" : avatarId,
            Score = 0,
            CurrentStreak = 0,
            JoinedAt = DateTime.UtcNow
        };

        await _supabase.Client.From<Player>().Insert(player);
        return player;
    }

    public async Task<Player?> GetPlayerByUser(string sessionId, string userId)
    {
        var response = await _supabase.Client
            .From<Player>()
            .Where(p => p.SessionId == sessionId)
            .Where(p => p.UserId == userId)
            .Get();

        return response.Models.FirstOrDefault();
    }

    public async Task<Player> EnsurePlayerForUser(string sessionId, string userId, string nickname, string? avatarId = null)
    {
        var existing = await GetPlayerByUser(sessionId, userId);
        if (existing != null) return existing;

        var player = new Player
        {
            SessionId = sessionId,
            UserId = userId,
            Nickname = nickname,
            AvatarId = string.IsNullOrWhiteSpace(avatarId) ? "cat_lady" : avatarId,
            Score = 0,
            CurrentStreak = 0,
            JoinedAt = DateTime.UtcNow
        };

        await _supabase.Client.From<Player>().Insert(player);
        return player;
    }

    public async Task<List<Player>> GetSessionPlayers(string sessionId)
    {
        var response = await _supabase.Client
            .From<Player>()
            .Where(p => p.SessionId == sessionId)
            .Order("score", Constants.Ordering.Descending)
            .Get();

        return response.Models;
    }

    public async Task<Player?> GetPlayerById(string playerId)
    {
        var player = await _supabase.Client
            .From<Player>()
            .Where(p => p.Id == playerId)
            .Single();

        return player;
    }

    public async Task RemovePlayer(string playerId)
    {
        // Supabase.Postgrest supports delete via query.
        await _supabase.Client
            .From<Player>()
            .Where(p => p.Id == playerId)
            .Delete();
    }

    public async Task UpdatePlayerNickname(string playerId, string nickname)
    {
        var player = await _supabase.Client
            .From<Player>()
            .Where(p => p.Id == playerId)
            .Single();

        if (player == null) return;
        player.Nickname = nickname;
        await player.Update<Player>();
    }

    public async Task UpdatePlayerAvatar(string playerId, string avatarId)
    {
        var player = await _supabase.Client
            .From<Player>()
            .Where(p => p.Id == playerId)
            .Single();

        if (player == null) return;
        player.AvatarId = string.IsNullOrWhiteSpace(avatarId) ? "cat_lady" : avatarId;
        await player.Update<Player>();
    }

    public async Task UpdateSessionStatus(string sessionId, string status, int? questionIndex = null)
    {
        var update = await _supabase.Client
            .From<GameSession>()
            .Where(s => s.Id == sessionId)
            .Single();

        if (update != null)
        {
            update.Status = status;
            if (questionIndex.HasValue)
                update.CurrentQuestionIndex = questionIndex.Value;
            if (status == SessionStatus.Playing && update.StartedAt == null)
                update.StartedAt = DateTime.UtcNow;
            if (status == SessionStatus.Finished)
                update.EndedAt = DateTime.UtcNow;

            await update.Update<GameSession>();
        }
    }

    public async Task SubmitAnswer(string playerId, string sessionId, string answer, int timeMs)
    {
        var player = await _supabase.Client
            .From<Player>()
            .Where(p => p.Id == playerId)
            .Single();

        if (player != null)
        {
            player.LastAnswer = answer;
            player.LastAnswerTimeMs = timeMs;
            await player.Update<Player>();
        }
    }

    public async Task ResetAnswers(string sessionId)
    {
        var players = await GetSessionPlayers(sessionId);
        foreach (var p in players)
        {
            p.LastAnswer = null;
            p.LastAnswerTimeMs = null;
            p.LastAnswerCorrect = null;
            await p.Update<Player>();
        }
    }

    public int CalculateScore(string answer, string correctAnswer, int timeMs, int maxTimeMs, int basePoints, int streak)
    {
        if (answer != correctAnswer) return 0;

        double timeFraction = 1.0 - ((double)timeMs / (maxTimeMs * 1000));
        timeFraction = Math.Max(0, Math.Min(1, timeFraction));

        int timeBonus = (int)(basePoints * timeFraction * 0.5);
        int streakBonus = Math.Min(streak * 100, 500);

        return basePoints / 2 + timeBonus + streakBonus;
    }

    public async Task UpdatePlayerScore(string playerId, int additionalScore, bool correct)
    {
        var player = await _supabase.Client
            .From<Player>()
            .Where(p => p.Id == playerId)
            .Single();

        if (player != null)
        {
            player.Score += additionalScore;
            player.LastAnswerCorrect = correct;
            player.CurrentStreak = correct ? player.CurrentStreak + 1 : 0;
            await player.Update<Player>();
        }
    }

    public async Task<List<Question>> GetQuizQuestions(string quizId)
    {
        var response = await _supabase.Client
            .From<Question>()
            .Where(q => q.QuizId == quizId)
            .Order("order_index", Constants.Ordering.Ascending)
            .Get();

        return response.Models;
    }

    public async Task<List<Quiz>> GetMyQuizzes()
    {
        if (_auth.CurrentUser == null) return new List<Quiz>();

        var response = await _supabase.Client
            .From<Quiz>()
            .Where(q => q.CreatorId == _auth.CurrentUser.Id!)
            .Order("created_at", Constants.Ordering.Descending)
            .Get();

        return response.Models;
    }

    public async Task<Quiz> CreateQuiz(string title, string? description, List<Question> questions)
    {
        if (_auth.CurrentUser?.Id == null)
            throw new InvalidOperationException("You must be signed in to create a quiz.");

        var quiz = new Quiz
        {
            CreatorId = _auth.CurrentUser.Id,
            Title = title,
            Description = description,
            IsPublic = false,
            QuestionCount = questions.Count,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _supabase.Client.From<Quiz>().Insert(quiz);

        for (int i = 0; i < questions.Count; i++)
        {
            questions[i].QuizId = quiz.Id;
            questions[i].OrderIndex = i;
        }

        await _supabase.Client.From<Question>().Insert(questions);

        return quiz;
    }

    private static string GeneratePin()
    {
        var random = new Random();
        return random.Next(100000, 999999).ToString();
    }
}
