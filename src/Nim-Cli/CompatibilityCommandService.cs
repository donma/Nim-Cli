using System.Runtime.InteropServices;
using System.Text;
using NimCli.Core;
using NimCli.Infrastructure;
using NimCli.Infrastructure.Config;
using NimCli.Tools.Shell;

namespace NimCli.App;

public sealed class CompatibilityCommandService
{
    private readonly SessionManager _sessionManager;
    private readonly WorkspaceCommandService _workspace;
    private readonly NimCliOptions _options;
    private readonly IShellProvider _shellProvider;
    private readonly PolicySummaryService _policySummaryService;
    private readonly CommandCatalogService _commandCatalogService;

    public CompatibilityCommandService(SessionManager sessionManager, WorkspaceCommandService workspace, NimCliOptions options, IShellProvider shellProvider, PolicySummaryService policySummaryService, CommandCatalogService commandCatalogService)
    {
        _sessionManager = sessionManager;
        _workspace = workspace;
        _options = options;
        _shellProvider = shellProvider;
        _policySummaryService = policySummaryService;
        _commandCatalogService = commandCatalogService;
    }

    public (int ExitCode, string Output) HandleCommands()
        => (0, _commandCatalogService.FormatSummary());

    public (int ExitCode, string Output) HandleCompatibility()
        => (0, _commandCatalogService.FormatCompatibilitySummary());

    public (int ExitCode, string Output) HandleAgents(SessionState session, string[] args)
    {
        if (args.Length == 0 || args[0].Equals("list", StringComparison.OrdinalIgnoreCase) || args[0].Equals("status", StringComparison.OrdinalIgnoreCase))
            return (0, $"Current agent mode: {session.Mode.ToString().ToLowerInvariant()}{Environment.NewLine}Available modes: analysis, coding, ops");

        var modeToken = args[0].Equals("set", StringComparison.OrdinalIgnoreCase) && args.Length >= 2
            ? args[1]
            : args[0];

        if (!TryMapMode(modeToken, out var mode))
            return (1, "Usage: nim-cli agents [list|status|set <analysis|coding|ops>]");

        session.Mode = mode;
        _sessionManager.SaveSession(session);
        return (0, $"Agent mode set to {mode.ToString().ToLowerInvariant()}.");
    }

    public (int ExitCode, string Output) HandlePolicies(SessionState session, string approvalMode, IReadOnlyList<string> includeDirectories)
    {
        var lines = new List<string>
        {
            $"Approval mode: {approvalMode}",
            $"Included directories: {(includeDirectories.Count == 0 ? "(none)" : string.Join(", ", includeDirectories))}",
            $"Trusted folders: {_workspace.ShowTrustedFolders()}",
            "Tool policies:",
            _policySummaryService.FormatSummaries(),
            $"Session workspace directories: {(session.WorkspaceDirectories.Count == 0 ? session.WorkingDirectory : string.Join(", ", session.WorkspaceDirectories))}"
        };

        return (0, string.Join(Environment.NewLine, lines));
    }

    public (int ExitCode, string Output) HandleCompress(SessionState session, string[] args)
    {
        EnsureSessionLoaded(session);

        if (session.ConversationHistory.Count == 0 && session.ToolExecutionHistory.Count == 0)
            return (0, "No active session history to compress.");

        var tag = args.Length >= 1 ? args[0] : $"compress-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}";
        _sessionManager.SaveCheckpoint(session, tag);

        const int keepMessages = 12;
        const int keepTools = 12;
        var removedMessages = Math.Max(0, session.ConversationHistory.Count - keepMessages);
        var removedTools = Math.Max(0, session.ToolExecutionHistory.Count - keepTools);

        if (removedMessages > 0)
            session.ConversationHistory.RemoveRange(0, removedMessages);

        if (removedTools > 0)
            session.ToolExecutionHistory.RemoveRange(0, removedTools);

        _sessionManager.SaveSession(session);

        if (removedMessages == 0 && removedTools == 0)
            return (0, $"Saved checkpoint '{tag}'. Session was already compact.");

        return (0, $"Saved checkpoint '{tag}' and removed {removedMessages} old messages plus {removedTools} old tool entries from the live session. Use /restore {tag} to recover it.");
    }

    public (int ExitCode, string Output) HandleRestore(SessionState session, string[] args, bool rewind)
    {
        var checkpoints = _sessionManager.ListCheckpoints(session.WorkspaceKey);
        if (checkpoints.Count == 0)
            return (1, "目前 workspace 沒有可用 checkpoint。先執行 compress 或 resume save <tag> 建立還原點。");

        if (args.Length >= 1)
        {
            var command = args[0];
            if (command.Equals("list", StringComparison.OrdinalIgnoreCase) || command.Equals("ls", StringComparison.OrdinalIgnoreCase))
                return (0, FormatCheckpointList(checkpoints, rewind));

            if (command.Equals("show", StringComparison.OrdinalIgnoreCase) || command.Equals("inspect", StringComparison.OrdinalIgnoreCase) || command.Equals("status", StringComparison.OrdinalIgnoreCase))
            {
                var showReference = args.Length >= 2 ? args[1] : "latest";
                var showCheckpoint = ResolveCheckpoint(checkpoints, showReference);
                return showCheckpoint is null
                    ? (1, $"找不到 checkpoint：{showReference}")
                    : (0, FormatCheckpointDetails(showCheckpoint, rewind));
            }
        }

        var reference = args.Length >= 1 ? args[0] : "latest";
        var checkpoint = ResolveCheckpoint(checkpoints, reference);
        if (checkpoint is null)
            return (1, $"找不到 checkpoint：{reference}");

        _sessionManager.RestoreSession(session, checkpoint.Session);
        _sessionManager.SaveSession(session);
        return (0, $"{(rewind ? "已退回" : "已還原")} checkpoint '{checkpoint.Tag}'。訊息 {checkpoint.Session.Messages.Count} 筆、工具紀錄 {checkpoint.Session.ToolHistory.Count} 筆。可用 '{(rewind ? "rewind" : "restore")} list' 查看其他還原點。");
    }

    public (int ExitCode, string Output) HandleBug(SessionState session, string[] args)
    {
        EnsureSessionLoaded(session);

        var outputPath = args.Length >= 1
            ? Path.GetFullPath(args[0])
            : Path.Combine(session.WorkingDirectory, $"nim-cli-bug-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.md");

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? session.WorkingDirectory);

        var builder = new StringBuilder();
        builder.AppendLine("# Nim-Cli Bug Report");
        builder.AppendLine();
        builder.AppendLine($"Session: `{session.SessionId}`");
        builder.AppendLine($"Working Directory: `{session.WorkingDirectory}`");
        builder.AppendLine($"Mode: `{session.Mode}`");
        builder.AppendLine($"Messages: `{session.ConversationHistory.Count}`");
        builder.AppendLine($"Tool Executions: `{session.ToolExecutionHistory.Count}`");
        builder.AppendLine();

        if (!string.IsNullOrWhiteSpace(session.LastDebugRequestJson))
        {
            builder.AppendLine("## Last Debug Request");
            builder.AppendLine();
            builder.AppendLine("```json");
            builder.AppendLine(session.LastDebugRequestJson);
            builder.AppendLine("```");
            builder.AppendLine();
        }

        if (session.ConversationHistory.Count > 0)
        {
            builder.AppendLine("## Recent Conversation");
            builder.AppendLine();
            foreach (var message in session.ConversationHistory.TakeLast(12))
            {
                builder.AppendLine($"### {message.Role}");
                builder.AppendLine();
                builder.AppendLine(message.Content);
                builder.AppendLine();
            }
        }

        if (session.ToolExecutionHistory.Count > 0)
        {
            builder.AppendLine("## Recent Tool Activity");
            builder.AppendLine();
            foreach (var entry in session.ToolExecutionHistory.TakeLast(12))
                builder.AppendLine($"- {entry}");
        }

        File.WriteAllText(outputPath, builder.ToString());
        return (0, $"Wrote bug report to {outputPath}");
    }

    public (int ExitCode, string Output) HandleShells(SessionState session)
    {
        var lines = new List<string>
        {
            $"Shell passthrough executable: {_options.Shell.PowershellExe}",
            "Interactive shell mode is one-shot in this console. Use '!<command>' to run commands.",
            string.IsNullOrWhiteSpace(session.LastShellCommand)
                ? "Last shell command: (none)"
                : $"Last shell command: {session.LastShellCommand}"
        };

        return (0, string.Join(Environment.NewLine, lines));
    }

    public (int ExitCode, string Output) HandleTerminalSetup()
    {
        var lines = new List<string>
        {
            $"Preferred editor: {_workspace.ShowSettings()}",
            $"Shell executable: {_options.Shell.PowershellExe}",
            "Use 'nim-cli settings set editor <command>' to configure an editor.",
            "Use 'nim-cli vim enable' to bind Vim mode to an installed Vim-compatible editor."
        };

        return (0, string.Join(Environment.NewLine, lines));
    }

    public async Task<(int ExitCode, string Output)> HandleTerminalSetupDetailedAsync()
    {
        var shellResult = await _shellProvider.ExecuteAsync("$PSVersionTable.PSVersion.ToString()", Directory.GetCurrentDirectory(), timeoutSeconds: 15);
        var lines = new List<string>
        {
            $"Shell executable: {_options.Shell.PowershellExe}",
            $"PowerShell version: {(shellResult.Success ? shellResult.StandardOutput.Trim() : "Unavailable")}",
            $"Preferred editor: {_workspace.ShowSettings()}",
            "建議：將常用編輯器設定到 settings.editor，並視需要啟用 vim mode。"
        };

        return (0, string.Join(Environment.NewLine, lines));
    }

    public async Task<(int ExitCode, string Output)> HandleSetupGitHubAsync()
    {
        var result = await _shellProvider.ExecuteAsync("gh --version", Directory.GetCurrentDirectory(), timeoutSeconds: 15);
        if (result.Success && !string.IsNullOrWhiteSpace(result.StandardOutput))
            return (0, "GitHub CLI detected." + Environment.NewLine + result.StandardOutput.Trim());

        return (0, "GitHub CLI not detected." + Environment.NewLine + GetGitHubInstallSuggestion());
    }

    private void EnsureSessionLoaded(SessionState session)
    {
        if (session.ConversationHistory.Count != 0 || session.ToolExecutionHistory.Count != 0)
            return;

        var latest = _sessionManager.LoadLatest(session.WorkspaceKey);
        if (latest is not null)
            _sessionManager.RestoreSession(session, latest);
    }

    private static bool TryMapMode(string token, out AgentMode mode)
    {
        switch (token.ToLowerInvariant())
        {
            case "analysis":
            case "analyze":
            case "plan":
                mode = AgentMode.Analysis;
                return true;
            case "coding":
            case "code":
                mode = AgentMode.Coding;
                return true;
            case "ops":
            case "operations":
                mode = AgentMode.Ops;
                return true;
            default:
                mode = AgentMode.Analysis;
                return false;
        }
    }

    private static StoredCheckpoint? ResolveCheckpoint(IReadOnlyList<StoredCheckpoint> checkpoints, string reference)
    {
        if (reference.Equals("latest", StringComparison.OrdinalIgnoreCase))
            return checkpoints[0];

        if (int.TryParse(reference, out var index) && index > 0 && index <= checkpoints.Count)
            return checkpoints[index - 1];

        return checkpoints.FirstOrDefault(checkpoint =>
            checkpoint.Tag.Equals(reference, StringComparison.OrdinalIgnoreCase) ||
            checkpoint.CheckpointId.Equals(reference, StringComparison.OrdinalIgnoreCase));
    }

    private static string FormatCheckpointList(IReadOnlyList<StoredCheckpoint> checkpoints, bool rewind)
    {
        var lines = new List<string>
        {
            rewind ? "可退回的 checkpoint：" : "可還原的 checkpoint：",
            rewind
                ? "使用 'nim-cli rewind <tag|index|latest>' 可把工作狀態退回指定 checkpoint。"
                : "使用 'nim-cli restore <tag|index|latest>' 可把工作狀態還原到指定 checkpoint。"
        };

        for (var index = 0; index < checkpoints.Count; index++)
        {
            var checkpoint = checkpoints[index];
            lines.Add($"{index + 1}. {checkpoint.Tag} | 建立時間={checkpoint.CreatedUtc:yyyy-MM-dd HH:mm:ss} UTC | 訊息={checkpoint.Session.Messages.Count} | 工具={checkpoint.Session.ToolHistory.Count} | Session={checkpoint.Session.SessionId}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string FormatCheckpointDetails(StoredCheckpoint checkpoint, bool rewind)
    {
        var lines = new List<string>
        {
            $"Checkpoint: {checkpoint.Tag}",
            $"Checkpoint ID: {checkpoint.CheckpointId}",
            $"建立時間: {checkpoint.CreatedUtc:yyyy-MM-dd HH:mm:ss} UTC",
            $"Session ID: {checkpoint.Session.SessionId}",
            $"標題: {checkpoint.Session.Title}",
            $"工作目錄: {checkpoint.Session.WorkingDirectory}",
            $"模式: {checkpoint.Session.Mode}",
            $"訊息數: {checkpoint.Session.Messages.Count}",
            $"工具紀錄數: {checkpoint.Session.ToolHistory.Count}",
            $"最近 build: {(string.IsNullOrWhiteSpace(checkpoint.Session.LastBuildSummary) ? "(無)" : checkpoint.Session.LastBuildSummary)}",
            $"最近 test: {(string.IsNullOrWhiteSpace(checkpoint.Session.LastTestSummary) ? "(無)" : checkpoint.Session.LastTestSummary)}",
            $"最近 repo map: {(string.IsNullOrWhiteSpace(checkpoint.Session.LastRepoMap) ? "(無)" : "可用")}",
            rewind
                ? $"執行方式: nim-cli rewind {checkpoint.Tag}"
                : $"執行方式: nim-cli restore {checkpoint.Tag}"
        };

        return string.Join(Environment.NewLine, lines);
    }

    private static string GetGitHubInstallSuggestion()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "Install GitHub CLI with 'winget install --id GitHub.cli -e'.";

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "Install GitHub CLI with 'brew install gh'.";

        return "Install GitHub CLI with your package manager, for example 'sudo apt-get install gh'.";
    }
}
