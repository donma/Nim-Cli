using Microsoft.Extensions.DependencyInjection;
using NimCli.App;
using NimCli.Core;
using NimCli.Infrastructure.Config;
using NimTui.App;
using Xunit;

namespace NimTui.Tests;

public sealed class TuiEventDrivenTests
{
    private static readonly string RepoRoot = ResolveRepoRoot();

    [Fact]
    public async Task Focus_Mode_And_Palette_Commands_Update_Tui_State()
    {
        var services = await ServiceConfiguration.BuildServicesAsync(new NimCliOptions());
        var session = services.GetRequiredService<SessionState>();
        var manager = services.GetRequiredService<SessionManager>();
        var policy = services.GetRequiredService<PolicySummaryService>();
        manager.InitializeNewSession(session, RepoRoot, []);

        var palette = TuiApplication.HandleUiCommandForTest("/palette", session, manager, new NimCliOptions(), policy);
        var focus = TuiApplication.HandleUiCommandForTest("/focus status", session, manager, new NimCliOptions(), policy);
        var mode = TuiApplication.HandleUiCommandForTest("/mode coding", session, manager, new NimCliOptions(), policy);

        Assert.True(palette.Handled);
        Assert.Contains("Build", palette.Output);
        Assert.True(focus.Handled);
        Assert.Equal("status", session.UserPreferences["tui_focus"]);
        Assert.True(mode.Handled);
        Assert.Equal(AgentMode.Coding, session.Mode);
    }

    [Fact]
    public async Task Recent_Sessions_Command_Returns_Persisted_Session_Summary()
    {
        var services = await ServiceConfiguration.BuildServicesAsync(new NimCliOptions());
        var session = services.GetRequiredService<SessionState>();
        var manager = services.GetRequiredService<SessionManager>();
        var policy = services.GetRequiredService<PolicySummaryService>();
        manager.InitializeNewSession(session, RepoRoot, []);
        session.AddUserMessage("hello session");
        manager.SaveSession(session);

        var result = TuiApplication.HandleUiCommandForTest("/sessions", session, manager, new NimCliOptions(), policy);

        Assert.True(result.Handled);
        Assert.Contains("messages=1", result.Output);
    }

    [Fact]
    public void Tab_Completion_Selects_First_Matching_Slash_Command()
    {
        var completed = TuiApplication.ApplySuggestion("/gi");

        Assert.Equal("/git status --working-dir <dir>", completed);
    }

    [Fact]
    public async Task Shared_Interactive_Help_Command_Returns_Multiline_Help_Text()
    {
        var services = await ServiceConfiguration.BuildServicesAsync(new NimCliOptions());
        var session = services.GetRequiredService<SessionState>();
        var manager = services.GetRequiredService<SessionManager>();
        manager.InitializeNewSession(session, RepoRoot, []);

        var result = await services.GetRequiredService<InteractiveCommandService>()
            .ExecuteAsync(services, "/help", "default");

        Assert.True(result.ShouldContinue);
        Assert.Contains("Interactive slash commands:", result.Output);
        Assert.Contains("/build [--project <path>]", result.Output);
        Assert.True(result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length > 3);
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
}
