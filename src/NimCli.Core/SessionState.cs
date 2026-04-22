using NimCli.Contracts;

namespace NimCli.Core;

public enum AgentMode { Analysis, Coding, Ops }

public class SessionState
{
    public string SessionId { get; set; } = Guid.NewGuid().ToString("N");
    public string WorkspaceKey { get; set; } = string.Empty;
    public string WorkingDirectory { get; set; } = Directory.GetCurrentDirectory();
    public AgentMode Mode { get; set; } = AgentMode.Analysis;
    public List<string> WorkspaceDirectories { get; } = [];
    public List<ChatMessage> ConversationHistory { get; } = [];
    public List<string> ToolExecutionHistory { get; } = [];
    public Dictionary<string, string> UserPreferences { get; } = new();
    public string? LastRepoMap { get; private set; }
    public string? LastShellCommand { get; private set; }
    public string? LastShellOutput { get; private set; }
    public string? LastWebUrl { get; private set; }
    public string? LastWebContent { get; private set; }
    public string? LastDbQuery { get; private set; }
    public string? LastDbResult { get; private set; }
    public string? LastBuildSummary { get; private set; }
    public string? LastTestSummary { get; private set; }
    public string? LastScreenshotPath { get; private set; }
    public string? LastSuggestedCommitMessage { get; private set; }
    public string? LastDebugRequestJson { get; private set; }
    public string? CurrentTask { get; private set; }
    public string? LastContextStrategy { get; private set; }
    public List<string> RecentActions { get; } = [];
    public List<PolicyAuditEntry> PolicyAuditTrail { get; } = [];

    public void AddUserMessage(string content)
        => ConversationHistory.Add(new ChatMessage("user", content));

    public void AddAssistantMessage(string content)
        => ConversationHistory.Add(new ChatMessage("assistant", content));

    public void AddToolResultMessage(string toolName, string result)
    {
        ConversationHistory.Add(new ChatMessage("tool", $"[{toolName}]: {result}"));
        ToolExecutionHistory.Add($"{DateTime.Now:HH:mm:ss} [{toolName}] {result[..Math.Min(100, result.Length)]}");
    }

    public void RecordRepoMap(string repoMap) => LastRepoMap = repoMap;

    public void RecordShellResult(string command, string output)
    {
        LastShellCommand = command;
        LastShellOutput = output;
    }

    public void RecordWebResult(string url, string content)
    {
        LastWebUrl = url;
        LastWebContent = content;
    }

    public void RecordDbResult(string query, string result)
    {
        LastDbQuery = query;
        LastDbResult = result;
    }

    public void RecordBuildSummary(string summary) => LastBuildSummary = summary;

    public void RecordTestSummary(string summary) => LastTestSummary = summary;

    public void RecordScreenshotPath(string path) => LastScreenshotPath = path;

    public void RecordSuggestedCommitMessage(string message) => LastSuggestedCommitMessage = message;

    public void RecordDebugRequest(string requestJson) => LastDebugRequestJson = requestJson;

    public void RecordCurrentTask(string task) => CurrentTask = task;

    public void RecordContextStrategy(string strategy) => LastContextStrategy = strategy;

    public void AddRecentAction(string action)
    {
        if (string.IsNullOrWhiteSpace(action))
            return;

        RecentActions.Add(action);
        while (RecentActions.Count > 12)
            RecentActions.RemoveAt(0);
    }

    public void AddPolicyAudit(PolicyAuditEntry entry)
    {
        PolicyAuditTrail.Add(entry);
        while (PolicyAuditTrail.Count > 12)
            PolicyAuditTrail.RemoveAt(0);
    }

    public void SetWorkspaceDirectories(IEnumerable<string> directories)
    {
        WorkspaceDirectories.Clear();
        WorkspaceDirectories.Add(WorkingDirectory);

        foreach (var directory in directories.Where(static dir => !string.IsNullOrWhiteSpace(dir)).Distinct(StringComparer.OrdinalIgnoreCase))
            WorkspaceDirectories.Add(Path.GetFullPath(directory));
    }

    public void Clear()
    {
        ConversationHistory.Clear();
        ToolExecutionHistory.Clear();
        WorkspaceDirectories.Clear();
        LastRepoMap = null;
        LastShellCommand = null;
        LastShellOutput = null;
        LastWebUrl = null;
        LastWebContent = null;
        LastDbQuery = null;
        LastDbResult = null;
        LastBuildSummary = null;
        LastTestSummary = null;
        LastScreenshotPath = null;
        LastSuggestedCommitMessage = null;
        LastDebugRequestJson = null;
        CurrentTask = null;
        LastContextStrategy = null;
        RecentActions.Clear();
        PolicyAuditTrail.Clear();
    }
}
