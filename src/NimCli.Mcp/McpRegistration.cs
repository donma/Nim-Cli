using System.Text.Json;
using NimCli.Core;
using NimCli.Tools.Abstractions;

namespace NimCli.Mcp;

public sealed record McpToolDefinition(string Name, string Description, JsonElement? InputSchema = null);

public static class McpRegistration
{
    public static async Task RegisterToolsAsync(IMcpClient client, ToolRegistry registry, CancellationToken cancellationToken = default)
    {
        var tools = await client.ListToolsAsync(cancellationToken);
        foreach (var tool in tools)
            registry.Register(new McpProxyTool(client, tool));
    }
}

public sealed class McpProxyTool : ITool
{
    private readonly IMcpClient _client;
    private readonly McpToolDefinition _definition;

    public McpProxyTool(IMcpClient client, McpToolDefinition definition)
    {
        _client = client;
        _definition = definition;
    }

    public string Name => _definition.Name;
    public string Description => _definition.Description;
    public RiskLevel RiskLevel => RiskLevel.Low;
    public object InputSchema => _definition.InputSchema.HasValue
        ? _definition.InputSchema.Value
        : new { type = "object", properties = new { } };

    public async Task<ToolExecuteResult> ExecuteAsync(Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var output = await _client.InvokeToolAsync(_definition.Name, input, cancellationToken);
        return new ToolExecuteResult(true, output);
    }
}
