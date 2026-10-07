using System.Diagnostics;
using System.IO;
using System.Text;

namespace AndroidDesktop.Services;

public sealed record ToolResult(int ExitCode, string Output, string Error)
{
    public string RequireSuccess()
    {
        if (ExitCode != 0) throw new InvalidOperationException($"Tool failed ({ExitCode}): {Error}\n{Output}");
        return Output.Trim();
    }
}

public interface IAndroidToolService
{
    Task<ToolResult> RunAsync(string executable, IEnumerable<string> arguments, TimeSpan timeout,
        CancellationToken cancellationToken, IDictionary<string, string>? environment = null);
    Task<ToolResult> RunWithInputAsync(string executable, IEnumerable<string> arguments, string input,
        TimeSpan timeout, CancellationToken cancellationToken, IDictionary<string, string>? environment = null)
        => throw new NotSupportedException("This tool runner does not support standard input.");
}

public sealed class AndroidToolService : IAndroidToolService
{
    public static ProcessStartInfo CreateStartInfo(string executable, IEnumerable<string> arguments,
        IDictionary<string, string>? environment = null)
    {
        var info = new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in arguments) info.ArgumentList.Add(argument);
        if (environment is not null) foreach (var pair in environment) info.Environment[pair.Key] = pair.Value;
        return info;
    }

    public async Task<ToolResult> RunAsync(string executable, IEnumerable<string> arguments, TimeSpan timeout,
        CancellationToken cancellationToken, IDictionary<string, string>? environment = null)
        => await RunCoreAsync(executable, arguments, null, timeout, cancellationToken, environment);

    public Task<ToolResult> RunWithInputAsync(string executable, IEnumerable<string> arguments, string input,
        TimeSpan timeout, CancellationToken cancellationToken, IDictionary<string, string>? environment = null)
        => RunCoreAsync(executable, arguments, input, timeout, cancellationToken, environment);

    private async Task<ToolResult> RunCoreAsync(string executable, IEnumerable<string> arguments, string? input, TimeSpan timeout,
        CancellationToken cancellationToken, IDictionary<string, string>? environment)
    {
        using var process = new Process { StartInfo = CreateStartInfo(executable, arguments, environment) };
        process.StartInfo.RedirectStandardInput = input is not null;
        process.Start();
        var output = DrainAsync(process.StandardOutput);
        var error = DrainAsync(process.StandardError);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(timeout);
        try {
            if (input is not null) { await process.StandardInput.WriteAsync(input.AsMemory(), deadline.Token); process.StandardInput.Close(); }
            await process.WaitForExitAsync(deadline.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true); // Only this owned tool invocation.
            await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
            await Task.WhenAll(output, error).ConfigureAwait(false);
            if (cancellationToken.IsCancellationRequested) throw;
            throw new TimeoutException($"{Path.GetFileName(executable)} timed out after {timeout.TotalSeconds:0} seconds.");
        }
        return new ToolResult(process.ExitCode, await output.ConfigureAwait(false), await error.ConfigureAwait(false));
    }

    private static async Task<string> DrainAsync(StreamReader reader)
    {
        // Keep draining even when the diagnostic tail reaches its bound.
        var text = new StringBuilder();
        var buffer = new char[4096];
        int read;
        while ((read = await reader.ReadAsync(buffer).ConfigureAwait(false)) > 0)
        {
            text.Append(buffer, 0, read);
            if (text.Length > 262144) text.Remove(0, text.Length - 262144);
        }
        return text.ToString();
    }
}

public sealed class OwnedProcess : IDisposable
{
    public Process Process { get; }
    public int Id => Process.Id;
    private readonly Task _stdout, _stderr;
    private readonly Queue<string> _tail = new();
    private readonly object _gate = new();
    public OwnedProcess(ProcessStartInfo startInfo)
    {
        Process = new Process { StartInfo = startInfo };
        Process.Start();
        _stdout = DrainAsync(Process.StandardOutput);
        _stderr = DrainAsync(Process.StandardError);
    }
    private async Task DrainAsync(StreamReader reader)
    {
        var buffer = new char[2048];
        int length;
        while ((length = await reader.ReadAsync(buffer).ConfigureAwait(false)) > 0)
        {
            lock (_gate) { _tail.Enqueue(new string(buffer, 0, length)); while (_tail.Count > 32) _tail.Dequeue(); }
        }
    }
    public string Tail { get { lock (_gate) return string.Concat(_tail); } }
    public async Task WaitAsync(CancellationToken token)
    {
        await Process.WaitForExitAsync(token);
        await Task.WhenAll(_stdout, _stderr);
    }
    public async Task ForceStopAsync()
    {
        if (!Process.HasExited) Process.Kill(entireProcessTree: true);
        await WaitAsync(CancellationToken.None);
    }
    public void Dispose() => Process.Dispose();
}
