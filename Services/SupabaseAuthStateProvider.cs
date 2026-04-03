using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace Quisly.Services;

public class SupabaseAuthStateProvider : AuthenticationStateProvider
{
    private readonly AuthService _authService;

    public SupabaseAuthStateProvider(AuthService authService)
    {
        _authService = authService;
        _authService.OnAuthStateChanged += () => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var user = _authService.CurrentUser;

        if (user == null)
        {
            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id!),
            new(ClaimTypes.Email, user.Email ?? ""),
        };

        var identity = new ClaimsIdentity(claims, "supabase");
        return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
    }
}
