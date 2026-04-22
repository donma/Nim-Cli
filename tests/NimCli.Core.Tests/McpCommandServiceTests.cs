using NimCli.App;
using NimCli.Infrastructure;
using NimCli.Mcp;
using NimCli.Infrastructure.Config;
using Xunit;

namespace NimCli.Core.Tests;

public sealed class McpCommandServiceTests : IDisposable
{
    private readonly string _originalDirectory;
    private readonly string _tempDirectory;
    private readonly string? _originalHome;

    public McpCommandServiceTests()
    {
        _originalDirectory = Directory.GetCurrentDirectory();
        _originalHome = Environment.GetEnvironmentVariable("NIMCLI_HOME");
        _tempDirectory = Path.Combine(Path.GetTempPath(), "nimcli-mcpcommand-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
        Directory.SetCurrentDirectory(_tempDirectory);
        Environment.SetEnvironmentVariable("NIMCLI_HOME", Path.Combine(_tempDirectory, "home"));
    }

    [Fact]
    public async Task Add_List_Inspect_Summary_And_Ping_Expose_Mcp_State_And_Client_Status()
    {
        var service = new McpCommandService(new CliRuntimeStore(), new FakeMcpClient());

        var addResult = service.AddServer(["mcp", "add", "demo", "npx @demo/server", "--transport", "stdio", "--scope", "project", "--include-tools", "mcp_echo", "--env", "TOKEN=masked"]);
        var listResult = service.ListServers(includeDescriptions: true, includeSchema: true);
        var inspectResult = await service.InspectAsync(null);
        var pingResult = await service.PingAsync();

        Assert.Contains("Added MCP server 'demo'", addResult);
        Assert.Contains("demo [stdio] enabled -> npx @demo/server", listResult);
        Assert.Contains("scope=project, tools=mcp_echo", listResult);
        Assert.Contains("TOKEN", listResult);
        Assert.Contains("Client Status: fake-mcp ok", inspectResult);
        Assert.Contains("Client Tools: 2", inspectResult);
        Assert.Contains("MCP Ping: OK", pingResult);
        Assert.Contains("fake-mcp ok", pingResult);
    }

    [Fact]
    public async Task Inspect_Without_Name_Returns_Client_Summary_With_Tool_Count()
    {
        var service = new McpCommandService(new CliRuntimeStore(), new FakeMcpClient());

        var result = await service.InspectAsync(null);

        Assert.Contains("Configured Servers: 0", result);
        Assert.Contains("Client Available: True", result);
        Assert.Contains("Client Status: fake-mcp ok", result);
        Assert.Contains("Client Tools: 2", result);
    }

    public void Dispose()
    {
        if (Directory.Exists(_originalDirectory))
            Directory.SetCurrentDirectory(_originalDirectory);

        Environment.SetEnvironmentVariable("NIMCLI_HOME", _originalHome);

        try
        {
            if (Directory.Exists(_tempDirectory))
                Directory.Delete(_tempDirectory, recursive: true);
        }
        catch
        {
        }
    }

    private sealed class FakeMcpClient : IMcpClient
    {
        public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(true);

        public Task<string> GetStatusAsync(CancellationToken cancellationToken = default)
            => Task.FromResult("fake-mcp ok");

        public Task<IReadOnlyList<McpToolDefinition>> ListToolsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<McpToolDefinition>>(
            [
                new McpToolDefinition("mcp_status", "status"),
                new McpToolDefinition("mcp_echo", "echo")
            ]);

        public Task<string> InvokeToolAsync(string toolName, Dictionary<string, object?> arguments, CancellationToken cancellationToken = default)
            => Task.FromResult($"{toolName}:{arguments.Count}");
    }
}
