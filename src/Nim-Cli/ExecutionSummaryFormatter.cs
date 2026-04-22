using System.Text;
using NimCli.Contracts;
using NimCli.Core;

namespace NimCli.App;

public sealed class ExecutionSummaryFormatter
{
    public ExecutionSummary BuildExecutionSummary(AgentResponse response, SessionState session, long elapsedMilliseconds)
    {
        var toolsUsed = response.ToolResults?
            .Select(result => result.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];

        var outputSummaries = new List<string>();
        if (!string.IsNullOrWhiteSpace(session.LastShellCommand))
            outputSummaries.Add($"Shell: {session.LastShellCommand}");

        if (!string.IsNullOrWhiteSpace(session.LastDbQuery))
            outputSummaries.Add($"DB: {TrimSingleLine(session.LastDbQuery, 100)}");

        if (!string.IsNullOrWhiteSpace(session.LastWebUrl))
            outputSummaries.Add($"Web: {session.LastWebUrl}");

        if (!string.IsNullOrWhiteSpace(session.LastBuildSummary))
            outputSummaries.Add($"Build: {TrimSingleLine(session.LastBuildSummary, 100)}");

        if (!string.IsNullOrWhiteSpace(session.LastTestSummary))
            outputSummaries.Add($"Test: {TrimSingleLine(session.LastTestSummary, 100)}");

        if (!string.IsNullOrWhiteSpace(session.LastScreenshotPath))
            outputSummaries.Add($"Screenshot: {session.LastScreenshotPath}");

        if (!string.IsNullOrWhiteSpace(session.LastRepoMap))
            outputSummaries.Add($"RepoMap: {TrimSingleLine(session.LastRepoMap, 100)}");

        if (!string.IsNullOrWhiteSpace(session.LastSuggestedCommitMessage))
            outputSummaries.Add($"Commit Suggestion: {TrimSingleLine(session.LastSuggestedCommitMessage, 100)}");

        var artifacts = new List<ExecutionArtifact>();
        if (!string.IsNullOrWhiteSpace(session.LastWebUrl))
            artifacts.Add(new ExecutionArtifact("web", session.LastWebUrl));

        if (!string.IsNullOrWhiteSpace(session.LastScreenshotPath))
            artifacts.Add(new ExecutionArtifact("screenshot", session.LastScreenshotPath));

        if (!string.IsNullOrWhiteSpace(session.LastSuggestedCommitMessage))
            artifacts.Add(new ExecutionArtifact("suggested_commit_message", session.LastSuggestedCommitMessage));

        if (!string.IsNullOrWhiteSpace(session.LastRepoMap))
            artifacts.Add(new ExecutionArtifact("repo_map", TrimSingleLine(session.LastRepoMap, 180)));

        if (!string.IsNullOrWhiteSpace(session.LastBuildSummary))
            artifacts.Add(new ExecutionArtifact("build_summary", session.LastBuildSummary));

        if (!string.IsNullOrWhiteSpace(session.LastTestSummary))
            artifacts.Add(new ExecutionArtifact("test_summary", session.LastTestSummary));

        if (!string.IsNullOrWhiteSpace(session.LastContextStrategy))
            outputSummaries.Add($"Context: {session.LastContextStrategy}");

        if (!string.IsNullOrWhiteSpace(session.CurrentTask))
            outputSummaries.Add($"Task: {TrimSingleLine(session.CurrentTask, 100)}");

        var warnings = response.ToolResults?
            .Where(result => result.IsError)
            .Select(result => new ExecutionWarning(result.Name, TrimSingleLine(result.ResultJson, 160)))
            .ToList() ?? [];

        var toolResultSummaries = response.ToolResults?
            .TakeLast(6)
            .Select(result => $"{result.Name}: {TrimSingleLine(result.ResultJson, 140)}")
            .ToList() ?? [];

        var approvalActions = new List<string>();
        if (response.RequiresApproval && !string.IsNullOrWhiteSpace(response.ApprovalPrompt))
            approvalActions.Add(response.ApprovalPrompt);

        if (response.ApprovalRequest is not null)
            approvalActions.Add($"{response.ApprovalRequest.ToolName}: {response.ApprovalRequest.Reason} | dry-run={response.ApprovalRequest.DryRun}");

        return new ExecutionSummary(
            Success: warnings.Count == 0,
            FinalMessage: response.Content,
            ToolsUsed: toolsUsed,
            OutputSummaries: outputSummaries,
            Warnings: warnings,
            Artifacts: artifacts,
            ApprovalActions: approvalActions,
            PolicyDecisions: session.PolicyAuditTrail.ToList(),
            ToolResultSummaries: toolResultSummaries,
            ElapsedMilliseconds: elapsedMilliseconds);
    }

    public SessionSummary BuildSessionSummary(SessionState session)
        => new(
            session.SessionId,
            session.WorkspaceKey,
            session.WorkingDirectory,
            session.Mode.ToString(),
            session.ConversationHistory.Count,
            session.ToolExecutionHistory.Count,
            session.WorkspaceDirectories.ToList(),
            session.LastShellCommand,
            session.LastShellOutput,
            session.LastDbQuery,
            session.LastDbResult,
            session.LastWebUrl,
            session.LastWebContent,
            session.LastBuildSummary,
            session.LastTestSummary,
            session.LastScreenshotPath,
            session.LastSuggestedCommitMessage,
            !string.IsNullOrWhiteSpace(session.LastDebugRequestJson),
            session.CurrentTask,
            session.LastContextStrategy,
            session.RecentActions.ToList(),
            session.PolicyAuditTrail.ToList());

    public string FormatExecutionSummary(ExecutionSummary summary)
    {
        var builder = new StringBuilder();
        builder.AppendLine(TrimSingleLine(summary.FinalMessage, 160));

        if (summary.ToolsUsed?.Count > 0)
            builder.AppendLine($"Tools used: {string.Join(", ", summary.ToolsUsed)}");

        if (summary.OutputSummaries?.Count > 0)
            foreach (var item in summary.OutputSummaries)
                builder.AppendLine(item);

        if (summary.Warnings?.Count > 0)
            foreach (var warning in summary.Warnings)
                builder.AppendLine($"Warning [{warning.Source}]: {warning.Message}");

        if (summary.ToolResultSummaries?.Count > 0)
            foreach (var toolResult in summary.ToolResultSummaries)
                builder.AppendLine($"Tool Result: {toolResult}");

        if (summary.ApprovalActions?.Count > 0)
            foreach (var action in summary.ApprovalActions)
                builder.AppendLine($"Approval: {TrimSingleLine(action, 160)}");

        if (summary.PolicyDecisions?.Count > 0)
            foreach (var decision in summary.PolicyDecisions.TakeLast(6))
                builder.AppendLine($"Policy [{decision.ToolName}]: {decision.Decision}/{decision.RiskLevel} dry-run={decision.DryRun} ({decision.Reason})");

        if (summary.Artifacts?.Count > 0)
            foreach (var artifact in summary.Artifacts.TakeLast(6))
                builder.AppendLine($"Artifact [{artifact.Kind}]: {TrimSingleLine(artifact.Value, 140)}");

        builder.AppendLine($"Elapsed: {summary.ElapsedMilliseconds} ms");
        return builder.ToString().TrimEnd();
    }

    private static string TrimSingleLine(string value, int maxLength)
    {
        var normalized = value.Replace("\r", " ").Replace("\n", " ").Trim();
        if (normalized.Length <= maxLength)
            return normalized;

        return normalized[..Math.Max(0, maxLength - 3)] + "...";
    }
}
