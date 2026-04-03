using Microsoft.JSInterop;

namespace Quisly.Services;

public class RealtimeService : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly SupabaseService _supabase;
    private readonly List<string> _channelIds = new();
    private bool _initialized;

    public RealtimeService(IJSRuntime js, SupabaseService supabase)
    {
        _js = js;
        _supabase = supabase;
    }

    private async Task EnsureInitialized()
    {
        if (_initialized) return;
        await _js.InvokeVoidAsync("QuislyRealtime.init", _supabase.Url, _supabase.AnonKey);
        _initialized = true;
    }

    public async Task SubscribeToSession<T>(string sessionId, DotNetObjectReference<T> dotNetRef) where T : class
    {
        await EnsureInitialized();
        var channelId = $"session:{sessionId}";
        _channelIds.Add(channelId);
        await _js.InvokeVoidAsync("QuislyRealtime.subscribeToSession", channelId, sessionId, dotNetRef);
    }

    public async Task SubscribeToPlayers<T>(string sessionId, DotNetObjectReference<T> dotNetRef) where T : class
    {
        await EnsureInitialized();
        var channelId = $"players:{sessionId}";
        _channelIds.Add(channelId);
        await _js.InvokeVoidAsync("QuislyRealtime.subscribeToPlayers", channelId, sessionId, dotNetRef);
    }

    public async Task UnsubscribeAll()
    {
        try
        {
            await _js.InvokeVoidAsync("QuislyRealtime.unsubscribeAll");
        }
        catch
        {
            // Component may have been disposed during navigation
        }
        _channelIds.Clear();
    }

    public async ValueTask DisposeAsync()
    {
        await UnsubscribeAll();
        GC.SuppressFinalize(this);
    }
}
