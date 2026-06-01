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

    public void Connect(string applicationId)
    {
        if (string.IsNullOrWhiteSpace(applicationId))
            throw new ArgumentException("Application ID cannot be empty.", nameof(applicationId));

        Disconnect();

        _client = new DiscordRpcClient(applicationId)
        {
            Logger = new ConsoleLogger(LogLevel.Warning)
        };

        _client.OnReady += (_, _) => Connected?.Invoke(this, EventArgs.Empty);
        _client.OnClose += (_, _) => Disconnected?.Invoke(this, EventArgs.Empty);
        _client.OnError += (_, e) => ErrorOccurred?.Invoke(this, e.Message);
        _client.OnConnectionFailed += (_, _) =>
            ErrorOccurred?.Invoke(this, "Could not connect to Discord — make sure Discord is running.");

        _client.Initialize();
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
        if (_client == null) return;
        try
        {
            _client.ClearPresence();
            _client.Dispose();
        }
        catch { /* best-effort */ }
        _client = null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        Disconnect();
        _disposed = true;
    }
}
