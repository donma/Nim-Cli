using NimCli.Infrastructure.Config;
using NimCli.Mcp;
using Xunit;

namespace NimCli.Integration.Tests;

public sealed class ThirdPartyMcpCompatibilityTests
{
    [Fact]
    public async Task Stdio_Mcp_Client_Works_With_ThirdParty_Like_Echo_Server_Script()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "nimcli-thirdparty-mcp", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        var scriptPath = Path.Combine(tempDirectory, "mock-thirdparty-mcp.ps1");

        try
        {
            File.WriteAllText(scriptPath, """
$line = [Console]::In.ReadLine()
[Console]::Out.WriteLine('{"jsonrpc":"2.0","id":1,"result":{"serverInfo":{"name":"third-party-like"},"protocolVersion":"2025-03-26"}}')
""");

            var client = new StdioMcpClient(new McpOptions
            {
                Enabled = true,
                Command = "pwsh",
                Arguments = $"-NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"{scriptPath}\"",
                WorkingDirectory = tempDirectory
            });

            Assert.True(await client.IsAvailableAsync());
            var status = await client.GetStatusAsync();
            Assert.Contains("MCP initialize response:", status);
            Assert.Contains("third-party-like", status);
        }
        finally
        {
            try { Directory.Delete(tempDirectory, recursive: true); } catch { }
        }
    }
}
