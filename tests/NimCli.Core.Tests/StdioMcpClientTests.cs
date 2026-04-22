using NimCli.Infrastructure.Config;
using NimCli.Mcp;
using Xunit;

namespace NimCli.Core.Tests;

public sealed class StdioMcpClientTests
{
    [Fact]
    public async Task Reports_Not_Configured_When_Command_Is_Missing()
    {
        var client = new StdioMcpClient(new McpOptions());

        Assert.False(await client.IsAvailableAsync());
        Assert.Equal("MCP command not configured.", await client.GetStatusAsync());
    }

    [Fact]
    public async Task Returns_Initialize_Response_For_Controlled_Stdio_Process()
    {
        var client = new StdioMcpClient(new McpOptions
        {
            Command = "pwsh",
            Arguments = "-NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"$line = [Console]::In.ReadLine(); [Console]::Out.WriteLine('{\\\"jsonrpc\\\":\\\"2.0\\\",\\\"id\\\":1,\\\"result\\\":{\\\"serverInfo\\\":{\\\"name\\\":\\\"demo\\\"}}}');\""
        });

        var available = await client.IsAvailableAsync();
        var status = await client.GetStatusAsync();
        var tools = await client.ListToolsAsync();
        var invokeResult = await client.InvokeToolAsync("mcp_echo", new Dictionary<string, object?> { ["value"] = 1 });

        Assert.True(available);
        Assert.Contains("MCP initialize response:", status);
        Assert.Contains("serverInfo", status);
        Assert.Equal(2, tools.Count);
        Assert.Contains("mcp_echo", invokeResult);
        Assert.Contains("stdio-mcp", invokeResult);
    }

    [Fact]
    public async Task Returns_Failure_Status_For_Invalid_Command()
    {
        var client = new StdioMcpClient(new McpOptions
        {
            Command = "__nimcli_missing_mcp_command__"
        });

        Assert.False(await client.IsAvailableAsync());
        Assert.Contains("MCP initialize failed:", await client.GetStatusAsync());
    }
}
