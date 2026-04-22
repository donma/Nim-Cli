using NimCli.Core;
using NimCli.Infrastructure.Config;
using NimCli.Tools.Abstractions;
using Xunit;

namespace NimCli.Core.Tests;

public sealed class BigPhase6CoreTests
{
    [Fact]
    public void ToolPolicyService_Global_And_PerTool_Override_Are_Auditable()
    {
        var service = new ToolPolicyService(new NimCliOptions());
        service.SetToolOverride("run_shell", ApprovalDecision.Deny);
        var toolDecision = service.EvaluateDetailed(new FakeTool("run_shell", RiskLevel.Medium), new Dictionary<string, object?> { ["command"] = "dir" });

        service.SetGlobalOverride(ApprovalDecision.Ask);
        var globalDecision = service.EvaluateDetailed(new FakeTool("git_status", RiskLevel.Low), new Dictionary<string, object?>());

        Assert.Equal(ApprovalDecision.Deny, toolDecision.Decision);
        Assert.Equal("Per-tool override: Deny", toolDecision.Reason);
        Assert.Equal(ApprovalDecision.Ask, globalDecision.Decision);
        Assert.Equal("Global override: Ask", globalDecision.Reason);
    }

    [Fact]
    public void ContextBuilder_Compresses_Long_Context_To_Token_Budget()
    {
        var session = new SessionState();
        session.RecordCurrentTask(new string('a', 1000));
        session.RecordRepoMap(new string('b', 7000));

        var builder = new ContextBuilder();
        builder.AddSessionState(session, AgentMode.Coding);
        var context = builder.Build(1000);

        Assert.Contains("[Context Audit]", context);
        Assert.True(context.Length <= 1100);
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
