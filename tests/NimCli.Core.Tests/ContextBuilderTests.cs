using NimCli.Core;
using Xunit;

namespace NimCli.Core.Tests;

public class ContextBuilderTests
{
    [Fact]
    public void AddSessionState_Includes_Recent_Summaries()
    {
        var session = new SessionState();
        session.WorkingDirectory = "D:\\repo";
        session.SetWorkspaceDirectories(["D:\\repo", "D:\\repo\\src"]);
        session.RecordRepoMap("repo map here");
        session.RecordShellResult("dotnet build", "build ok");
        session.RecordWebResult("https://example.com", "example page");
        session.RecordDbResult("select 1", "1");
        session.RecordBuildSummary("build summary");
        session.RecordTestSummary("test summary");
        session.RecordScreenshotPath("D:\\shot.png");
        session.RecordSuggestedCommitMessage("update tests");

        var builder = new ContextBuilder();
        builder.AddSessionState(session);
        var context = builder.Build();

        Assert.Contains("[Workspace Directories]", context);
        Assert.Contains("[Repo Map]", context);
        Assert.Contains("[Shell: dotnet build]", context);
        Assert.Contains("[Web: https://example.com]", context);
        Assert.Contains("[DB Query: select 1]", context);
        Assert.Contains("[Tool Result: build]", context);
        Assert.Contains("[Tool Result: test]", context);
        Assert.Contains("[Tool Result: screenshot]", context);
        Assert.Contains("[Suggested Commit Message]", context);
    }

    [Fact]
    public void Clear_Removes_All_Context_Parts()
    {
        var builder = new ContextBuilder();
        builder.AddToolResult("build", "ok");
        Assert.NotEmpty(builder.Build());

        builder.Clear();

        Assert.Equal(string.Empty, builder.Build());
    }

    [Fact]
    public void Build_Prioritizes_Current_Task_And_Recent_Actions_Before_Low_Priority_Content()
    {
        var session = new SessionState { Mode = AgentMode.Coding };
        session.RecordCurrentTask("修正 shell quoting");
        session.AddRecentAction("tool:run_shell");
        session.RecordRepoMap(new string('r', 6000));
        session.RecordShellResult("dotnet build", new string('s', 6000));

        var builder = new ContextBuilder();
        builder.AddSessionState(session, AgentMode.Coding);
        var context = builder.Build(900);

        Assert.Contains("[Current Task]", context);
        Assert.Contains("shell quoting", context);
        Assert.Contains("[Recent Actions]", context);
        Assert.Contains("tool:run_shell", context);
        Assert.DoesNotContain(new string('r', 500), context, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_Uses_Resume_Strategy_When_Task_Indicates_Resume()
    {
        var session = new SessionState();
        session.RecordCurrentTask("resume previous coding session");

        var builder = new ContextBuilder();
        builder.AddSessionState(session, AgentMode.Analysis);
        var context = builder.Build();

        Assert.Equal("resume", builder.LastStrategy);
        Assert.Contains("[Context Strategy]\nresume", context);
    }

    [Fact]
    public void Build_Uses_TuiInteractive_Strategy_And_Includes_Conversation_Summary()
    {
        var session = new SessionState { Mode = AgentMode.Analysis };
        session.UserPreferences["tui_focus"] = "input";
        session.AddUserMessage("show latest shell policy state");
        session.AddAssistantMessage("policy state is visible in status pane");

        var builder = new ContextBuilder();
        builder.AddSessionState(session, AgentMode.Analysis);
        var context = builder.Build(900);

        Assert.Equal("tui-interactive", builder.LastStrategy);
        Assert.Contains("[Recent Conversation]", context);
        Assert.Contains("show latest shell policy state", context);
        Assert.Contains("[Context Audit]", context);
    }
}
