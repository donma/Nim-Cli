using NimCli.Core;
using NimCli.Mcp;
using Xunit;

namespace NimCli.Core.Tests;

public class McpTests
{
    [Fact]
    public async Task NullMcpClient_Reports_Disabled_State()
    {
        var client = new NullMcpClient();

        Assert.False(await client.IsAvailableAsync());
        Assert.Equal("MCP disabled.", await client.GetStatusAsync());
        Assert.Empty(await client.ListToolsAsync());
    }

    [Fact]
    public async Task McpRegistration_Registers_Proxy_Tools()
    {
        var registry = new ToolRegistry();
        var client = new FakeMcpClient();

        await McpRegistration.RegisterToolsAsync(client, registry);

        Assert.True(registry.Exists("fake_tool"));
        var tool = registry.Get("fake_tool");
        Assert.NotNull(tool);
        var result = await tool!.ExecuteAsync(new Dictionary<string, object?> { ["value"] = 1 });
        Assert.True(result.Success);
        Assert.Contains("fake_tool", result.Output);
    }

    private sealed class FakeMcpClient : IMcpClient
    {
        public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(true);

        public Task<string> GetStatusAsync(CancellationToken cancellationToken = default)
            => Task.FromResult("ok");

        public Task<IReadOnlyList<McpToolDefinition>> ListToolsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<McpToolDefinition>>([new McpToolDefinition("fake_tool", "fake description")]);

        public Task<string> InvokeToolAsync(string toolName, Dictionary<string, object?> arguments, CancellationToken cancellationToken = default)
            => Task.FromResult($"invoked {toolName} with {arguments.Count} arg(s)");
    }
}
