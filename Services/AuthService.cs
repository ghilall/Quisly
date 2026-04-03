using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;
using Quisly.Models;

namespace Quisly.Services;

public class AuthService
{
    private readonly SupabaseService _supabase;
    private Profile? _cachedProfile;

    public IGotrueClient<User, Session> Auth => _supabase.Client.Auth;

    public event Action? OnAuthStateChanged;

    public AuthService(SupabaseService supabase)
    {
        _supabase = supabase;
    }

    public User? CurrentUser => Auth.CurrentUser;
    public bool IsAuthenticated => Auth.CurrentUser != null;

    // ── XP / Level helpers ──────────────────────────────────────────────────

    /// <summary>Minimum XP required to reach a given level.</summary>
    public static int XpForLevel(int level) => (level - 1) * (level - 1) * 100;

    /// <summary>Current level for a given total XP.</summary>
    public static int GetLevel(int xp) => (int)Math.Floor(Math.Sqrt(xp / 100.0)) + 1;

    /// <summary>Progress (0–100) within the current level toward the next.</summary>
    public static int LevelProgressPercent(int xp)
    {
        var level = GetLevel(xp);
        var low = XpForLevel(level);
        var high = XpForLevel(level + 1);
        return (int)((xp - low) / (double)(high - low) * 100);
    }

    /// <summary>XP still needed to advance to the next level.</summary>
    public static int XpToNextLevel(int xp)
    {
        var level = GetLevel(xp);
        return XpForLevel(level + 1) - xp;
    }

    // ── XP awarded by finish rank ────────────────────────────────────────────
    public static int XpForRank(int rank) => rank switch
    {
        1 => 200,
        2 or 3 => 100,
        4 or 5 => 50,
        _ => 10   // participation
    };

    // ── Session restore ──────────────────────────────────────────────────────

    public async Task RestoreSessionAsync()
    {
        try
        {
            await Auth.RetrieveSessionAsync();
            OnAuthStateChanged?.Invoke();
        }
        catch
        {
            // No stored session — user needs to sign in
        }
    }

    // ── Auth operations ──────────────────────────────────────────────────────

    public async Task<Session?> SignUp(string email, string password, string username)
    {
        var session = await Auth.SignUp(email, password, new SignUpOptions
        {
            Data = new Dictionary<string, object>
            {
                { "username", username }
            }
        });

        OnAuthStateChanged?.Invoke();
        return session;
    }

    public async Task<Session?> SignIn(string email, string password)
    {
        _cachedProfile = null;
        var session = await Auth.SignIn(email, password);
        OnAuthStateChanged?.Invoke();
        return session;
    }

    public async Task SignOut()
    {
        _cachedProfile = null;
        await Auth.SignOut();
        OnAuthStateChanged?.Invoke();
    }

    // ── Profile ──────────────────────────────────────────────────────────────

    public async Task<Profile?> GetCurrentProfile(bool forceRefresh = false)
    {
        if (CurrentUser == null) return null;
        if (_cachedProfile != null && !forceRefresh) return _cachedProfile;

        var response = await _supabase.Client
            .From<Profile>()
            .Where(p => p.Id == CurrentUser.Id!)
            .Single();

        _cachedProfile = response;
        return response;
    }

    // ── XP award (call after a game ends) ────────────────────────────────────

    public async Task AwardGameXp(int rank)
    {
        if (CurrentUser == null) return;

        var profile = await GetCurrentProfile();
        if (profile == null) return;

        var xpGained = XpForRank(rank);
        profile.Xp += xpGained;
        profile.Level = GetLevel(profile.Xp);
        profile.TotalGamesPlayed++;
        if (rank == 1) profile.TotalGamesWon++;

        await profile.Update<Profile>();
        _cachedProfile = profile;
        OnAuthStateChanged?.Invoke();
    }

    // ── Premium upgrade ──────────────────────────────────────────────────────

    public async Task UpgradeToPremium()
    {
        if (CurrentUser == null) return;

        var profile = await GetCurrentProfile();
        if (profile == null) return;

        profile.AccountType = "Premium";
        await profile.Update<Profile>();
        _cachedProfile = profile;
        OnAuthStateChanged?.Invoke();
    }
}
