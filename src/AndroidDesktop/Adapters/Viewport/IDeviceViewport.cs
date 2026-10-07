using System.Windows;
using System.Text.Json;

namespace AndroidDesktop.Adapters.Viewport;

public interface IDeviceViewport : IAsyncDisposable
{
    FrameworkElement Control { get; }
    event Action<string, JsonElement>? Message;
    Task ConnectAsync(int port, string token, CancellationToken cancellationToken);
    void Send(string kind, object? data = null);
    Task DisconnectAsync();
}
