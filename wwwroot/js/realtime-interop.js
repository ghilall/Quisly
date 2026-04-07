window.QuislyRealtime = {
    _client: null,
    _channels: {},

    init: function (url, anonKey) {
        // If Supabase JS is unavailable (offline / DNS issues / CSP),
        // we silently no-op and the app will fall back to polling.
        if (!window.supabase) return;
        this._client = window.supabase.createClient(url, anonKey);
    },

    subscribeToSession: function (channelId, sessionId, dotNetRef) {
        if (!this._client) return;

        const channel = this._client
            .channel(channelId)
            .on(
                'postgres_changes',
                { event: 'UPDATE', schema: 'public', table: 'sessions', filter: `id=eq.${sessionId}` },
                (payload) => {
                    const status = payload.new?.status;
                    if (status) {
                        dotNetRef.invokeMethodAsync('OnSessionStatusChanged', status);
                    }
                }
            )
            .subscribe();

        this._channels[channelId] = channel;
    },

    subscribeToPlayers: function (channelId, sessionId, dotNetRef) {
        if (!this._client) return;

        const channel = this._client
            .channel(channelId)
            .on(
                'postgres_changes',
                { event: '*', schema: 'public', table: 'players', filter: `session_id=eq.${sessionId}` },
                (_payload) => {
                    dotNetRef.invokeMethodAsync('OnPlayersChanged');
                }
            )
            .subscribe();

        this._channels[channelId] = channel;
    },

    unsubscribe: function (channelId) {
        const channel = this._channels[channelId];
        if (channel) {
            this._client.removeChannel(channel);
            delete this._channels[channelId];
        }
    },

    unsubscribeAll: function () {
        for (const id in this._channels) {
            if (this._channels[id]) {
                this._client.removeChannel(this._channels[id]);
            }
        }
        this._channels = {};
    }
};
