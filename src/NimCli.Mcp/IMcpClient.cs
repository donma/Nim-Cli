using System.Text.Json;

namespace NimCli.Mcp;

public interface IMcpClient
{
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
    Task<string> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<McpToolDefinition>> ListToolsAsync(CancellationToken cancellationToken = default);
    Task<string> InvokeToolAsync(string toolName, Dictionary<string, object?> arguments, CancellationToken cancellationToken = default);
}

public sealed class NullMcpClient : IMcpClient
{
    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    public Task<string> GetStatusAsync(CancellationToken cancellationToken = default)
        => Task.FromResult("MCP disabled.");

    public Task<IReadOnlyList<McpToolDefinition>> ListToolsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<McpToolDefinition>>([]);

    public Task<string> InvokeToolAsync(string toolName, Dictionary<string, object?> arguments, CancellationToken cancellationToken = default)
        => Task.FromResult($"MCP disabled. Tool '{toolName}' was not invoked.");
}

public sealed class StdioMcpClient : IMcpClient
{
    private readonly NimCli.Infrastructure.Config.McpOptions _options;

    public StdioMcpClient(NimCli.Infrastructure.Config.McpOptions options)
    {
        _options = options;
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Command))
            return false;

        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo(_options.Command, _options.Arguments)
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WorkingDirectory = string.IsNullOrWhiteSpace(_options.WorkingDirectory)
                    ? Directory.GetCurrentDirectory()
                    : _options.WorkingDirectory
            };

            using var process = System.Diagnostics.Process.Start(psi);
            if (process == null)
                return false;

            await Task.Delay(300, cancellationToken);
            if (!process.HasExited)
            {
                process.Kill(true);
                return true;
            }

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Command))
            return "MCP command not configured.";

        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo(_options.Command, _options.Arguments)
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WorkingDirectory = string.IsNullOrWhiteSpace(_options.WorkingDirectory)
                    ? Directory.GetCurrentDirectory()
                    : _options.WorkingDirectory
            };

            using var process = System.Diagnostics.Process.Start(psi);
            if (process == null)
                return "Failed to start MCP process.";

            await process.StandardInput.WriteLineAsync("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{}}".AsMemory(), cancellationToken);
            await process.StandardInput.FlushAsync();

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(2));
            var line = await process.StandardOutput.ReadLineAsync(timeoutCts.Token);

            try { if (!process.HasExited) process.Kill(true); } catch { }

            return string.IsNullOrWhiteSpace(line)
                ? "MCP process started but returned no initialize response."
                : $"MCP initialize response: {line}";
        }
        catch (OperationCanceledException)
        {
            return "MCP initialize timed out.";
        }
        catch (Exception ex)
        {
            return $"MCP initialize failed: {ex.Message}";
        }
    }

    public Task<IReadOnlyList<McpToolDefinition>> ListToolsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<McpToolDefinition> tools =
        [
            new("mcp_status", "Return MCP connection status"),
            new("mcp_echo", "Echo arguments through the MCP client")
        ];

        return Task.FromResult(tools);
    }

    public async Task<string> InvokeToolAsync(string toolName, Dictionary<string, object?> arguments, CancellationToken cancellationToken = default)
    {
        if (toolName == "mcp_status")
            return await GetStatusAsync(cancellationToken);

        return JsonSerializer.Serialize(new
        {
            tool = toolName,
            arguments,
            source = "stdio-mcp"
        }, new JsonSerializerOptions { WriteIndented = true });
    }
}
