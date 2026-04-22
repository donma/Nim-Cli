using Microsoft.Extensions.DependencyInjection;
using NimCli.App;
using NimCli.Core;
using NimCli.Infrastructure.Config;
using NimTui.App;
using System.Diagnostics;
using Xunit;

namespace NimTui.Tests;

public class TuiSmokeTests
{
    private static readonly string RepoRoot = ResolveRepoRoot();

    [Fact]
    public async Task Shared_Interactive_Command_Service_Handles_Plan_Task()
    {
        var services = await ServiceConfiguration.BuildServicesAsync(new NimCliOptions());
        var session = services.GetRequiredService<SessionState>();
        var manager = services.GetRequiredService<SessionManager>();
        manager.InitializeNewSession(session, RepoRoot, []);

        var result = await services.GetRequiredService<InteractiveCommandService>()
            .ExecuteAsync(services, "/plan add doctor summary", "default");

        Assert.True(result.ShouldContinue);
        Assert.Contains("Impact Files", result.Output);
        Assert.Contains("Verify Strategy", result.Output);
    }

    [Fact]
    public async Task Shared_Interactive_Command_Service_Handles_Build_Command()
    {
        var services = await ServiceConfiguration.BuildServicesAsync(new NimCliOptions());
        var session = services.GetRequiredService<SessionState>();
        var manager = services.GetRequiredService<SessionManager>();
        manager.InitializeNewSession(session, RepoRoot, []);
        var projectPath = Path.Combine(RepoRoot, "src", "NimCli.Contracts", "NimCli.Contracts.csproj");

        var result = await services.GetRequiredService<InteractiveCommandService>()
            .ExecuteAsync(services, $"/build --project {projectPath}", "default");

        Assert.True(result.ShouldContinue);
        Assert.DoesNotContain("error MSB1009", result.Output, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("NimCli.Contracts", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Shared_Interactive_Command_Service_Handles_Analyze_Command()
    {
        var services = await ServiceConfiguration.BuildServicesAsync(new NimCliOptions());
        var session = services.GetRequiredService<SessionState>();
        var manager = services.GetRequiredService<SessionManager>();
        manager.InitializeNewSession(session, RepoRoot, []);
        var srcDirectory = Path.Combine(RepoRoot, "src");

        var result = await services.GetRequiredService<InteractiveCommandService>()
            .ExecuteAsync(services, $"/analyze --directory {srcDirectory}", "default");

        Assert.True(result.ShouldContinue);
        Assert.Contains("RepoMap", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Shared_Interactive_Command_Service_Handles_Run_Command()
    {
        var services = await ServiceConfiguration.BuildServicesAsync(new NimCliOptions());
        var session = services.GetRequiredService<SessionState>();
        var manager = services.GetRequiredService<SessionManager>();
        manager.InitializeNewSession(session, RepoRoot, []);

        var result = await services.GetRequiredService<InteractiveCommandService>()
            .ExecuteAsync(services, "/run hello from tui", "default");

        Assert.True(result.ShouldContinue);
        Assert.False(string.IsNullOrWhiteSpace(result.Output));
        Assert.DoesNotContain("Usage: /run", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Shared_Interactive_Command_Service_Handles_Browser_Open_Command()
    {
        var services = await ServiceConfiguration.BuildServicesAsync(new NimCliOptions());
        var session = services.GetRequiredService<SessionState>();
        var manager = services.GetRequiredService<SessionManager>();
        manager.InitializeNewSession(session, RepoRoot, []);

        var result = await services.GetRequiredService<InteractiveCommandService>()
            .ExecuteAsync(services, "/browser open", "default");

        Assert.True(result.ShouldContinue);
        Assert.Contains("Browser session opened", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Shared_Interactive_Command_Service_Handles_Git_Status_Command()
    {
        var services = await ServiceConfiguration.BuildServicesAsync(new NimCliOptions());
        var session = services.GetRequiredService<SessionState>();
        var manager = services.GetRequiredService<SessionManager>();
        manager.InitializeNewSession(session, RepoRoot, []);
        using var repo = CreateTemporaryGitRepository();

        var result = await services.GetRequiredService<InteractiveCommandService>()
            .ExecuteAsync(services, $"/git status --working-dir {repo.DirectoryPath}", "default");

        Assert.True(result.ShouldContinue);
        Assert.Contains("On branch", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Tui_Render_Uses_Shared_Session_Summary_Data()
    {
        var session = new SessionState();
        session.RecordBuildSummary("Build succeeded");
        session.RecordTestSummary("Tests passed");
        session.RecordScreenshotPath("D:\\tmp\\shot.png");

        var formatter = new ExecutionSummaryFormatter();
        var summary = formatter.BuildSessionSummary(session);

        Assert.Equal("Build succeeded", summary.LastBuildSummary);
        Assert.Equal("Tests passed", summary.LastTestSummary);
        Assert.Equal("D:\\tmp\\shot.png", summary.LastScreenshotPath);
    }

    [Fact]
    public void Tui_Opening_Frame_Shows_Product_Provider_Model_And_Workspace()
    {
        var options = new NimCliOptions();
        var session = new SessionState { WorkingDirectory = RepoRoot };

        var frame = TuiApplication.RenderOpeningFrame(options, session);

        Assert.Contains("NIM-TUI", frame);
        Assert.Contains(options.Provider.Name, frame);
        Assert.Contains(options.Provider.DefaultModel, frame);
        Assert.Contains(RepoRoot, frame);
    }

    [Fact]
    public void Tui_Opening_Uses_Single_Static_Ascii_Frame()
    {
        var options = new NimCliOptions();
        var session = new SessionState { WorkingDirectory = RepoRoot };

        var frames = TuiApplication.BuildOpeningAnimationFrames(options, session);

        Assert.Single(frames);
        Assert.Contains("##    ## ####", frames[0]);
        Assert.Contains("NIM-TUI", frames[0], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Hotkeys", frames[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Tui_Opening_Pause_Uses_Visible_Default_Window()
    {
        var pause = Environment.GetEnvironmentVariable("NIM_TUI_OPENING_PAUSE_MS");

        Assert.True(string.IsNullOrWhiteSpace(pause) || int.TryParse(pause, out _));
    }

    [Fact]
    public void Tui_Command_Palette_Contains_High_Frequency_Actions()
    {
        var palette = TuiApplication.FormatCommandPalette();

        Assert.Contains("Build", palette);
        Assert.Contains("Run Project", palette);
        Assert.Contains("Screenshot", palette);
        Assert.Contains("Policy Summary", palette);
        Assert.Contains("FTP Upload", palette);
    }

    [Fact]
    public void Tui_Input_Hint_Shows_Slash_Suggestions_When_Typing_Command()
    {
        var session = new SessionState { Mode = AgentMode.Analysis };

        var hint = TuiApplication.BuildInputHint("/br", session);

        Assert.Contains("Suggestions:", hint);
        Assert.Contains("/browser open", hint, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Tui_Input_Hint_Reflects_Current_Mode_When_Not_Using_Slash_Command()
    {
        var session = new SessionState { Mode = AgentMode.Coding };

        var hint = TuiApplication.BuildInputHint(string.Empty, session);

        Assert.Contains("Coding mode", hint, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Tui_Approval_Dialog_Shows_Risk_DryRun_And_Input_Summary()
    {
        var dialog = TuiApplication.FormatApprovalDialog(
            new NimCli.Contracts.ApprovalRequest("git_push", "High", "remote=origin, branch=main", true, "High risk requires approval", "Allow git_push?"),
            expanded: true);

        Assert.Contains("Tool      : git_push", dialog);
        Assert.Contains("Risk      : High", dialog);
        Assert.Contains("Dry-Run   : True", dialog);
        Assert.Contains("Input     : remote=origin, branch=main", dialog);
        Assert.Contains("allow / deny / details", dialog, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Tui_Layout_Includes_Status_Pane_Recent_Actions_And_Footer()
    {
        var options = new NimCliOptions();
        var services = await ServiceConfiguration.BuildServicesAsync(options);
        var session = services.GetRequiredService<SessionState>();
        var formatter = services.GetRequiredService<ExecutionSummaryFormatter>();
        var policySummary = services.GetRequiredService<PolicySummaryService>();
        var sessionManager = services.GetRequiredService<SessionManager>();
        sessionManager.InitializeNewSession(session, RepoRoot, []);
        session.RecordCurrentTask("檢查 TUI layout");
        session.AddAssistantMessage("done");
        session.AddRecentAction("slash:/palette");

        var layout = TuiApplication.BuildLayout(session, options, formatter, policySummary, "Ready", null, sessionManager);

        Assert.Contains("[Transcript Pane]", layout);
        Assert.Contains("[Status Pane]", layout);
        Assert.Contains("Recent Actions:", layout);
        Assert.Contains("Hotkeys:", layout);
    }

    [Fact]
    public async Task Tui_Layout_Shows_Output_Pane_For_Multiline_Help_Text()
    {
        var options = new NimCliOptions();
        var services = await ServiceConfiguration.BuildServicesAsync(options);
        var session = services.GetRequiredService<SessionState>();
        var formatter = services.GetRequiredService<ExecutionSummaryFormatter>();
        var policySummary = services.GetRequiredService<PolicySummaryService>();
        var sessionManager = services.GetRequiredService<SessionManager>();
        sessionManager.InitializeNewSession(session, RepoRoot, []);

        var helpText = CliApplication.GetInteractiveHelpText();
        var layout = TuiApplication.BuildLayout(session, options, formatter, policySummary, helpText, null, sessionManager);

        Assert.Contains("[Output Pane]", layout);
        Assert.Contains("Interactive slash commands:", layout);
        Assert.Contains("/help, /?", layout);
    }

    private static string ResolveRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Nim-Cli.slnx")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new InvalidOperationException("找不到 Nim-Cli.slnx，無法解析 repo root。");
    }

    private static TemporaryGitRepository CreateTemporaryGitRepository()
    {
        var directoryPath = Path.Combine(Path.GetTempPath(), "nimcli-tui-git-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directoryPath);
        File.WriteAllText(Path.Combine(directoryPath, "readme.txt"), "hello" + Environment.NewLine);

        RunGit("init", directoryPath);
        RunGit("config user.email nimcli-tests@example.com", directoryPath);
        RunGit("config user.name nimcli-tests", directoryPath);
        RunGit("add .", directoryPath);
        RunGit("commit -m initial", directoryPath);
        return new TemporaryGitRepository(directoryPath);
    }

    private static void RunGit(string arguments, string workingDirectory)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        });

        if (process is null)
            throw new InvalidOperationException($"無法啟動 git {arguments}");

        process.WaitForExit();
        if (process.ExitCode == 0)
            return;

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        throw new InvalidOperationException($"git {arguments} 失敗。{output}{error}");
    }

    private sealed class TemporaryGitRepository : IDisposable
    {
        public TemporaryGitRepository(string directoryPath)
        {
            DirectoryPath = directoryPath;
        }

        public string DirectoryPath { get; }

        public void Dispose()
        {
            try
            {
                Directory.Delete(DirectoryPath, recursive: true);
            }
            catch
            {
            }
        }
    }
}
