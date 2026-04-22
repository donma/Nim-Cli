using NimCli.Contracts;

namespace NimCli.Core;

public class ContextBuilder
{
    private readonly List<ContextBlock> _contextParts = [];
    private string _lastStrategy = "general";

    public string LastStrategy => _lastStrategy;

    public void AddSessionState(SessionState session)
    {
        AddSessionState(session, session.Mode);
    }

    public void AddSessionState(SessionState session, AgentMode mode)
    {
        _lastStrategy = mode switch
        {
            _ when session.UserPreferences.ContainsKey("tui_focus") => "tui-interactive",
            AgentMode.Coding => "coding",
            AgentMode.Ops => "ops",
            _ => session.CurrentTask?.Contains("resume", StringComparison.OrdinalIgnoreCase) == true ? "resume" : "analysis"
        };

        AddBlock("Context Strategy", _lastStrategy, "strategy", GetPriority(mode, "strategy"), preserveEdges: true);

        if (session.WorkspaceDirectories.Count > 0)
            AddBlock("Workspace Directories", string.Join("\n", session.WorkspaceDirectories), "workspace", GetPriority(mode, "workspace"), preserveEdges: true);

        if (!string.IsNullOrWhiteSpace(session.CurrentTask))
            AddBlock("Current Task", session.CurrentTask, "task", GetPriority(mode, "task"), preserveEdges: true);

        if (session.ConversationHistory.Count > 0)
            AddBlock("Recent Conversation", BuildConversationSummary(session.ConversationHistory, _lastStrategy), "conversation", GetPriority(mode, "conversation"), preserveEdges: true);

        if (!string.IsNullOrWhiteSpace(session.LastRepoMap))
            AddRepoMap(session.LastRepoMap);

        if (!string.IsNullOrWhiteSpace(session.LastShellCommand) && !string.IsNullOrWhiteSpace(session.LastShellOutput))
            AddShellOutput(session.LastShellCommand, session.LastShellOutput);

        if (!string.IsNullOrWhiteSpace(session.LastWebUrl) && !string.IsNullOrWhiteSpace(session.LastWebContent))
            AddWebResult(session.LastWebUrl, session.LastWebContent);

        if (!string.IsNullOrWhiteSpace(session.LastDbQuery) && !string.IsNullOrWhiteSpace(session.LastDbResult))
            AddDbResult(session.LastDbQuery, session.LastDbResult);

        if (!string.IsNullOrWhiteSpace(session.LastBuildSummary))
            AddToolResult("build", session.LastBuildSummary);

        if (!string.IsNullOrWhiteSpace(session.LastTestSummary))
            AddToolResult("test", session.LastTestSummary);

        if (!string.IsNullOrWhiteSpace(session.LastScreenshotPath))
            AddToolResult("screenshot", session.LastScreenshotPath);

        if (!string.IsNullOrWhiteSpace(session.LastSuggestedCommitMessage))
            AddBlock("Suggested Commit Message", session.LastSuggestedCommitMessage, "commit", GetPriority(mode, "commit"));

        if (session.RecentActions.Count > 0)
            AddBlock("Recent Actions", string.Join("\n", session.RecentActions.TakeLast(8)), "recent", GetPriority(mode, "recent"), preserveEdges: true);

        if (session.PolicyAuditTrail.Count > 0)
            AddBlock("Policy Audit", string.Join("\n", session.PolicyAuditTrail.TakeLast(6).Select(entry => $"{entry.ToolName}: {entry.Decision} / {entry.RiskLevel} / dry-run={entry.DryRun} / {entry.Reason}")), "policy", GetPriority(mode, "policy"), preserveEdges: true);
    }

    public void AddFileContent(string filePath, string content)
        => AddBlock($"File: {filePath}", content, "file", 70, preserveEdges: true, codeFence: true);

    public void AddRepoMap(string repoMap)
        => AddBlock("Repo Map", repoMap, "repo_map", 82, preserveEdges: true);

    public void AddShellOutput(string command, string output)
        => AddBlock($"Shell: {command}", output, "shell", 74, preserveEdges: true);

    public void AddToolResult(string toolName, string result)
        => AddBlock($"Tool Result: {toolName}", result, "tool_result", 72, preserveEdges: true);

    public void AddWebResult(string url, string content)
        => AddBlock($"Web: {url}", content, "web", 64, preserveEdges: true);

    public void AddDbResult(string query, string result)
        => AddBlock($"DB Query: {query}", result, "db", 76, preserveEdges: true);

    public string Build(int maxChars = 6000)
    {
        if (_contextParts.Count == 0)
            return string.Empty;

        var ordered = _contextParts
            .OrderByDescending(block => block.Priority)
            .ThenBy(block => block.Sequence)
            .ToList();

        var builder = new List<string>();
        var used = 0;
        var omitted = 0;
        var compressedBlocks = new List<string>();
        var includedKinds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var omittedKinds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var auditReserve = Math.Min(420, Math.Max(140, maxChars / 6));

        foreach (var block in ordered)
        {
            var remaining = maxChars - used - auditReserve;
            if (remaining <= 120)
            {
                omitted++;
                omittedKinds.Add(block.Kind);
                continue;
            }

            var rendered = RenderBlock(block, remaining);
            if (string.IsNullOrWhiteSpace(rendered.Text))
            {
                omitted++;
                omittedKinds.Add(block.Kind);
                continue;
            }

            builder.Add(rendered.Text);
            used += rendered.Text.Length + 2;
            includedKinds.Add(block.Kind);
            if (rendered.Compressed)
                compressedBlocks.Add(block.Title);
        }

        var audit = BuildAuditBlock(maxChars, used, includedKinds, omittedKinds, compressedBlocks, omitted);
        if (!string.IsNullOrWhiteSpace(audit))
            builder.Add(audit);

        var content = string.Join("\n\n", builder);
        if (content.Length <= maxChars)
            return content;

        if (!string.IsNullOrWhiteSpace(audit))
        {
            builder.RemoveAt(builder.Count - 1);
            content = string.Join("\n\n", builder);
            if (content.Length <= maxChars)
                return content;
        }

        while (builder.Count > 0 && content.Length > maxChars)
        {
            builder.RemoveAt(builder.Count - 1);
            content = string.Join("\n\n", builder);
        }

        return content;
    }

    public void Clear()
    {
        _contextParts.Clear();
        _lastStrategy = "general";
    }

    private void AddBlock(string title, string? content, string kind, int priority, bool preserveEdges = false, bool codeFence = false)
    {
        if (string.IsNullOrWhiteSpace(content))
            return;

        _contextParts.Add(new ContextBlock(_contextParts.Count, title, kind, content, priority, preserveEdges, codeFence));
    }

    private int GetPriority(AgentMode mode, string kind)
    {
        return (mode, kind) switch
        {
            (_, "strategy") => 100,
            (_, "task") => 96,
            (_, "conversation") when _lastStrategy == "resume" => 95,
            (_, "conversation") when _lastStrategy == "tui-interactive" => 93,
            (AgentMode.Coding, "policy") => 92,
            (AgentMode.Coding, "recent") => 90,
            (AgentMode.Coding, "conversation") => 89,
            (AgentMode.Coding, "workspace") => 88,
            (AgentMode.Ops, "policy") => 94,
            (AgentMode.Ops, "recent") => 92,
            (AgentMode.Ops, "conversation") => 90,
            (AgentMode.Analysis, "policy") => 86,
            (AgentMode.Analysis, "recent") => 84,
            (AgentMode.Analysis, "conversation") => 82,
            (_, "workspace") => 80,
            (_, "commit") => 70,
            _ => 60
        };
    }

    private static RenderedBlock RenderBlock(ContextBlock block, int remaining)
    {
        var header = $"[{block.Title}]\n";
        var fencePadding = block.CodeFence ? 8 : 0;
        var available = remaining - header.Length - fencePadding;
        if (available <= 24)
            return new RenderedBlock(string.Empty, false);

        var content = Compact(block.Content, available, block.PreserveEdges);
        var compressed = !string.Equals(content, block.Content, StringComparison.Ordinal);
        if (block.CodeFence)
            content = $"```\n{content}\n```";

        return new RenderedBlock(header + content, compressed);
    }

    private static string Compact(string content, int maxChars, bool preserveEdges)
    {
        if (content.Length <= maxChars)
            return content;

        if (content.Contains('\n'))
            return CompactMultiline(content, maxChars, preserveEdges);

        if (maxChars < 48)
            return content[..Math.Max(0, maxChars - 3)] + "...";

        if (!preserveEdges)
            return content[..Math.Max(0, maxChars - 23)] + "\n... [summary truncated]";

        var headSize = Math.Max(16, (maxChars - 26) / 2);
        var tailSize = Math.Max(12, maxChars - 26 - headSize);
        if (headSize + tailSize >= content.Length)
            return content;

        var head = content[..headSize].TrimEnd();
        var tail = content[^tailSize..].TrimStart();
        return $"{head}\n... [middle omitted] ...\n{tail}";
    }

    private static string CompactMultiline(string content, int maxChars, bool preserveEdges)
    {
        var lines = content.Split('\n');
        if (lines.Length <= 3)
            return Compact(content.Replace("\n", " "), maxChars, preserveEdges: false);

        if (!preserveEdges)
        {
            var builder = new List<string>();
            var used = 0;
            foreach (var line in lines)
            {
                var candidate = line.TrimEnd();
                if (used + candidate.Length + 1 >= maxChars - 24)
                    break;

                builder.Add(candidate);
                used += candidate.Length + 1;
            }

            builder.Add($"... [{Math.Max(1, lines.Length - builder.Count)} lines omitted]");
            return string.Join("\n", builder);
        }

        var headCount = Math.Max(1, Math.Min(4, lines.Length / 3));
        var tailCount = Math.Max(1, Math.Min(3, lines.Length / 4));
        var summary = $"... [{Math.Max(1, lines.Length - headCount - tailCount)} lines omitted] ...";
        var assembled = string.Join("\n", lines.Take(headCount).Concat([summary]).Concat(lines.TakeLast(tailCount)));

        if (assembled.Length <= maxChars)
            return assembled;

        while ((headCount > 1 || tailCount > 1) && assembled.Length > maxChars)
        {
            if (headCount >= tailCount && headCount > 1)
                headCount--;
            else if (tailCount > 1)
                tailCount--;

            summary = $"... [{Math.Max(1, lines.Length - headCount - tailCount)} lines omitted] ...";
            assembled = string.Join("\n", lines.Take(headCount).Concat([summary]).Concat(lines.TakeLast(tailCount)));
        }

        return assembled.Length <= maxChars
            ? assembled
            : Compact(assembled.Replace("\n", " "), maxChars, preserveEdges: false);
    }

    private static string BuildConversationSummary(IReadOnlyList<ChatMessage> history, string strategy)
    {
        var count = strategy == "resume" ? 10 : 6;
        return string.Join("\n", history.TakeLast(count).Select(static message => $"{message.Role}: {SingleLine(message.Content, 180)}"));
    }

    private static string BuildAuditBlock(int maxChars, int used, IReadOnlyCollection<string> includedKinds, IReadOnlyCollection<string> omittedKinds, IReadOnlyList<string> compressedBlocks, int omittedCount)
    {
        var lines = new List<string>
        {
            $"Budget: used {Math.Min(maxChars, used)} / {maxChars} chars",
            $"Included kinds: {(includedKinds.Count == 0 ? "(none)" : string.Join(", ", includedKinds.OrderBy(static kind => kind, StringComparer.OrdinalIgnoreCase)))}"
        };

        if (compressedBlocks.Count > 0)
            lines.Add($"Compressed: {string.Join(", ", compressedBlocks.Take(6))}");

        if (omittedCount > 0)
            lines.Add($"Omitted lower-priority blocks: {omittedCount} ({(omittedKinds.Count == 0 ? "unknown" : string.Join(", ", omittedKinds.OrderBy(static kind => kind, StringComparer.OrdinalIgnoreCase)))})");

        return "[Context Audit]\n" + string.Join("\n", lines);
    }

    private static string SingleLine(string value, int maxLength)
    {
        var normalized = value.Replace("\r", " ").Replace("\n", " ").Trim();
        if (normalized.Length <= maxLength)
            return normalized;

        return normalized[..Math.Max(0, maxLength - 3)] + "...";
    }

    private sealed record ContextBlock(int Sequence, string Title, string Kind, string Content, int Priority, bool PreserveEdges, bool CodeFence);
    private sealed record RenderedBlock(string Text, bool Compressed);
}
