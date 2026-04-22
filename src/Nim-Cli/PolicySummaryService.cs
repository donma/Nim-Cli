using NimCli.Contracts;
using NimCli.Core;

namespace NimCli.App;

public sealed class PolicySummaryService
{
    private readonly ToolRegistry _toolRegistry;
    private readonly ToolPolicyService _toolPolicyService;

    public PolicySummaryService(ToolRegistry toolRegistry, ToolPolicyService toolPolicyService)
    {
        _toolRegistry = toolRegistry;
        _toolPolicyService = toolPolicyService;
    }

    public IReadOnlyList<ToolPolicySummary> GetSummaries()
        => _toolRegistry.GetAll()
            .OrderBy(tool => tool.Name, StringComparer.OrdinalIgnoreCase)
            .Select(tool =>
            {
                var decision = _toolPolicyService.EvaluateDetailed(tool);
                var decisionLabel = MapDecision(decision.Decision);
                if (decision.DryRun)
                    decisionLabel += "/DryRun";

                return new ToolPolicySummary(
                    tool.Name,
                    tool.Description,
                    decision.RiskLevel.ToString(),
                    decisionLabel,
                    decision.DryRun,
                    decision.Reason);
            })
            .ToList();

    public string FormatSummaries()
        => string.Join(Environment.NewLine,
            GetSummaries().Select(summary =>
                $"{summary.ToolName} | risk={summary.RiskLevel.ToLowerInvariant()} | decision={summary.Decision.ToLowerInvariant()} | dry-run={summary.DryRun} | {summary.Reason} | {summary.Description}"));

    private static string MapDecision(ApprovalDecision decision)
        => decision switch
        {
            ApprovalDecision.Allow => "Allow",
            ApprovalDecision.Ask => "Ask",
            ApprovalDecision.Deny => "Deny",
            _ => "Allow"
        };
}
