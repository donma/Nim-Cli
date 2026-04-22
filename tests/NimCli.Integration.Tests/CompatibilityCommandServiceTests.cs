using NimCli.App;
using NimCli.Core;
using NimCli.Infrastructure;
using NimCli.Infrastructure.Config;
using NimCli.Tools.Shell;
using Xunit;

namespace NimCli.Integration.Tests;

public class CompatibilityCommandServiceTests : IDisposable
{
    private readonly string _originalDirectory;
    private readonly string _tempDirectory;

    public CompatibilityCommandServiceTests()
    {
        _originalDirectory = Directory.GetCurrentDirectory();
        _tempDirectory = Path.Combine(Path.GetTempPath(), "nimcli-compatibility-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
        Directory.SetCurrentDirectory(_tempDirectory);
    }

    [Fact]
    public void Restore_List_Reports_Available_Checkpoints()
    {
        var sessionManager = new SessionManager(new CliRuntimeStore());
        var session = new SessionState();
        sessionManager.InitializeNewSession(session, _tempDirectory, []);
        session.AddUserMessage("檢查 restore list");
        session.AddToolResultMessage("build_project", "build ok");
        session.RecordBuildSummary("build ok");
        sessionManager.SaveCheckpoint(session, "alpha");

        var service = CreateService(sessionManager);
        var result = service.HandleRestore(session, ["list"], rewind: false);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("可還原的 checkpoint", result.Output);
        Assert.Contains("alpha", result.Output);
        Assert.Contains("訊息=2", result.Output);
    }

    [Fact]
    public void Rewind_Show_Returns_Checkpoint_Details()
    {
        var sessionManager = new SessionManager(new CliRuntimeStore());
        var session = new SessionState();
        sessionManager.InitializeNewSession(session, _tempDirectory, []);
        session.AddUserMessage("檢查 rewind show");
        session.RecordBuildSummary("dotnet build 成功");
        session.RecordTestSummary("dotnet test 成功");
        session.RecordRepoMap("repo map ready");
        sessionManager.SaveCheckpoint(session, "beta");

        var service = CreateService(sessionManager);
        var result = service.HandleRestore(session, ["show", "beta"], rewind: true);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Checkpoint: beta", result.Output);
        Assert.Contains("最近 build: dotnet build 成功", result.Output);
        Assert.Contains("最近 test: dotnet test 成功", result.Output);
        Assert.Contains("執行方式: nim-cli rewind beta", result.Output);
    }

    [Fact]
    public void Restore_By_Index_Restores_Selected_Checkpoint()
    {
        var sessionManager = new SessionManager(new CliRuntimeStore());
        var session = new SessionState();
        sessionManager.InitializeNewSession(session, _tempDirectory, []);

        session.AddUserMessage("第一個 checkpoint");
        sessionManager.SaveCheckpoint(session, "first");

        session.AddUserMessage("第二個 checkpoint");
        sessionManager.SaveCheckpoint(session, "second");

        session.Clear();
        session.SetWorkspaceDirectories([session.WorkingDirectory]);

        var service = CreateService(sessionManager);
        var result = service.HandleRestore(session, ["2"], rewind: false);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("已還原 checkpoint 'first'", result.Output);
        Assert.Contains(session.ConversationHistory, message => message.Content.Contains("第一個 checkpoint", StringComparison.Ordinal));
    }

    public void Dispose()
    {
        Directory.SetCurrentDirectory(_originalDirectory);

        try
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
        catch
        {
        }
    }

    private static CompatibilityCommandService CreateService(SessionManager sessionManager)
    {
        var options = new NimCliOptions();
        var shellProvider = new FakeShellProvider();
        var workspace = new WorkspaceCommandService(new CliRuntimeStore(), shellProvider);
        var registry = new ToolRegistry();
        var policy = new ToolPolicyService(options);
        var policySummary = new PolicySummaryService(registry, policy);
        var commandCatalog = new CommandCatalogService();
        return new CompatibilityCommandService(sessionManager, workspace, options, shellProvider, policySummary, commandCatalog);
    }

    private sealed class FakeShellProvider : IShellProvider
    {
        public Task<ShellResult> ExecuteAsync(string command, string? workingDir = null, int timeoutSeconds = 60, CancellationToken cancellationToken = default)
            => Task.FromResult(new ShellResult(0, string.Empty, string.Empty));
    }
}
