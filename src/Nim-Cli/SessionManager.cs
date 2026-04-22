using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NimCli.Contracts;
using NimCli.Core;
using NimCli.Infrastructure;

namespace NimCli.App;

public sealed class SessionManager
{
    private readonly CliRuntimeStore _runtimeStore;

    public SessionManager(CliRuntimeStore runtimeStore)
    {
        _runtimeStore = runtimeStore;
    }

    public string ComputeWorkspaceKey(string workingDirectory)
    {
        var normalized = Path.GetFullPath(workingDirectory).Trim().ToLowerInvariant();
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(hash)[..16].ToLowerInvariant();
    }

    public void InitializeNewSession(SessionState session, string workingDirectory, IEnumerable<string> workspaceDirectories)
    {
        session.WorkingDirectory = Path.GetFullPath(workingDirectory);
        session.WorkspaceKey = ComputeWorkspaceKey(session.WorkingDirectory);
        session.SessionId = Guid.NewGuid().ToString("N");
        session.SetWorkspaceDirectories(workspaceDirectories);
    }

    public IReadOnlyList<StoredSessionSummary> ListSessions(string workspaceKey)
        => _runtimeStore.ListSessions(workspaceKey);

    public bool DeleteSession(string sessionId)
        => _runtimeStore.DeleteSession(sessionId);

    public StoredSession? LoadLatest(string workspaceKey)
        => _runtimeStore.LoadLatestSession(workspaceKey);

    public StoredSession? LoadByReference(string workspaceKey, string reference)
    {
        if (reference.Equals("latest", StringComparison.OrdinalIgnoreCase))
            return LoadLatest(workspaceKey);

        if (int.TryParse(reference, out var index) && index > 0)
        {
            var sessions = ListSessions(workspaceKey);
            return index <= sessions.Count ? _runtimeStore.LoadSession(sessions[index - 1].SessionId) : null;
        }

        return _runtimeStore.LoadSession(reference);
    }

    public void RestoreSession(SessionState session, StoredSession stored)
    {
        session.SessionId = stored.SessionId;
        session.WorkspaceKey = stored.WorkspaceKey;
        session.WorkingDirectory = stored.WorkingDirectory;
        session.Mode = Enum.TryParse<AgentMode>(stored.Mode, out var mode) ? mode : AgentMode.Analysis;
        session.Clear();
        session.SetWorkspaceDirectories(stored.WorkspaceDirectories.Count == 0 ? [stored.WorkingDirectory] : stored.WorkspaceDirectories);

        foreach (var message in stored.Messages)
            session.ConversationHistory.Add(message);

        foreach (var history in stored.ToolHistory)
            session.ToolExecutionHistory.Add(history);

        if (!string.IsNullOrWhiteSpace(stored.LastRepoMap))
            session.RecordRepoMap(stored.LastRepoMap);

        if (!string.IsNullOrWhiteSpace(stored.LastShellCommand) || !string.IsNullOrWhiteSpace(stored.LastShellOutput))
            session.RecordShellResult(stored.LastShellCommand ?? string.Empty, stored.LastShellOutput ?? string.Empty);

        if (!string.IsNullOrWhiteSpace(stored.LastWebUrl) || !string.IsNullOrWhiteSpace(stored.LastWebContent))
            session.RecordWebResult(stored.LastWebUrl ?? string.Empty, stored.LastWebContent ?? string.Empty);

        if (!string.IsNullOrWhiteSpace(stored.LastDbQuery) || !string.IsNullOrWhiteSpace(stored.LastDbResult))
            session.RecordDbResult(stored.LastDbQuery ?? string.Empty, stored.LastDbResult ?? string.Empty);

        if (!string.IsNullOrWhiteSpace(stored.LastBuildSummary))
            session.RecordBuildSummary(stored.LastBuildSummary);

        if (!string.IsNullOrWhiteSpace(stored.LastTestSummary))
            session.RecordTestSummary(stored.LastTestSummary);

        if (!string.IsNullOrWhiteSpace(stored.LastScreenshotPath))
            session.RecordScreenshotPath(stored.LastScreenshotPath);

        if (!string.IsNullOrWhiteSpace(stored.LastSuggestedCommitMessage))
            session.RecordSuggestedCommitMessage(stored.LastSuggestedCommitMessage);

        if (!string.IsNullOrWhiteSpace(stored.LastDebugRequestJson))
            session.RecordDebugRequest(stored.LastDebugRequestJson);

        if (!string.IsNullOrWhiteSpace(stored.CurrentTask))
            session.RecordCurrentTask(stored.CurrentTask);

        if (!string.IsNullOrWhiteSpace(stored.LastContextStrategy))
            session.RecordContextStrategy(stored.LastContextStrategy);

        foreach (var action in stored.RecentActions)
            session.AddRecentAction(action);

        foreach (var audit in stored.PolicyAuditTrail)
            session.AddPolicyAudit(audit);
    }

    public void SaveSession(SessionState session)
    {
        var stored = new StoredSession
        {
            SessionId = session.SessionId,
            WorkspaceKey = session.WorkspaceKey,
            Title = BuildSessionTitle(session),
            Messages = session.ConversationHistory.ToList(),
            ToolHistory = session.ToolExecutionHistory.ToList(),
            WorkspaceDirectories = session.WorkspaceDirectories.ToList(),
            WorkingDirectory = session.WorkingDirectory,
            Mode = session.Mode.ToString(),
            LastRepoMap = session.LastRepoMap,
            LastShellCommand = session.LastShellCommand,
            LastShellOutput = session.LastShellOutput,
            LastWebUrl = session.LastWebUrl,
            LastWebContent = session.LastWebContent,
            LastDbQuery = session.LastDbQuery,
            LastDbResult = session.LastDbResult,
            LastBuildSummary = session.LastBuildSummary,
            LastTestSummary = session.LastTestSummary,
            LastScreenshotPath = session.LastScreenshotPath,
            LastSuggestedCommitMessage = session.LastSuggestedCommitMessage,
            LastDebugRequestJson = session.LastDebugRequestJson,
            CurrentTask = session.CurrentTask,
            LastContextStrategy = session.LastContextStrategy,
            RecentActions = session.RecentActions.ToList(),
            PolicyAuditTrail = session.PolicyAuditTrail.ToList()
        };

        var existing = _runtimeStore.LoadSession(session.SessionId);
        if (existing is not null)
            stored.CreatedUtc = existing.CreatedUtc;

        _runtimeStore.SaveSession(stored);
    }

    public IReadOnlyList<StoredCheckpoint> ListCheckpoints(string workspaceKey)
        => _runtimeStore.ListCheckpoints(workspaceKey);

    public void SaveCheckpoint(SessionState session, string tag)
    {
        SaveSession(session);

        var storedSession = _runtimeStore.LoadSession(session.SessionId);
        if (storedSession is null)
            return;

        _runtimeStore.SaveCheckpoint(new StoredCheckpoint
        {
            WorkspaceKey = session.WorkspaceKey,
            SessionId = session.SessionId,
            Tag = tag,
            Session = storedSession
        });
    }

    public bool DeleteCheckpoint(string checkpointId)
        => _runtimeStore.DeleteCheckpoint(checkpointId);

    public StoredCheckpoint? LoadCheckpoint(string workspaceKey, string tag)
        => _runtimeStore.LoadCheckpointByTag(workspaceKey, tag);

    public string ExportSession(SessionState session, string? fileName)
    {
        var extension = Path.GetExtension(fileName ?? string.Empty);
        var outputPath = string.IsNullOrWhiteSpace(fileName)
            ? Path.Combine(session.WorkingDirectory, $"nim-cli-session-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.md")
            : Path.GetFullPath(fileName!);

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? session.WorkingDirectory);

        if (extension.Equals(".json", StringComparison.OrdinalIgnoreCase))
        {
            var payload = new
            {
                session.SessionId,
                session.WorkingDirectory,
                Mode = session.Mode.ToString(),
                Messages = session.ConversationHistory,
                Tools = session.ToolExecutionHistory
            };
            File.WriteAllText(outputPath, JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
        }
        else
        {
            var builder = new StringBuilder();
            builder.AppendLine($"# Nim-Cli Session {session.SessionId}");
            builder.AppendLine();
            builder.AppendLine($"Working Directory: `{session.WorkingDirectory}`");
            builder.AppendLine($"Mode: `{session.Mode}`");
            builder.AppendLine();

            foreach (var message in session.ConversationHistory)
            {
                builder.AppendLine($"## {message.Role}");
                builder.AppendLine();
                builder.AppendLine(message.Content);
                builder.AppendLine();
            }

            File.WriteAllText(outputPath, builder.ToString());
        }

        return outputPath;
    }

    private static string BuildSessionTitle(SessionState session)
    {
        var firstUserMessage = session.ConversationHistory.FirstOrDefault(message => message.Role == "user")?.Content?.Trim();
        if (string.IsNullOrWhiteSpace(firstUserMessage))
            return $"Session {session.SessionId[..8]}";

        return firstUserMessage.Length <= 80 ? firstUserMessage : firstUserMessage[..80];
    }
}
