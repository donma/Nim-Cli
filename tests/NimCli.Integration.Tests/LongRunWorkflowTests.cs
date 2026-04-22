using Microsoft.Extensions.DependencyInjection;
using NimCli.App;
using NimCli.Contracts;
using NimCli.Core;
using NimCli.Infrastructure.Config;
using Xunit;

namespace NimCli.Integration.Tests;

public sealed class LongRunWorkflowTests
{
    [Fact]
    public void Long_Chat_Session_Maintains_Transcript_And_Recent_Actions()
    {
        var session = new SessionState();
        for (var index = 0; index < 18; index++)
        {
            session.AddUserMessage($"user-{index}");
            session.AddAssistantMessage($"assistant-{index}");
            session.AddRecentAction($"turn-{index}");
        }

        var formatter = new ExecutionSummaryFormatter();
        var summary = formatter.BuildSessionSummary(session);

        Assert.Equal(36, summary.MessageCount);
        Assert.NotNull(summary.RecentActions);
        Assert.True(summary.RecentActions!.Count <= 12);
    }

    [Fact]
    public void Session_Resume_Workflow_Persists_Current_Task_Context_And_Policy_Audit()
    {
        var originalDirectory = Directory.GetCurrentDirectory();
        var tempDirectory = Path.Combine(Path.GetTempPath(), "nimcli-longrun-session", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        Directory.SetCurrentDirectory(tempDirectory);

        try
        {
            var manager = new SessionManager(new NimCli.Infrastructure.CliRuntimeStore());
            var session = new SessionState();
            manager.InitializeNewSession(session, tempDirectory, []);
            session.RecordCurrentTask("long resume workflow");
            session.RecordContextStrategy("coding");
            session.AddRecentAction("step:build");
            session.AddPolicyAudit(new PolicyAuditEntry("git_push", "Ask", "High", true, "High risk requires approval", "remote=origin"));
            manager.SaveSession(session);

            var restored = new SessionState();
            manager.InitializeNewSession(restored, tempDirectory, []);
            var stored = manager.LoadLatest(restored.WorkspaceKey);
            Assert.NotNull(stored);
            manager.RestoreSession(restored, stored!);

            Assert.Equal("long resume workflow", restored.CurrentTask);
            Assert.Equal("coding", restored.LastContextStrategy);
            Assert.Contains(restored.RecentActions, item => item == "step:build");
            Assert.Contains(restored.PolicyAuditTrail, item => item.ToolName == "git_push");
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
            try { Directory.Delete(tempDirectory, recursive: true); } catch { }
        }
    }

    [Fact]
    public void High_Risk_Approval_Workflow_Produces_Audit_Trail_With_DryRun()
    {
        var policy = new ToolPolicyService(new NimCliOptions { Tools = new ToolsOptions { AllowGitPush = true, AllowFtpUpload = true } });
        var gitDecision = policy.EvaluateDetailed(new FakeTool("git_push", NimCli.Tools.Abstractions.RiskLevel.High), new Dictionary<string, object?> { ["remote"] = "origin" });
        var ftpDecision = policy.EvaluateDetailed(new FakeTool("upload_ftp", NimCli.Tools.Abstractions.RiskLevel.High), new Dictionary<string, object?> { ["host"] = "ftp.example.com" });

        Assert.True(gitDecision.DryRun);
        Assert.True(ftpDecision.DryRun);
        Assert.Equal(ApprovalDecision.Ask, gitDecision.Decision);
        Assert.Equal(ApprovalDecision.Ask, ftpDecision.Decision);
    }

    private sealed class FakeTool : NimCli.Tools.Abstractions.ITool
    {
        public FakeTool(string name, NimCli.Tools.Abstractions.RiskLevel riskLevel)
        {
            Name = name;
            RiskLevel = riskLevel;
        }

        public string Name { get; }
        public string Description => Name;
        public NimCli.Tools.Abstractions.RiskLevel RiskLevel { get; }
        public object InputSchema => new { };
        public Task<NimCli.Tools.Abstractions.ToolExecuteResult> ExecuteAsync(Dictionary<string, object?> input, CancellationToken cancellationToken = default)
            => Task.FromResult(new NimCli.Tools.Abstractions.ToolExecuteResult(true, string.Empty));
    }
}
