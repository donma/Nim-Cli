using Microsoft.Extensions.DependencyInjection;
using NimCli.App;
using NimCli.Core;
using NimCli.Infrastructure.Config;
using NimTui.App;
using Xunit;

namespace NimTui.Tests;

public sealed class TuiLongRunTests
{
    [Fact]
    public async Task Tui_Long_Session_Render_Remains_Stable_With_Many_Actions()
    {
        var options = new NimCliOptions();
        var services = await ServiceConfiguration.BuildServicesAsync(options);
        var session = services.GetRequiredService<SessionState>();
        var formatter = services.GetRequiredService<ExecutionSummaryFormatter>();
        var policy = services.GetRequiredService<PolicySummaryService>();
        var manager = services.GetRequiredService<SessionManager>();
        manager.InitializeNewSession(session, Directory.GetCurrentDirectory(), []);

        for (var index = 0; index < 20; index++)
        {
            session.AddUserMessage($"user-{index}");
            session.AddAssistantMessage($"assistant-{index}");
            session.AddRecentAction($"action-{index}");
        }

        var state = TuiApplication.BuildRenderState(session, options, formatter, policy, "long run ok", null, manager);

        Assert.Contains("[Transcript Pane]", state.Layout);
        Assert.Contains("Recent Actions:", state.StatusPane);
        Assert.Contains("action-19", state.StatusPane);
    }

    [Fact]
    public async Task Cli_Tui_Mixed_Workflow_Keeps_Shared_Session_State()
    {
        var options = new NimCliOptions();
        var services = await ServiceConfiguration.BuildServicesAsync(options);
        var session = services.GetRequiredService<SessionState>();
        var manager = services.GetRequiredService<SessionManager>();
        var policy = services.GetRequiredService<PolicySummaryService>();
        manager.InitializeNewSession(session, Directory.GetCurrentDirectory(), []);

        session.RecordCurrentTask("cli step");
        session.AddRecentAction("cli:doctor");

        var result = TuiApplication.HandleUiCommandForTest("/recent", session, manager, options, policy);

        Assert.True(result.Handled);
        Assert.Contains("cli:doctor", result.Output);
    }

    [Fact]
    public async Task Tui_Long_Session_Status_Pane_Shows_Expanded_Summary_Artifacts()
    {
        var options = new NimCliOptions();
        var services = await ServiceConfiguration.BuildServicesAsync(options);
        var session = services.GetRequiredService<SessionState>();
        var formatter = services.GetRequiredService<ExecutionSummaryFormatter>();
        var policy = services.GetRequiredService<PolicySummaryService>();
        var manager = services.GetRequiredService<SessionManager>();
        manager.InitializeNewSession(session, Directory.GetCurrentDirectory(), []);

        session.RecordCurrentTask("phase8 tui summary review");
        session.RecordBuildSummary("build ok");
        session.RecordTestSummary("tests ok");
        session.RecordScreenshotPath("D:\\tmp\\shot.png");
        session.RecordSuggestedCommitMessage("improve tui summary visibility");

        var state = TuiApplication.BuildRenderState(session, options, formatter, policy, "summary ok", null, manager);

        Assert.Contains("phase8 tui summary review", state.StatusPane);
        Assert.Contains("Last Status: summary ok", state.StatusPane, StringComparison.OrdinalIgnoreCase);
    }
}
