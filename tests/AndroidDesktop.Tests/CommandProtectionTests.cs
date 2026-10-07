using AndroidDesktop.Adapters.Viewport;
using AndroidDesktop.Models;
using AndroidDesktop.Services;
using AndroidDesktop.ViewModels;
using System.Text.Json;
using System.Windows;

namespace AndroidDesktop.Tests;

public class CommandProtectionTests
{
    [Fact] public async Task BusySessionDisablesImportAndLaunchAndRejectsDirectDropWithoutChangingSelection()
    {
        var tools = new CountingTools(); var viewport = new FakeViewport();
        var model = new PrototypeViewModel(new(new DesktopSettings().Runtime, tools, new()), viewport, new());
        var selected = new ApkSelection(new("game.apk", "example.game", 26, [], "hash"), "device", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        model.RestoreSelection(selected);
        model.RestoreLibrary([selected]); model.LibrarySelection = selected;
        Assert.True(model.LaunchLibraryCommand.CanExecute(null));
        Assert.True(model.OpenApkCommand.CanExecute(null)); Assert.True(model.LaunchCommand.CanExecute(null));
        model.Busy = true;
        Assert.False(model.OpenApkCommand.CanExecute(null)); Assert.False(model.LaunchCommand.CanExecute(null));
        Assert.False(model.RelaunchCommand.CanExecute(null)); Assert.False(model.ImportFilesCommand.CanExecute(new[] { "game.apk" }));
        Assert.False(model.LaunchLibraryCommand.CanExecute(null)); Assert.False(model.LocateLibraryCommand.CanExecute(null));
        await model.LaunchLibraryCommand.ExecuteAsync(null);
        Assert.Single(model.Library); Assert.Same(selected, model.Library[0]);
        await model.ImportFilesAsync(["game.apk"], CancellationToken.None);
        Assert.Same(selected, model.Selection); Assert.Equal(0, tools.Calls);
        model.Busy = false; Assert.True(model.OpenApkCommand.CanExecute(null));
    }
    [Fact] public async Task ForceTerminationRequiresFailedGracefulShutdown()
    {
        var tools = new CountingTools(); var session = new EmulatorSessionService(new DesktopSettings().Runtime, tools, new());
        Assert.False(session.ForceStopAvailable);
        await Assert.ThrowsAsync<InvalidOperationException>(() => session.ForceStopAsync(CancellationToken.None));
        Assert.Equal(0, tools.Calls); Assert.False(session.HasOwnedProcessHandles);
    }
    [Theory][InlineData(true,false)][InlineData(false,true)]
    public async Task ActiveAutomationPreventsConflictingPackageAndConnectionCommands(bool recording,bool playback)
    {
        var tools = new CountingTools(); var model = new PrototypeViewModel(new(new DesktopSettings().Runtime,tools,new()),new FakeViewport(),new());
        var selected = new ApkSelection(new("game.apk", "example.game", 26, [], "hash"), "device", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        model.RestoreLibrary([selected]); model.LibrarySelection = selected;
        model.RecordingActive=recording;model.PlaybackActive=playback;
        Assert.False(model.OpenApkCommand.CanExecute(null));Assert.False(model.ConnectCommand.CanExecute(null));Assert.False(model.AutomationEditingEnabled);
        Assert.False(model.OpenRecordingCommand.CanExecute(null));Assert.False(model.RecoverRecordingCommand.CanExecute(null));
        Assert.False(model.LaunchLibraryCommand.CanExecute(null)); Assert.False(model.LocateLibraryCommand.CanExecute(null));
        Assert.Equal(0,tools.Calls);await Task.CompletedTask;
    }
    private sealed class CountingTools : IAndroidToolService
    {
        public int Calls;
        public Task<ToolResult> RunAsync(string executable, IEnumerable<string> arguments, TimeSpan timeout, CancellationToken token, IDictionary<string, string>? environment = null)
        { Calls++; return Task.FromResult(new ToolResult(0, "", "")); }
    }
    [Fact] public async Task MissingToolsFailActionablyWithoutLaunchingOrDiscardingConfiguration()
    {
        var tools = new CountingTools(); var options = new DesktopSettings().Runtime with { SdkRoot = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString()) };
        var session = new EmulatorSessionService(options, tools, new());
        var error = await Assert.ThrowsAsync<System.IO.FileNotFoundException>(() => session.StartAsync(CancellationToken.None));
        Assert.Contains("Missing Android SDK tool", error.Message);
        Assert.Equal(SessionState.Faulted, session.State); Assert.Equal(0, tools.Calls);
        Assert.False(session.HasOwnedProcessHandles); Assert.Equal(options.AvdHome, session.Options.AvdHome);
        session.Configure(options); Assert.Equal(SessionState.Stopped, session.State);
    }
    private sealed class FakeViewport : IDeviceViewport
    {
        public FrameworkElement Control => throw new NotSupportedException("No UI rendering in command tests.");
        public event Action<string, JsonElement>? Message;
        public void Emit(string kind, object data) => Message?.Invoke(kind, JsonSerializer.SerializeToElement(data));
        public Task ConnectAsync(int port, string token, CancellationToken cancellationToken) => Task.CompletedTask;
        public void Send(string kind, object? data = null) { }
        public Task DisconnectAsync() => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
