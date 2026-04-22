using NimCli.App;
using NimCli.Contracts;
using NimCli.Core;
using NimCli.Infrastructure;
using NimCli.Infrastructure.Config;
using NimCli.Tools.Shell;
using Xunit;

namespace NimCli.Integration.Tests;

public sealed class BigPhase8IntegrationTests
{
    [Fact]
    public void Long_Context_Workflow_Prioritizes_Current_Task_And_Policy_Audit()
    {
        var session = new SessionState { Mode = AgentMode.Coding };
        session.RecordCurrentTask("stabilize phase8 long context workflow");
        session.RecordRepoMap(new string('r', 7000));
        session.RecordBuildSummary(new string('b', 3000));
        session.AddRecentAction("tool:build_project");
        session.AddPolicyAudit(new PolicyAuditEntry("run_shell", "Ask", "High", false, "High risk requires approval", "command=dotnet test"));

        var builder = new ContextBuilder();
        builder.AddSessionState(session, AgentMode.Coding);
        var context = builder.Build(1200);

        Assert.Contains("stabilize phase8 long context workflow", context);
        Assert.Contains("run_shell", context);
        Assert.Contains("[Context Audit]", context);
    }

    [Fact]
    public void Summary_Audit_Workflow_Carries_Policy_Approval_And_Artifacts()
    {
        var session = new SessionState();
        session.RecordCurrentTask("audit summary visibility");
        session.RecordContextStrategy("ops");
        session.RecordBuildSummary("dotnet build ok");
        session.RecordTestSummary("dotnet test ok");
        session.RecordScreenshotPath("D:\\tmp\\audit.png");
        session.RecordSuggestedCommitMessage("tighten summary audit");
        session.AddPolicyAudit(new PolicyAuditEntry("git_push", "Ask", "High", true, "High risk requires approval", "remote=origin"));

        var formatter = new ExecutionSummaryFormatter();
        var summary = formatter.BuildExecutionSummary(
            new AgentResponse(
                "workflow complete",
                RequiresApproval: true,
                ApprovalPrompt: "Allow git_push?",
                ApprovalRequest: new ApprovalRequest("git_push", "High", "remote=origin", true, "High risk requires approval", "Allow git_push?"),
                ToolResults: [new ToolCallResult("1", "git_push", "dry-run ok")]),
            session,
            125);

        Assert.NotNull(summary.ApprovalActions);
        Assert.NotNull(summary.PolicyDecisions);
        Assert.NotNull(summary.Artifacts);
        Assert.NotNull(summary.ToolResultSummaries);
        Assert.NotEmpty(summary.ApprovalActions!);
        Assert.NotEmpty(summary.PolicyDecisions!);
        Assert.NotEmpty(summary.Artifacts!);
        Assert.NotEmpty(summary.ToolResultSummaries!);
    }

    [Fact]
    public async Task Config_MultiWorkspace_Workflow_Uses_User_Base_And_Workspace_Override()
    {
        var originalHome = Environment.GetEnvironmentVariable("NIMCLI_HOME");
        var originalDirectory = Directory.GetCurrentDirectory();
        var tempHome = Path.Combine(Path.GetTempPath(), "nimcli-phase8-int-home", Guid.NewGuid().ToString("N"));
        var workspaceA = Path.Combine(Path.GetTempPath(), "nimcli-phase8-workspace-a", Guid.NewGuid().ToString("N"));
        var workspaceB = Path.Combine(Path.GetTempPath(), "nimcli-phase8-workspace-b", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempHome);
        Directory.CreateDirectory(workspaceA);
        Directory.CreateDirectory(workspaceB);

        try
        {
            Environment.SetEnvironmentVariable("NIMCLI_HOME", tempHome);
            File.WriteAllText(Path.Combine(tempHome, "appsettings.secret.json"), """
{
  "NimCli": {
    "Provider": {
      "BaseUrl": "https://shared.example",
      "DefaultModel": "shared-model"
    }
  }
}
""");

            File.WriteAllText(Path.Combine(workspaceB, "appsettings.json"), """
{
  "NimCli": {
    "Provider": {
      "DefaultModel": "workspace-b-model"
    }
  }
}
""");

            Directory.SetCurrentDirectory(workspaceA);
            var a = UserConfigStore.LoadUserConfig();
            Directory.SetCurrentDirectory(workspaceB);
            var b = UserConfigStore.LoadUserConfig();

            Assert.Equal("shared-model", a.Provider.DefaultModel);
            Assert.Equal("workspace-b-model", b.Provider.DefaultModel);
            Assert.Equal("https://shared.example", b.Provider.BaseUrl);
            await Task.CompletedTask;
        }
        finally
        {
            Environment.SetEnvironmentVariable("NIMCLI_HOME", originalHome);
            Directory.SetCurrentDirectory(originalDirectory);
            try { Directory.Delete(tempHome, recursive: true); } catch { }
            try { Directory.Delete(workspaceA, recursive: true); } catch { }
            try { Directory.Delete(workspaceB, recursive: true); } catch { }
        }
    }
}
