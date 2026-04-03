using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using Quisly;
using Quisly.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddSingleton<SupabaseService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<GameService>();
builder.Services.AddScoped<RealtimeService>();

builder.Services.AddScoped<AuthenticationStateProvider, SupabaseAuthStateProvider>();
builder.Services.AddAuthorizationCore();

var host = builder.Build();

try
{
    var supabase = host.Services.GetRequiredService<SupabaseService>();
    await supabase.InitializeAsync();

    // Restore session from browser storage so CurrentUser is populated on page load/refresh
    var authService = host.Services.GetRequiredService<AuthService>();
    await authService.RestoreSessionAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"Supabase initialization warning: {ex.Message}");
}

await host.RunAsync();
