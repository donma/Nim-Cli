using NimCli.App;
using NimCli.Contracts;
using NimCli.Core;
using NimCli.Infrastructure.Config;
using NimCli.Tools.Abstractions;
using Xunit;

namespace NimCli.Core.Tests;

public sealed class BigPhase5CoreTests
{
    [Fact]
    public void ContextBuilder_Uses_Coding_Strategy_And_Recent_Audit_State()
    {
        var session = new SessionState
        {
            WorkingDirectory = "D:\\repo",
            Mode = AgentMode.Coding
        };
        session.RecordCurrentTask("修正 build 問題");
        session.RecordRepoMap("repo map");
        session.AddRecentAction("tool:build_project");
        session.AddPolicyAudit(new PolicyAuditEntry("git_push", "Ask", "High", true, "High risk requires approval", "remote=origin"));

        var builder = new ContextBuilder();
        builder.AddSessionState(session, AgentMode.Coding);
        var context = builder.Build();

        Assert.Equal("coding", builder.LastStrategy);
        Assert.Contains("[Context Strategy]", context);
        Assert.Contains("coding", context);
        Assert.Contains("[Current Task]", context);
        Assert.Contains("[Recent Actions]", context);
        Assert.Contains("[Policy Audit]", context);
    }

    [Fact]
    public void ToolPolicyService_Builds_Masked_Audit_Entry_And_DryRun()
    {
        var service = new ToolPolicyService(new NimCliOptions { Tools = new ToolsOptions { AllowFtpUpload = true } });
        var tool = new FakeTool("upload_ftp", RiskLevel.High);
        var input = new Dictionary<string, object?>
        {
            ["host"] = "ftp.example.com",
            ["password"] = "secret-value"
        };

        var decision = service.EvaluateDetailed(tool, input);
        var audit = service.BuildAuditEntry(tool, decision, input);

        Assert.Equal(ApprovalDecision.Ask, decision.Decision);
        Assert.True(decision.DryRun);
        Assert.Contains("password=***", audit.InputSummary);
    }

    [Fact]
    public void ExecutionSummaryFormatter_Includes_Context_And_Policy_Details()
    {
        var session = new SessionState();
        session.RecordContextStrategy("ops");
        session.AddPolicyAudit(new PolicyAuditEntry("git_push", "Ask", "High", true, "High risk requires approval", "remote=origin"));
        var formatter = new ExecutionSummaryFormatter();
        var summary = formatter.BuildExecutionSummary(new AgentResponse("done"), session, 42);
        var text = formatter.FormatExecutionSummary(summary);

        Assert.Contains("Context: ops", text);
        Assert.Contains("Policy [git_push]: Ask/High dry-run=True", text);
    }

    private sealed class FakeTool : ITool
    {
        public FakeTool(string name, RiskLevel riskLevel)
        {
            Name = name;
            RiskLevel = riskLevel;
        }

        public string Name { get; }
        public string Description => Name;
        public RiskLevel RiskLevel { get; }
        public object InputSchema => new { };
        public Task<ToolExecuteResult> ExecuteAsync(Dictionary<string, object?> input, CancellationToken cancellationToken = default)
            => Task.FromResult(new ToolExecuteResult(true, string.Empty));
    }
}
