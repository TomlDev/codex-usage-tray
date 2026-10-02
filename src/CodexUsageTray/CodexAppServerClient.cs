using System.Diagnostics;
using System.Text.Json;
using CodexUsageTray.Core;

namespace CodexUsageTray;

internal sealed class CodexAppServerClient : IAsyncDisposable
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private Process? process;
    private StreamWriter? input;
    private StreamReader? output;
    private long requestId;

    public async Task<UsageSnapshot> ReadUsageAsync(CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            await EnsureStartedAsync(cancellationToken);
            using var result = await RequestAsync("account/rateLimits/read", null, cancellationToken);
            return UsageParser.Parse(result.RootElement, DateTimeOffset.Now);
        }
        catch
        {
            Stop();
            throw;
        }
        finally { gate.Release(); }
    }

    private async Task EnsureStartedAsync(CancellationToken cancellationToken)
    {
        if (process is { HasExited: false }) return;
        var start = new ProcessStartInfo
        {
            // via cmd.exe, damit auch der npm-Shim codex.cmd gefunden wird
            FileName = "cmd.exe",
            Arguments = "/c codex app-server --listen stdio://",
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        process = Process.Start(start) ?? throw new InvalidOperationException("Codex CLI konnte nicht gestartet werden.");
        input = process.StandardInput;
        output = process.StandardOutput;
        using var initialized = await RequestAsync("initialize", new
        {
            clientInfo = new { name = "codex-usage-tray", title = "Codex Usage Tray", version = "0.1.0" },
            capabilities = new { optOutNotificationMethods = Array.Empty<string>() }
        }, cancellationToken);
        await SendAsync(new { method = "initialized" }, cancellationToken);
    }

    private async Task<JsonDocument> RequestAsync(string method, object? parameters, CancellationToken cancellationToken)
    {
        var id = Interlocked.Increment(ref requestId);
        await SendAsync(new { method, id, @params = parameters }, cancellationToken);
        while (true)
        {
            var line = await output!.ReadLineAsync(cancellationToken) ?? throw new IOException("Codex app-server hat die Verbindung geschlossen.");
            var document = JsonDocument.Parse(line);
            var root = document.RootElement;
            if (!root.TryGetProperty("id", out var responseId) || responseId.GetInt64() != id)
            {
                document.Dispose();
                continue;
            }
            if (root.TryGetProperty("error", out var error))
            {
                var message = error.TryGetProperty("message", out var m) ? m.GetString() : error.ToString();
                document.Dispose();
                throw new InvalidOperationException(message ?? "Codex hat einen Fehler gemeldet.");
            }
            var result = root.GetProperty("result").Clone();
            document.Dispose();
            return JsonDocument.Parse(result.GetRawText());
        }
    }

    private Task SendAsync(object message, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(message, new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull });
        return SendLineAsync(json, cancellationToken);
    }

    private async Task SendLineAsync(string json, CancellationToken cancellationToken)
    {
        await input!.WriteLineAsync(json.AsMemory(), cancellationToken);
        await input.FlushAsync(cancellationToken);
    }

    private void Stop()
    {
        try { if (process is { HasExited: false }) process.Kill(true); } catch { }
        process?.Dispose(); process = null; input = null; output = null;
    }

    public ValueTask DisposeAsync() { Stop(); gate.Dispose(); return ValueTask.CompletedTask; }
}
