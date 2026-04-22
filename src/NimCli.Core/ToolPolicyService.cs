using NimCli.Tools.Abstractions;
using NimCli.Infrastructure.Config;
using NimCli.Contracts;

namespace NimCli.Core;

public enum ApprovalDecision { Allow, Ask, Deny }

public sealed record ToolPolicyDecision(
    ApprovalDecision Decision,
    RiskLevel RiskLevel,
    bool DryRun,
    string Reason);

public class ToolPolicyService
{
    private readonly NimCliOptions _options;
    private readonly Dictionary<string, ApprovalDecision> _perToolOverrides = new(StringComparer.OrdinalIgnoreCase);
    private ApprovalDecision? _globalOverride;

    public ToolPolicyService(NimCliOptions options)
    {
        _options = options;
    }

    private readonly HashSet<string> _alwaysAllow = new(StringComparer.OrdinalIgnoreCase)
    {
        "read_file", "repo_map", "build_project", "test_project",
        "web_fetch", "web_search", "screenshot_page", "query_db",
        "git_status", "git_diff", "analyze_project", "lint_project"
    };

    private readonly HashSet<string> _alwaysDeny = new(StringComparer.OrdinalIgnoreCase)
    {
        "recursive_delete", "credential_dump"
    };

    public ToolPolicyDecision EvaluateDetailed(ITool tool)
        => EvaluateDetailed(tool, null);

    public ToolPolicyDecision EvaluateDetailed(ITool tool, Dictionary<string, object?>? input)
    {
        if (IsDisabledByConfig(tool))
            return new ToolPolicyDecision(ApprovalDecision.Deny, tool.RiskLevel, false, "Disabled by configuration");

        if (_globalOverride.HasValue)
            return new ToolPolicyDecision(_globalOverride.Value, tool.RiskLevel, ShouldDryRun(tool), $"Global override: {_globalOverride.Value}");

        if (_perToolOverrides.TryGetValue(tool.Name, out var overrideDecision))
            return new ToolPolicyDecision(overrideDecision, tool.RiskLevel, ShouldDryRun(tool, input), $"Per-tool override: {overrideDecision}");

        if (_alwaysDeny.Contains(tool.Name))
            return new ToolPolicyDecision(ApprovalDecision.Deny, tool.RiskLevel, false, "Blocked by safety policy");

        if (_alwaysAllow.Contains(tool.Name))
            return new ToolPolicyDecision(ApprovalDecision.Allow, tool.RiskLevel, false, "Low-risk allow list");

        var dryRun = ShouldDryRun(tool, input);

        return tool.RiskLevel switch
        {
            RiskLevel.Low => new ToolPolicyDecision(ApprovalDecision.Allow, tool.RiskLevel, dryRun, "Low risk tool"),
            RiskLevel.Medium => new ToolPolicyDecision(ApprovalDecision.Ask, tool.RiskLevel, dryRun, "Medium risk requires approval"),
            RiskLevel.High => new ToolPolicyDecision(ApprovalDecision.Ask, tool.RiskLevel, dryRun, "High risk requires approval"),
            RiskLevel.Critical => new ToolPolicyDecision(ApprovalDecision.Deny, tool.RiskLevel, dryRun, "Critical risk denied by default"),
            _ => new ToolPolicyDecision(ApprovalDecision.Allow, tool.RiskLevel, dryRun, "Default allow")
        };
    }

    public ApprovalDecision Evaluate(ITool tool)
        => EvaluateDetailed(tool).Decision;

    public bool RequiresApproval(ITool tool)
        => EvaluateDetailed(tool).Decision == ApprovalDecision.Ask;

    public void SetGlobalOverride(ApprovalDecision? decision)
        => _globalOverride = decision;

    public void SetToolOverride(string toolName, ApprovalDecision decision)
        => _perToolOverrides[toolName] = decision;

    public IReadOnlyDictionary<string, ApprovalDecision> GetToolOverrides()
        => _perToolOverrides;

    public string BuildInputSummary(Dictionary<string, object?>? input)
    {
        if (input is null || input.Count == 0)
            return "(no input)";

        return string.Join(", ",
            input.OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .Select(pair => $"{pair.Key}={MaskValue(pair.Key, pair.Value)}"));
    }

    public PolicyAuditEntry BuildAuditEntry(ITool tool, ToolPolicyDecision decision, Dictionary<string, object?>? input)
        => new(
            tool.Name,
            decision.Decision.ToString(),
            decision.RiskLevel.ToString(),
            decision.DryRun,
            decision.Reason,
            BuildInputSummary(input));

    private bool IsDisabledByConfig(ITool tool)
    {
        return tool.Name switch
        {
            "run_shell" => !_options.Tools.AllowShell,
            "web_fetch" => !_options.Tools.AllowWebFetch,
            "web_search" => !_options.Tools.AllowWebSearch,
            "open_page" or "screenshot_page" => !_options.Tools.AllowBrowser,
            "query_db" => !_options.Tools.AllowDbRead,
            "upload_ftp" => !_options.Tools.AllowFtpUpload,
            "git_push" => !_options.Tools.AllowGitPush,
            _ => false
        };
    }

    private static string MaskValue(string key, object? value)
    {
        var text = value?.ToString() ?? "(null)";
        return key.Contains("password", StringComparison.OrdinalIgnoreCase)
               || key.Contains("token", StringComparison.OrdinalIgnoreCase)
               || key.Contains("secret", StringComparison.OrdinalIgnoreCase)
            ? "***"
            : text;
    }

    private static bool ShouldDryRun(ITool tool, Dictionary<string, object?>? input = null)
    {
        if (tool.Name is "git_push" or "upload_ftp")
            return true;

        if (tool.Name is "run_shell" && input is not null)
            return input.ContainsKey("dry_run") && string.Equals(input["dry_run"]?.ToString(), "true", StringComparison.OrdinalIgnoreCase);

        return false;
    }
}
