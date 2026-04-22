using NimCli.Core;
using NimCli.Infrastructure.Config;
using NimCli.Tools.Abstractions;
using Xunit;

namespace NimCli.Core.Tests;

public class ToolPolicyServiceTests
{
    [Fact]
    public void EvaluateDetailed_HighRisk_GitPush_Is_Ask_With_DryRun()
    {
        var service = new ToolPolicyService(new NimCliOptions { Tools = new ToolsOptions { AllowGitPush = true } });
        var decision = service.EvaluateDetailed(new FakeTool("git_push", RiskLevel.High));

        Assert.Equal(ApprovalDecision.Ask, decision.Decision);
        Assert.True(decision.DryRun);
    }

    [Fact]
    public void EvaluateDetailed_HighRisk_FtpUpload_Is_Ask_With_DryRun()
    {
        var service = new ToolPolicyService(new NimCliOptions { Tools = new ToolsOptions { AllowFtpUpload = true } });
        var decision = service.EvaluateDetailed(new FakeTool("upload_ftp", RiskLevel.High));

        Assert.Equal(ApprovalDecision.Ask, decision.Decision);
        Assert.True(decision.DryRun);
    }

    [Fact]
    public void EvaluateDetailed_HighRisk_RunShell_Is_Ask_When_Enabled()
    {
        var service = new ToolPolicyService(new NimCliOptions { Tools = new ToolsOptions { AllowShell = true } });
        var decision = service.EvaluateDetailed(new FakeTool("run_shell", RiskLevel.High));

        Assert.Equal(ApprovalDecision.Ask, decision.Decision);
    }

    [Fact]
    public void EvaluateDetailed_Disabled_Config_Becomes_Deny()
    {
        var service = new ToolPolicyService(new NimCliOptions { Tools = new ToolsOptions { AllowShell = false } });
        var decision = service.EvaluateDetailed(new FakeTool("run_shell", RiskLevel.High));

        Assert.Equal(ApprovalDecision.Deny, decision.Decision);
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
