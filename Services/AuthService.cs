using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;
using Quisly.Models;

namespace Quisly.Services;

public class AuthService
{
    private readonly SupabaseService _supabase;

    public IGotrueClient<User, Session> Auth => _supabase.Client.Auth;

    public event Action? OnAuthStateChanged;

    public AuthService(SupabaseService supabase)
    {
        _supabase = supabase;
    }

    public User? CurrentUser => Auth.CurrentUser;
    public bool IsAuthenticated => Auth.CurrentUser != null;

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
        var session = await Auth.SignIn(email, password);
        OnAuthStateChanged?.Invoke();
        return session;
    }

    public async Task SignOut()
    {
        await Auth.SignOut();
        OnAuthStateChanged?.Invoke();
    }

    public async Task<Profile?> GetCurrentProfile()
    {
        if (CurrentUser == null) return null;

        var response = await _supabase.Client
            .From<Profile>()
            .Where(p => p.Id == CurrentUser.Id!)
            .Single();

        return response;
    }
}
