using System.Text.Json;
using NimCli.Infrastructure;
using NimCli.Mcp;

namespace NimCli.App;

public sealed class McpCommandService
{
    private readonly CliRuntimeStore _runtimeStore;
    private readonly IMcpClient _client;

    public McpCommandService(CliRuntimeStore runtimeStore, IMcpClient client)
    {
        _runtimeStore = runtimeStore;
        _client = client;
    }

    public string AddServer(string[] args)
    {
        if (args.Length < 4)
            return "Usage: nim-cli mcp add <name> <command-or-url> [--transport stdio|http] [--env KEY=value] [--scope project|user] [--include-tools a,b]";

        var name = args[2];
        var commandOrUrl = args[3];
        var state = _runtimeStore.LoadState();
        var existing = state.Mcp.Servers.FirstOrDefault(server => server.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
            state.Mcp.Servers.Remove(existing);

        var entry = new McpServerEntry
        {
            Name = name,
            CommandOrUrl = commandOrUrl
        };

        for (var index = 4; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--transport" when index + 1 < args.Length:
                    entry.Transport = args[++index];
                    break;
                case "--scope" when index + 1 < args.Length:
                    entry.Scope = args[++index];
                    break;
                case "--include-tools" when index + 1 < args.Length:
                    entry.IncludedTools = args[++index].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
                    break;
                case "--env" when index + 1 < args.Length:
                    var kvp = args[++index].Split('=', 2);
                    if (kvp.Length == 2)
                        entry.EnvironmentVariables[kvp[0]] = kvp[1];
                    break;
            }
        }

        state.Mcp.Servers.Add(entry);
        _runtimeStore.SaveState(state);
        return $"Added MCP server '{name}'";
    }

    public string RemoveServer(string name)
    {
        var state = _runtimeStore.LoadState();
        var existing = state.Mcp.Servers.FirstOrDefault(server => server.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (existing is null)
            return $"MCP server not found: {name}";

        state.Mcp.Servers.Remove(existing);
        _runtimeStore.SaveState(state);
        return $"Removed MCP server '{name}'";
    }

    public string SetEnabled(string name, bool enabled)
    {
        var state = _runtimeStore.LoadState();
        var existing = state.Mcp.Servers.FirstOrDefault(server => server.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (existing is null)
            return $"MCP server not found: {name}";

        existing.Enabled = enabled;
        _runtimeStore.SaveState(state);
        return $"MCP server '{name}' {(enabled ? "enabled" : "disabled")}";
    }

    public string ListServers(bool includeDescriptions = false, bool includeSchema = false)
    {
        var state = _runtimeStore.LoadState();
        if (state.Mcp.Servers.Count == 0)
            return "No MCP servers configured.";

        var lines = new List<string>();
        foreach (var server in state.Mcp.Servers.OrderBy(server => server.Name, StringComparer.OrdinalIgnoreCase))
        {
            lines.Add($"{server.Name} [{server.Transport}] {(server.Enabled ? "enabled" : "disabled")} -> {server.CommandOrUrl}");
            if (includeDescriptions)
                lines.Add($"  scope={server.Scope}, tools={(server.IncludedTools.Count == 0 ? "all" : string.Join(",", server.IncludedTools))}");
            if (includeSchema)
                lines.Add($"  env={JsonSerializer.Serialize(server.EnvironmentVariables)}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    public string Reload()
        => "Reloaded MCP registry from runtime state.";

    public string Auth(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Configured MCP servers that may require auth:\n" + ListServers(includeDescriptions: true);

        return $"MCP server '{name}' is registered. Interactive OAuth/device auth is not part of this phase; configure required credentials through the server command or environment and verify with 'nim-cli mcp status'.";
    }

    public async Task<string> InspectAsync(string? name)
    {
        var state = _runtimeStore.LoadState();
        if (string.IsNullOrWhiteSpace(name))
            return await BuildClientSummaryAsync();

        var server = state.Mcp.Servers.FirstOrDefault(entry => entry.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (server is null)
            return $"MCP server not found: {name}";

        return string.Join(Environment.NewLine,
        [
            $"Name: {server.Name}",
            $"Transport: {server.Transport}",
            $"Enabled: {server.Enabled}",
            $"Scope: {server.Scope}",
            $"CommandOrUrl: {server.CommandOrUrl}",
            $"IncludedTools: {(server.IncludedTools.Count == 0 ? "all" : string.Join(", ", server.IncludedTools))}",
            $"EnvironmentVariables: {server.EnvironmentVariables.Count}",
            $"ClientStatus: {await _client.GetStatusAsync()}"
        ]);
    }

    public async Task<string> PingAsync()
    {
        var available = await _client.IsAvailableAsync();
        var status = await _client.GetStatusAsync();
        return $"MCP Ping: {(available ? "OK" : "FAIL")}{Environment.NewLine}{status}";
    }

    private async Task<string> BuildClientSummaryAsync()
    {
        var available = await _client.IsAvailableAsync();
        var status = await _client.GetStatusAsync();
        var tools = await _client.ListToolsAsync();
        return string.Join(Environment.NewLine,
        [
            $"Configured Servers: {_runtimeStore.LoadState().Mcp.Servers.Count}",
            $"Client Available: {available}",
            $"Client Status: {status}",
            $"Client Tools: {tools.Count}"
        ]);
    }
}
