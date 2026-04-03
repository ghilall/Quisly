using Supabase;
using Microsoft.Extensions.Configuration;

namespace Quisly.Services;

public class SupabaseService
{
    private readonly Client _client;
    private readonly string _url;
    private readonly string _anonKey;

    public Client Client => _client;
    public string Url => _url;
    public string AnonKey => _anonKey;

    public SupabaseService(IConfiguration configuration)
    {
        _url = configuration["Supabase:Url"]
            ?? throw new InvalidOperationException("Supabase:Url is not configured");
        _anonKey = configuration["Supabase:AnonKey"]
            ?? throw new InvalidOperationException("Supabase:AnonKey is not configured");

        _client = new Client(_url, _anonKey, new SupabaseOptions
        {
            AutoRefreshToken = true,
            AutoConnectRealtime = false
        });
    }

    public async Task InitializeAsync()
    {
        await _client.InitializeAsync();
    }
}
