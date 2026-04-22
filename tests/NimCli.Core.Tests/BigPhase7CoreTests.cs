using NimCli.App;
using NimCli.Core;
using NimCli.Infrastructure;
using NimCli.Infrastructure.Config;
using NimCli.Tools.Abstractions;
using NimCli.Tools.Browser;
using NimCli.Tools.Db;
using NimCli.Tools.Ftp;
using NimCli.Tools.Git;
using NimCli.Tools.Shell;
using Xunit;

namespace NimCli.Core.Tests;

public sealed class BigPhase7CoreTests
{
    [Fact]
    public async Task GitPushTool_DryRun_Uses_Git_DryRun_Flag()
    {
        var shell = new RecordingShellProvider();
        var tool = new GitPushTool(shell);

        var result = await tool.ExecuteAsync(new Dictionary<string, object?>
        {
            ["remote"] = "origin",
            ["branch"] = "main",
            ["dry_run"] = true
        });

        Assert.True(result.Success);
        Assert.NotNull(shell.LastCommand);
        Assert.Contains("--dry-run", shell.LastCommand, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FtpUploadTool_DryRun_Does_Not_Require_Network_Call()
    {
        var tool = new FtpUploadTool();
        var file = Path.GetTempFileName();
        try
        {
            var result = await tool.ExecuteAsync(new Dictionary<string, object?>
            {
                ["host"] = "ftp.example.com",
                ["username"] = "user",
                ["password"] = "secret",
                ["local_path"] = file,
                ["remote_path"] = "/incoming/file.txt",
                ["dry_run"] = true
            });

            Assert.True(result.Success);
            Assert.Contains("FTP dry-run OK", result.Output);
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public async Task RunProjectTool_Timeout_Returns_Failure()
    {
        var shell = new FakeShellProvider(new ShellResult(-1, "partial", "timeout", TimedOut: true));
        var tool = new RunProjectTool(shell);

        var result = await tool.ExecuteAsync(new Dictionary<string, object?>
        {
            ["args"] = "--help"
        });

        Assert.False(result.Success);
        Assert.Equal("Run timed out", result.ErrorMessage);
        Assert.Contains("timed out", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PowerShellCommandBuilder_Quotes_Single_Quotes_Safely()
    {
        var command = PowerShellCommandBuilder.BuildExternalCommand("git", ["commit", "-m", "fix user's path"]);

        Assert.Contains("'fix user''s path'", command, StringComparison.Ordinal);
    }

    [Fact]
    public async Task QueryDbTool_Rejects_Unsafe_Structured_Where_Clause()
    {
        var tool = new QueryDbTool();
        var result = await tool.ExecuteAsync(new Dictionary<string, object?>
        {
            ["connection_string"] = "Data Source=:memory:",
            ["table"] = "Users",
            ["where"] = "1=1; DROP TABLE Users",
            ["db_type"] = "sqlite"
        });

        Assert.False(result.Success);
        Assert.Contains("unsupported", result.ErrorMessage ?? result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ContextBuilder_Truncation_Preserves_Recent_Sections_Before_Notice()
    {
        var session = new SessionState();
        session.RecordCurrentTask("phase7 context hardening");
        session.AddRecentAction("recent-action");
        session.RecordRepoMap(new string('a', 4000));
        session.RecordShellResult("dotnet build", new string('b', 4000));

        var builder = new ContextBuilder();
        builder.AddSessionState(session, AgentMode.Coding);
        var context = builder.Build(1200);

        Assert.Contains("[Context Strategy]", context);
        Assert.Contains("[Current Task]", context);
        Assert.Contains("[Context Audit]", context);
    }

    [Fact]
    public async Task BrowserSessionManager_Serializes_Concurrent_Access()
    {
        var manager = new BrowserSessionManager();
        var order = new List<int>();

        await Task.WhenAll(
            manager.SerializeAsync(async _ =>
            {
                order.Add(1);
                await Task.Delay(50);
                order.Add(2);
                return 0;
            }),
            manager.SerializeAsync(async _ =>
            {
                order.Add(3);
                await Task.Delay(10);
                order.Add(4);
                return 0;
            }));

        Assert.True(order.SequenceEqual([1, 2, 3, 4]) || order.SequenceEqual([3, 4, 1, 2]));
    }

    [Fact]
    public void UserConfigStore_Uses_UserLevel_Home_And_Workspace_Override()
    {
        var original = Environment.GetEnvironmentVariable("NIMCLI_HOME");
        var tempHome = Path.Combine(Path.GetTempPath(), "nimcli-home-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempHome);

        try
        {
            Environment.SetEnvironmentVariable("NIMCLI_HOME", tempHome);
            Assert.Equal(tempHome, UserConfigStore.AppHomeDirectory);
            Assert.Equal(Path.Combine(tempHome, "appsettings.secret.json"), UserConfigStore.ConfigFilePath);
        }
        finally
        {
            Environment.SetEnvironmentVariable("NIMCLI_HOME", original);
            try { Directory.Delete(tempHome, recursive: true); } catch { }
        }
    }

    private sealed class FakeShellProvider : IShellProvider
    {
        private readonly ShellResult _result;

        public FakeShellProvider(ShellResult result)
        {
            _result = result;
        }

        public Task<ShellResult> ExecuteAsync(string command, string? workingDir = null, int timeoutSeconds = 60, CancellationToken cancellationToken = default)
            => Task.FromResult(_result);
    }

    private sealed class RecordingShellProvider : IShellProvider
    {
        public string? LastCommand { get; private set; }

        public Task<ShellResult> ExecuteAsync(string command, string? workingDir = null, int timeoutSeconds = 60, CancellationToken cancellationToken = default)
        {
            LastCommand = command;
            return Task.FromResult(new ShellResult(0, "ok", string.Empty));
        }
    }
}
