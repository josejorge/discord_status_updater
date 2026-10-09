// File: DiscordRpcService.cs
// Description: Thin wrapper over the Discord Rich Presence IPC client, able to switch Application IDs at runtime.
// Author: Jose-Jorge HERNANDEZ
// Company: Parlee Conseiller, Inc.
// Date: 2026-06-01
// Last edit date: 2026-10-09
// Version: 1.1.0

using DiscordRPC;
using DiscordRPC.Logging;

namespace DiscordStatusUpdater.Services;

public sealed class DiscordRpcService : IDisposable
{
    private DiscordRpcClient? _client;
    private bool _disposed;

    public event EventHandler? Connected;
    public event EventHandler? Disconnected;
    public event EventHandler<string>? ErrorOccurred;

    public bool IsInitialized => _client?.IsInitialized ?? false;

    /// <summary>The Application ID of the live connection, or null when not connected.</summary>
    public string? CurrentApplicationId { get; private set; }

    public void Connect(string applicationId)
    {
        if (string.IsNullOrWhiteSpace(applicationId))
            throw new ArgumentException("Application ID cannot be empty.", nameof(applicationId));

        Disconnect();

        var client = new DiscordRpcClient(applicationId)
        {
            Logger = new ConsoleLogger(LogLevel.Warning)
        };

        // Auto-detect swaps Application IDs while the app runs. Events of a client we already replaced
        // (for instance the Close raised by disposing it) must not look like a real disconnect.
        client.OnReady += (_, _) => { if (ReferenceEquals(client, _client)) Connected?.Invoke(this, EventArgs.Empty); };
        client.OnClose += (_, _) => { if (ReferenceEquals(client, _client)) Disconnected?.Invoke(this, EventArgs.Empty); };
        client.OnError += (_, e) => { if (ReferenceEquals(client, _client)) ErrorOccurred?.Invoke(this, e.Message); };
        client.OnConnectionFailed += (_, _) =>
        {
            if (ReferenceEquals(client, _client))
                ErrorOccurred?.Invoke(this, "Could not connect to Discord — make sure Discord is running.");
        };

        _client = client;
        CurrentApplicationId = applicationId;
        client.Initialize();
    }

    public void SetPresence(RichPresence presence)
    {
        if (_client == null || !_client.IsInitialized)
            throw new InvalidOperationException("Not connected to Discord.");

        _client.SetPresence(presence);
    }

    public void ClearPresence() => _client?.ClearPresence();

    public void Disconnect()
    {
        var client = _client;
        if (client == null) return;

        // Detach first so the events raised while disposing are ignored (see Connect).
        _client = null;
        CurrentApplicationId = null;
        try
        {
            client.ClearPresence();
            client.Dispose();
        }
        catch { /* best-effort */ }
    }

    public void Dispose()
    {
        if (_disposed) return;
        Disconnect();
        _disposed = true;
    }
}
