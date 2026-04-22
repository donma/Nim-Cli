using System.Text;
using NimCli.Core;

namespace NimCli.App;

public sealed class SessionCommandService
{
    private readonly SessionManager _sessionManager;

    public SessionCommandService(SessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }

    public (int ExitCode, string Output) Handle(SessionState session, string[] args)
    {
        var subcommand = args.Length == 0 ? "show" : args[0].ToLowerInvariant();

        return subcommand switch
        {
            "show" => Show(session),
            "clear" => Clear(session),
            "resume" => Resume(session, args.Skip(1).ToArray()),
            _ => (1, "Usage: nim-cli session [show|clear|resume [latest|session-id|index]]")
        };
    }

    private (int ExitCode, string Output) Show(SessionState session)
    {
        EnsureSessionLoaded(session);

        var lines = new StringBuilder();
        lines.AppendLine($"Session ID: {session.SessionId}");
        lines.AppendLine($"Workspace Key: {session.WorkspaceKey}");
        lines.AppendLine($"Working Directory: {session.WorkingDirectory}");
        lines.AppendLine($"Mode: {session.Mode}");
        lines.AppendLine($"Messages: {session.ConversationHistory.Count}");
        lines.AppendLine($"Tool Executions: {session.ToolExecutionHistory.Count}");
        lines.AppendLine($"Workspace Directories: {(session.WorkspaceDirectories.Count == 0 ? session.WorkingDirectory : string.Join(", ", session.WorkspaceDirectories))}");

        if (!string.IsNullOrWhiteSpace(session.LastDebugRequestJson))
            lines.AppendLine("Debug Request: available");

        return (0, lines.ToString().TrimEnd());
    }

    private (int ExitCode, string Output) Clear(SessionState session)
    {
        session.Clear();
        session.SetWorkspaceDirectories([session.WorkingDirectory]);
        _sessionManager.SaveSession(session);
        return (0, "Session context cleared.");
    }

    private (int ExitCode, string Output) Resume(SessionState session, string[] args)
    {
        var reference = args.Length == 0 ? "latest" : args[0];
        var stored = _sessionManager.LoadByReference(session.WorkspaceKey, reference);
        if (stored is null)
            return (1, $"Session not found: {reference}");

        _sessionManager.RestoreSession(session, stored);
        _sessionManager.SaveSession(session);
        return (0, $"Resumed session {stored.SessionId}");
    }

    private void EnsureSessionLoaded(SessionState session)
    {
        if (session.ConversationHistory.Count != 0 || session.ToolExecutionHistory.Count != 0)
            return;

        var stored = _sessionManager.LoadByReference(session.WorkspaceKey, "latest");
        if (stored is not null)
            _sessionManager.RestoreSession(session, stored);
    }
}
