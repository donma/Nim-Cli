using System.Text.Json;
using NimCli.Contracts;
using NimCli.Infrastructure.Config;

namespace NimCli.Infrastructure;

public sealed class CliRuntimeStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _baseDirectory;
    private readonly string _runtimeDirectory;
    private readonly string _sessionsDirectory;
    private readonly string _checkpointsDirectory;
    private readonly string _stateFilePath;

    public CliRuntimeStore(string? baseDirectory = null)
    {
        _baseDirectory = string.IsNullOrWhiteSpace(baseDirectory) ? UserConfigStore.AppHomeDirectory : baseDirectory;
        _runtimeDirectory = Path.Combine(_baseDirectory, ".nim-cli-runtime");
        _sessionsDirectory = Path.Combine(_runtimeDirectory, "sessions");
        _checkpointsDirectory = Path.Combine(_runtimeDirectory, "checkpoints");
        _stateFilePath = Path.Combine(_runtimeDirectory, "state.json");
        EnsureDirectories();
    }

    public RuntimeState LoadState()
    {
        EnsureDirectories();

        if (!File.Exists(_stateFilePath))
        {
            var state = new RuntimeState();
            SaveState(state);
            return state;
        }

        try
        {
            return JsonSerializer.Deserialize<RuntimeState>(File.ReadAllText(_stateFilePath), JsonOptions) ?? new RuntimeState();
        }
        catch
        {
            return new RuntimeState();
        }
    }

    public void SaveState(RuntimeState state)
    {
        EnsureDirectories();
        File.WriteAllText(_stateFilePath, JsonSerializer.Serialize(state, JsonOptions));
    }

    public IReadOnlyList<StoredSessionSummary> ListSessions(string workspaceKey)
    {
        EnsureDirectories();
        return Directory.GetFiles(_sessionsDirectory, "*.json", SearchOption.TopDirectoryOnly)
            .Select(LoadSessionFile)
            .Where(session => session is not null && session.WorkspaceKey.Equals(workspaceKey, StringComparison.OrdinalIgnoreCase))
            .Select(session => new StoredSessionSummary(session!.SessionId, session.Title, session.LastUpdatedUtc, session.Messages.Count))
            .OrderByDescending(session => session.LastUpdatedUtc)
            .ToList();
    }

    public StoredSession? LoadLatestSession(string workspaceKey)
        => Directory.GetFiles(_sessionsDirectory, "*.json", SearchOption.TopDirectoryOnly)
            .Select(LoadSessionFile)
            .Where(session => session is not null && session.WorkspaceKey.Equals(workspaceKey, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(session => session!.LastUpdatedUtc)
            .FirstOrDefault();

    public StoredSession? LoadSession(string sessionId)
    {
        var path = GetSessionPath(sessionId);
        if (!File.Exists(path))
            return null;

        return LoadSessionFile(path);
    }

    private StoredSession? LoadSessionFile(string path)
    {
        if (!File.Exists(path))
            return null;

        try
        {
            return JsonSerializer.Deserialize<StoredSession>(File.ReadAllText(path), JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public void SaveSession(StoredSession session)
    {
        EnsureDirectories();
        session.LastUpdatedUtc = DateTimeOffset.UtcNow;
        File.WriteAllText(GetSessionPath(session.SessionId), JsonSerializer.Serialize(session, JsonOptions));
    }

    public bool DeleteSession(string sessionId)
    {
        var path = GetSessionPath(sessionId);
        if (!File.Exists(path))
            return false;

        File.Delete(path);
        return true;
    }

    public IReadOnlyList<StoredCheckpoint> ListCheckpoints(string workspaceKey)
        => Directory.GetFiles(_checkpointsDirectory, "*.json", SearchOption.TopDirectoryOnly)
            .Select(LoadCheckpoint)
            .Where(checkpoint => checkpoint is not null && checkpoint.WorkspaceKey.Equals(workspaceKey, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(checkpoint => checkpoint!.CreatedUtc)
            .Cast<StoredCheckpoint>()
            .ToList();

    public StoredCheckpoint? LoadCheckpointByTag(string workspaceKey, string tag)
        => ListCheckpoints(workspaceKey).FirstOrDefault(checkpoint => checkpoint.Tag.Equals(tag, StringComparison.OrdinalIgnoreCase));

    public void SaveCheckpoint(StoredCheckpoint checkpoint)
    {
        EnsureDirectories();
        File.WriteAllText(GetCheckpointPath(checkpoint.CheckpointId), JsonSerializer.Serialize(checkpoint, JsonOptions));
    }

    public bool DeleteCheckpoint(string checkpointId)
    {
        var path = GetCheckpointPath(checkpointId);
        if (!File.Exists(path))
            return false;

        File.Delete(path);
        return true;
    }

    private StoredCheckpoint? LoadCheckpoint(string path)
    {
        try
        {
            return JsonSerializer.Deserialize<StoredCheckpoint>(File.ReadAllText(path), JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private string GetSessionPath(string sessionId)
        => Path.Combine(_sessionsDirectory, $"{sessionId}.json");

    private string GetCheckpointPath(string checkpointId)
        => Path.Combine(_checkpointsDirectory, $"{checkpointId}.json");

    private void EnsureDirectories()
    {
        Directory.CreateDirectory(_runtimeDirectory);
        Directory.CreateDirectory(_sessionsDirectory);
        Directory.CreateDirectory(_checkpointsDirectory);
    }
}

public sealed class RuntimeState
{
    public SettingsDocument Settings { get; set; } = new();
    public RegistryDocument Extensions { get; set; } = new();
    public RegistryDocument Skills { get; set; } = new();
    public RegistryDocument Hooks { get; set; } = new();
    public McpRegistryDocument Mcp { get; set; } = new();
    public ConsentDocument Consent { get; set; } = new();
}

public sealed class SettingsDocument
{
    public bool VimMode { get; set; }
    public string Theme { get; set; } = "default";
    public string ApprovalMode { get; set; } = "default";
    public bool TelemetryConsent { get; set; }
    public List<string> TrustedFolders { get; set; } = [];
    public string? PreferredEditor { get; set; }
}

public sealed class RegistryDocument
{
    public List<RegistryItem> Items { get; set; } = [];
}

public sealed class RegistryItem
{
    public string Name { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public bool AutoUpdate { get; set; }
    public string? Reference { get; set; }
    public string? Description { get; set; }
}

public sealed class McpRegistryDocument
{
    public List<McpServerEntry> Servers { get; set; } = [];
}

public sealed class McpServerEntry
{
    public string Name { get; set; } = string.Empty;
    public string CommandOrUrl { get; set; } = string.Empty;
    public string Transport { get; set; } = "stdio";
    public bool Enabled { get; set; } = true;
    public string Scope { get; set; } = "project";
    public List<string> IncludedTools { get; set; } = [];
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class ConsentDocument
{
    public bool PrivacyAccepted { get; set; }
    public DateTimeOffset? AcceptedUtc { get; set; }
}

public sealed class StoredSession
{
    public string SessionId { get; set; } = Guid.NewGuid().ToString("N");
    public string WorkspaceKey { get; set; } = string.Empty;
    public string Title { get; set; } = "Untitled session";
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastUpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public List<ChatMessage> Messages { get; set; } = [];
    public List<string> ToolHistory { get; set; } = [];
    public List<string> WorkspaceDirectories { get; set; } = [];
    public string WorkingDirectory { get; set; } = Directory.GetCurrentDirectory();
    public string Mode { get; set; } = "Analysis";
    public string? LastRepoMap { get; set; }
    public string? LastShellCommand { get; set; }
    public string? LastShellOutput { get; set; }
    public string? LastWebUrl { get; set; }
    public string? LastWebContent { get; set; }
    public string? LastDbQuery { get; set; }
    public string? LastDbResult { get; set; }
    public string? LastBuildSummary { get; set; }
    public string? LastTestSummary { get; set; }
    public string? LastScreenshotPath { get; set; }
    public string? LastSuggestedCommitMessage { get; set; }
    public string? LastDebugRequestJson { get; set; }
    public string? CurrentTask { get; set; }
    public string? LastContextStrategy { get; set; }
    public List<string> RecentActions { get; set; } = [];
    public List<PolicyAuditEntry> PolicyAuditTrail { get; set; } = [];
}

public sealed class StoredCheckpoint
{
    public string CheckpointId { get; set; } = Guid.NewGuid().ToString("N");
    public string WorkspaceKey { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public StoredSession Session { get; set; } = new();
}

public sealed record StoredSessionSummary(string SessionId, string Title, DateTimeOffset LastUpdatedUtc, int MessageCount);
