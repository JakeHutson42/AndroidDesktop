using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace AndroidDesktop.Adapters.Viewport;

/// <summary>Provisional local WebRTC adapter. Browser decodes media; WPF never copies frames.</summary>
public sealed class WebRtcViewport : IDeviceViewport
{
    private WebView2? _standard;
    private WebView2CompositionControl? _composition;
    private string _mode = "";
    private string _transport = "webrtc";
    private CoreWebView2? _core;
    private readonly string _assets;
    private readonly string? _userData;
    private TaskCompletionSource? _loaded;
    private TaskCompletionSource? _connected;
    private int _allowedPort;
    private bool _processFailed;
    private readonly List<string> _scriptErrors = [];
    public FrameworkElement Control { get; } = new System.Windows.Controls.ContentControl
    {
        HorizontalContentAlignment = HorizontalAlignment.Stretch,
        VerticalContentAlignment = VerticalAlignment.Stretch
    };
    public event Action<string, JsonElement>? Message;
    public WebRtcViewport(string mode, string? userData = null)
    {
        _userData = userData;
        _assets = Path.Combine(AppContext.BaseDirectory, "Assets", "Viewport");
        ConfigureMode(mode);
    }
    public void ConfigureMode(string mode)
    {
        if (_mode == mode && (_standard is not null || _composition is not null)) return;
        if (_core is not null) throw new InvalidOperationException("Restart the application before changing the display control mode.");
        _standard?.Dispose(); _composition?.Dispose(); _standard = null; _composition = null;
        ((System.Windows.Controls.ContentControl)Control).Content = mode == "composition"
            ? _composition = new WebView2CompositionControl() : (FrameworkElement)(_standard = new WebView2());
        if (_standard is not null) _standard.DefaultBackgroundColor = System.Drawing.Color.FromArgb(16, 18, 22);
        if (_composition is not null) _composition.DefaultBackgroundColor = System.Drawing.Color.FromArgb(16, 18, 22);
        _mode = mode;
    }
    public void ConfigureTransport(string transport) => _transport = transport;
    public async Task ConnectAsync(int port, string token, CancellationToken cancellationToken)
    {
        _allowedPort = port;
        await InitializeAsync(cancellationToken);
        _connected = new(TaskCreationOptions.RunContinuationsAsynchronously);
        try { _core!.PostWebMessageAsJson(JsonSerializer.Serialize(new { kind = "connect", port, token, transport = _transport })); }
        catch (System.Runtime.InteropServices.COMException) { _processFailed = true; _connected = null; throw new InvalidOperationException("Display process is unavailable. Connect display again to recreate it."); }
        try { await _connected.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken); }
        catch { Send("cancel"); Send("disconnect"); throw; }
        finally { _connected = null; }
    }
    public string? BrowserVersion => _core?.Environment.BrowserVersionString;
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (_processFailed) {
            _core = null; _loaded = null; _scriptErrors.Clear();
            var mode = _mode; _mode = ""; ConfigureMode(mode); _processFailed = false;
        }
        if (_core is null)
        {
            ConfigureMode(_mode);
            if (!File.Exists(Path.Combine(_assets, "viewport.js"))) throw new FileNotFoundException("Packaged display assets are missing. Repair the application installation.");
            var userData = _userData ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AndroidDesktop", "WebView2");
            var environment = await CoreWebView2Environment.CreateAsync(null, userData);
            if (_standard is not null) { await _standard.EnsureCoreWebView2Async(environment); _core = _standard.CoreWebView2; }
            else { await _composition!.EnsureCoreWebView2Async(environment); _core = _composition.CoreWebView2; }
            _core.Settings.AreDefaultContextMenusEnabled = false;
            _core.Settings.AreDevToolsEnabled = Environment.GetEnvironmentVariable("ANDROID_DESKTOP_DEVTOOLS") == "1";
            _core.Settings.IsStatusBarEnabled = false;
            _core.Settings.IsGeneralAutofillEnabled = false;
            _core.Settings.IsPasswordAutosaveEnabled = false;
            await _core.CallDevToolsProtocolMethodAsync("Runtime.enable", "{}");
            _core.GetDevToolsProtocolEventReceiver("Runtime.exceptionThrown").DevToolsProtocolEventReceived += (_, e) => {
                _scriptErrors.Add(e.ParameterObjectAsJson);
                if (_scriptErrors.Count > 8) _scriptErrors.RemoveAt(0);
            };
            _core.SetVirtualHostNameToFolderMapping("android-desktop.local", _assets, CoreWebView2HostResourceAccessKind.DenyCors);
            _core.NavigationStarting += (_, e) => e.Cancel = e.Uri != "https://android-desktop.local/index.html";
            _core.FrameNavigationStarting += (_, e) => e.Cancel = true;
            _core.NewWindowRequested += (_, e) => e.Handled = true;
            _core.PermissionRequested += (_, e) => e.State = CoreWebView2PermissionState.Deny;
            _core.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
            _core.WebResourceRequested += (_, e) => {
                if (!Uri.TryCreate(e.Request.Uri, UriKind.Absolute, out var uri) ||
                    (uri.Host != "android-desktop.local" && !(uri.Host == "127.0.0.1" && uri.Port == _allowedPort && uri.Scheme == "http")))
                    e.Response = environment.CreateWebResourceResponse(null, 403, "Blocked", "");
            };
            _core.WebMessageReceived += (_, e) => {
                if (e.Source != "https://android-desktop.local/index.html") return;
                try {
                    using var document = JsonDocument.Parse(e.WebMessageAsJson);
                    var kind = document.RootElement.GetProperty("kind").GetString()!;
                    if (kind == "loaded") _loaded?.TrySetResult();
                    var data = document.RootElement.GetProperty("data");
                    if (kind == "inputReady" && data.ValueKind == JsonValueKind.True) _connected?.TrySetResult();
                    if (kind is "fault" or "disconnected") _connected?.TrySetException(new InvalidOperationException("Display connection failed. Stop or reconnect; Android data is retained."));
                    Message?.Invoke(kind, data.Clone());
                } catch (JsonException) { }
            };
            _core.ProcessFailed += (_, e) => {
                _processFailed = true;
                _connected?.TrySetException(new InvalidOperationException("WebView2 failed. Reconnect the display."));
                _loaded?.TrySetException(new InvalidOperationException("WebView2 failed. Use Connect display to recreate the viewport."));
                Message?.Invoke("fault", JsonSerializer.SerializeToElement("WebView2 process failed: " + e.ProcessFailedKind));
            };
            _loaded = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _core.Navigate("https://android-desktop.local/index.html");
        }
        try { await _loaded!.Task.WaitAsync(TimeSpan.FromSeconds(15), cancellationToken); }
        catch (TimeoutException) {
            var page = await _core.ExecuteScriptAsync("JSON.stringify({url:location.href,ready:document.readyState,bridge:!!window.chrome?.webview})");
            throw new InvalidOperationException("Packaged viewport did not signal readiness. Page=" + page + "; Script errors=" + string.Join(";", _scriptErrors));
        }
    }
    public void Send(string kind, object? data = null)
    {
        if (_core is null || _processFailed) return;
        // Only a closed message vocabulary is accepted by the packaged page.
        try { _core.PostWebMessageAsJson(kind == "rotation" ? JsonSerializer.Serialize(new { kind, degrees = data }) : JsonSerializer.Serialize(new { kind, data })); }
        catch (Exception failure) when (failure is System.Runtime.InteropServices.COMException or InvalidOperationException) {
            _processFailed = true;
            Message?.Invoke("fault", JsonSerializer.SerializeToElement("Display process unavailable. Use Connect display to recover."));
        }
    }
    public async Task DisconnectAsync()
    {
        Send("disconnect");
        var environment = _core?.Environment;
        var exited = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void BrowserExited(object? sender, CoreWebView2BrowserProcessExitedEventArgs args) => exited.TrySetResult();
        if (environment is not null) environment.BrowserProcessExited += BrowserExited;
        // Closing the view releases its browser/rendering resources. Recreate lazily on connect.
        _core = null; _loaded = null;
        _standard?.Dispose(); _composition?.Dispose();
        _standard = null; _composition = null;
        ((System.Windows.Controls.ContentControl)Control).Content = null;
        _scriptErrors.Clear(); _processFailed = false;
        if (environment is not null)
        {
            try { await exited.Task.WaitAsync(TimeSpan.FromSeconds(10)); }
            catch (TimeoutException) { Message?.Invoke("fault", JsonSerializer.SerializeToElement("Browser resource release was not confirmed within ten seconds.")); }
            finally { environment.BrowserProcessExited -= BrowserExited; }
        }
    }
    public async Task CapturePreviewAsync(string path)
    {
        if (_core is null) throw new InvalidOperationException("Viewport is not initialized.");
        await using var output = File.Create(path);
        await _core.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, output);
    }
    public async Task CaptureDecodedFrameAsync(string path)
    {
        if (_core is null) throw new InvalidOperationException("Viewport is not initialized.");
        var json = await _core.ExecuteScriptAsync("(()=>{const v=document.getElementById('display');if(!v.videoWidth||!v.videoHeight)throw new Error('No decoded Android frame');const c=document.createElement('canvas');c.width=v.videoWidth;c.height=v.videoHeight;c.getContext('2d').drawImage(v,0,0);return c.toDataURL('image/png')})()");
        var data = JsonSerializer.Deserialize<string>(json) ?? throw new InvalidOperationException("No decoded Android frame available.");
        await File.WriteAllBytesAsync(path, Convert.FromBase64String(data[(data.IndexOf(',') + 1)..]));
    }
    public async Task DiagnosticTapAsync()
    {
        if (_core is null) throw new InvalidOperationException("Viewport is not initialized.");
        var bounds = await _core.ExecuteScriptAsync("(()=>{const r=document.getElementById('display').getBoundingClientRect();return {x:r.x+r.width/2,y:r.y+r.height/2}})()");
        using var document = JsonDocument.Parse(bounds);
        var x = document.RootElement.GetProperty("x").GetDouble();
        var y = document.RootElement.GetProperty("y").GetDouble();
        await _core.CallDevToolsProtocolMethodAsync("Input.dispatchMouseEvent", JsonSerializer.Serialize(new { type = "mousePressed", x, y, button = "left", clickCount = 1 }));
        await Task.Delay(100);
        await _core.CallDevToolsProtocolMethodAsync("Input.dispatchMouseEvent", JsonSerializer.Serialize(new { type = "mouseReleased", x, y, button = "left", clickCount = 1 }));
    }
    public async ValueTask DisposeAsync() => await DisconnectAsync();
}
