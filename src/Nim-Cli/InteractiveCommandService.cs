using Microsoft.Extensions.DependencyInjection;
using NimCli.Core;
using NimCli.Infrastructure;
using NimCli.Infrastructure.Config;

namespace NimCli.App;

public sealed record InteractiveCommandResult(bool ShouldContinue, string Output, bool ClearScreen = false);

public sealed class InteractiveCommandService
{
    public async Task<InteractiveCommandResult> ExecuteAsync(
        IServiceProvider services,
        string input,
        string approvalMode,
        Func<Task<InteractiveCommandResult>>? onExit = null)
    {
        var session = services.GetRequiredService<SessionState>();
        var sessionManager = services.GetRequiredService<SessionManager>();
        var runtimeStore = services.GetRequiredService<CliRuntimeStore>();
        var workspace = services.GetRequiredService<WorkspaceCommandService>();
        var compatibility = services.GetRequiredService<CompatibilityCommandService>();
        var options = services.GetRequiredService<NimCliOptions>();
        var args = input[1..].Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (args.Length == 0)
            return new InteractiveCommandResult(true, string.Empty);

        switch (args[0].ToLowerInvariant())
        {
            case "help":
            case "?":
                return new InteractiveCommandResult(true, CliApplication.GetInteractiveHelpText());
            case "quit":
            case "exit":
                return onExit is null ? new InteractiveCommandResult(false, string.Empty) : await onExit();
            case "clear":
                session.Clear();
                session.SetWorkspaceDirectories([session.WorkingDirectory]);
                sessionManager.SaveSession(session);
                return new InteractiveCommandResult(true, "Session context cleared.", ClearScreen: true);
            case "about":
                return new InteractiveCommandResult(true,
                    $"Nim-Cli interactive mode{Environment.NewLine}Model: {options.Provider.DefaultModel}{Environment.NewLine}Working directory: {session.WorkingDirectory}{Environment.NewLine}Session: {session.SessionId}");
            case "model":
                if (args.Length == 1)
                    return new InteractiveCommandResult(true, options.Provider.DefaultModel);

                if (args.Length >= 3 && args[1].Equals("set", StringComparison.OrdinalIgnoreCase))
                {
                    options.Provider.DefaultModel = args[2];
                    UserConfigStore.SaveUserConfig(options);
                    return new InteractiveCommandResult(true, $"Model set to {args[2]}");
                }

                return new InteractiveCommandResult(true, "Usage: /model [set <model-id>]");
            case "tools":
                var registry = services.GetRequiredService<ToolRegistry>();
                var showDescriptions = args.Length >= 2 && (args[1].Equals("desc", StringComparison.OrdinalIgnoreCase) || args[1].Equals("descriptions", StringComparison.OrdinalIgnoreCase));
                return new InteractiveCommandResult(true, string.Join(Environment.NewLine,
                    registry.GetToolDefinitions().OrderBy(tool => tool.Name, StringComparer.OrdinalIgnoreCase)
                        .Select(tool => showDescriptions ? $"{tool.Name}: {tool.Description}" : tool.Name)));
            case "models":
                return new InteractiveCommandResult(true, await CaptureCommandOutputAsync(() => CliApplication.RunAsync(["models", "list"], options)));
            case "doctor":
                return new InteractiveCommandResult(true, await services.GetRequiredService<DoctorCommandService>().BuildReportAsync(options));
            case "run":
                if (args.Length == 1)
                    return new InteractiveCommandResult(true, "Usage: /run <prompt>");

                return new InteractiveCommandResult(true, await CaptureCommandOutputAsync(() => CliApplication.RunAsync(["-p", string.Join(" ", args.Skip(1))], options)));
            case "build":
                return new InteractiveCommandResult(true, await CaptureCommandOutputAsync(() => CliApplication.RunAsync(args, options)));
            case "run-project":
                return new InteractiveCommandResult(true, await CaptureCommandOutputAsync(() => CliApplication.RunAsync(args, options)));
            case "screenshot":
                return new InteractiveCommandResult(true, await CaptureCommandOutputAsync(() => CliApplication.RunAsync(args, options)));
            case "analyze":
                return new InteractiveCommandResult(true, await CaptureCommandOutputAsync(() => CliApplication.RunAsync(args, options)));
            case "browser":
                return new InteractiveCommandResult(true, await CaptureCommandOutputAsync(() => CliApplication.RunAsync(args, options)));
            case "db":
                return new InteractiveCommandResult(true, await CaptureCommandOutputAsync(() => CliApplication.RunAsync(args, options)));
            case "git":
                return new InteractiveCommandResult(true, await CaptureCommandOutputAsync(() => CliApplication.RunAsync(args, options)));
            case "ftp":
                return new InteractiveCommandResult(true, await CaptureCommandOutputAsync(() => CliApplication.RunAsync(args, options)));
            case "plan":
                session.Mode = AgentMode.Analysis;
                if (args.Length == 1)
                    return new InteractiveCommandResult(true, "Plan mode enabled for this session. Use '/plan <task>' or 'nim-cli plan \"<task>\"' for a structured plan.");

                return new InteractiveCommandResult(true,
                    services.GetRequiredService<PlanCommandService>().BuildPlan(string.Join(" ", args.Skip(1)), session.WorkingDirectory));
            case "session":
                var sessionResult = services.GetRequiredService<SessionCommandService>().Handle(session, args.Skip(1).ToArray());
                return new InteractiveCommandResult(true, sessionResult.Output);
            case "mcp":
                return new InteractiveCommandResult(true, await CaptureCommandOutputAsync(() => CliApplication.RunAsync(args.Select(static value => value.ToLowerInvariant()).ToArray(), options)));
            case "memory":
                return new InteractiveCommandResult(true, await CaptureCommandOutputAsync(() => CliApplication.RunAsync(args.Select(static value => value.ToLowerInvariant()).ToArray(), options)));
            case "init":
                return new InteractiveCommandResult(true, workspace.InitializeMemoryFile(Directory.GetCurrentDirectory()));
            case "directory":
            case "dir":
                return new InteractiveCommandResult(true, HandleDirectorySlashCommand(session, args));
            case "stats":
                return new InteractiveCommandResult(true, await CaptureCommandOutputAsync(() => CliApplication.RunAsync(args.Select(static value => value.ToLowerInvariant()).ToArray(), options)));
            case "permissions":
                return new InteractiveCommandResult(true, await CaptureCommandOutputAsync(() => CliApplication.RunAsync(args.Select(static value => value.ToLowerInvariant()).ToArray(), options)));
            case "resume":
                return new InteractiveCommandResult(true, await CaptureCommandOutputAsync(() => CliApplication.RunAsync(args.Prepend("resume").ToArray(), options)));
            case "chat":
                return new InteractiveCommandResult(true, "TUI/interactive chat is already active. Type your prompt directly, or use /run <prompt> for one-shot execution.");
            case "copy":
                var lastAssistant = session.ConversationHistory.LastOrDefault(message => message.Role == "assistant")?.Content;
                return string.IsNullOrWhiteSpace(lastAssistant)
                    ? new InteractiveCommandResult(true, "No assistant output available to copy.")
                    : new InteractiveCommandResult(true, await workspace.CopyToClipboardAsync(lastAssistant));
            case "docs":
                return new InteractiveCommandResult(true, await workspace.OpenUrlAsync("https://www.geminicli.com/docs/reference/commands"));
            case "privacy":
                var state = runtimeStore.LoadState();
                state.Consent.PrivacyAccepted = true;
                state.Consent.AcceptedUtc = DateTimeOffset.UtcNow;
                runtimeStore.SaveState(state);
                return new InteractiveCommandResult(true, "Privacy notice acknowledged for this execution directory.");
            case "settings":
                return new InteractiveCommandResult(true, await CaptureCommandOutputAsync(() => CliApplication.RunAsync(args.Select(static value => value.ToLowerInvariant()).ToArray(), options)));
            case "theme":
                return new InteractiveCommandResult(true, await CaptureCommandOutputAsync(() => CliApplication.RunAsync(args.Length >= 2 ? ["settings", "set", "theme", args[1]] : ["settings", "show"], options)));
            case "commands":
                return new InteractiveCommandResult(true, compatibility.HandleCommands().Output);
            case "workspace":
                return new InteractiveCommandResult(true, await CaptureCommandOutputAsync(() => CliApplication.RunAsync(args, options)));
            case "compatibility":
                return new InteractiveCommandResult(true, compatibility.HandleCompatibility().Output);
            case "vim":
                var vimResult = await services.GetRequiredService<VimCommandService>().HandleAsync(args.Skip(1).ToArray());
                return new InteractiveCommandResult(true, vimResult.Output);
            case "editor":
                if (args.Length >= 2)
                {
                    var editorState = runtimeStore.LoadState();
                    editorState.Settings.PreferredEditor = args[1];
                    runtimeStore.SaveState(editorState);
                    return new InteractiveCommandResult(true, $"Preferred editor set to {args[1]}");
                }

                return new InteractiveCommandResult(true, $"Preferred editor: {runtimeStore.LoadState().Settings.PreferredEditor ?? "(not set)"}");
            case "auth":
                if (args.Length >= 2 && args[1].Equals("status", StringComparison.OrdinalIgnoreCase))
                    return new InteractiveCommandResult(true, await CaptureCommandOutputAsync(() => CliApplication.RunAsync(["auth", "status"], options)));

                return new InteractiveCommandResult(true, "Use root command 'nim-cli auth login' for interactive key entry.");
            case "agents":
                return new InteractiveCommandResult(true, compatibility.HandleAgents(session, args.Skip(1).ToArray()).Output);
            case "compress":
                return new InteractiveCommandResult(true, compatibility.HandleCompress(session, args.Skip(1).ToArray()).Output);
            case "policies":
                return new InteractiveCommandResult(true, compatibility.HandlePolicies(session, approvalMode, session.WorkspaceDirectories).Output);
            case "restore":
                return new InteractiveCommandResult(true, compatibility.HandleRestore(session, args.Skip(1).ToArray(), rewind: false).Output);
            case "rewind":
                return new InteractiveCommandResult(true, compatibility.HandleRestore(session, args.Skip(1).ToArray(), rewind: true).Output);
            case "setup-github":
                return new InteractiveCommandResult(true, (await compatibility.HandleSetupGitHubAsync()).Output);
            case "bug":
                return new InteractiveCommandResult(true, compatibility.HandleBug(session, args.Skip(1).ToArray()).Output);
            case "hooks":
            case "extensions":
            case "skills":
                return new InteractiveCommandResult(true, await CaptureCommandOutputAsync(() => CliApplication.RunAsync(args.Select(static value => value.ToLowerInvariant()).ToArray(), options)));
            case "shells":
            case "bashes":
                return new InteractiveCommandResult(true, compatibility.HandleShells(session).Output);
            case "upgrade":
                return new InteractiveCommandResult(true, await workspace.OpenUrlAsync("https://www.geminicli.com/plans/"));
            case "ide":
            case "terminal-setup":
                return new InteractiveCommandResult(true, (await compatibility.HandleTerminalSetupDetailedAsync()).Output);
            case "update":
                return new InteractiveCommandResult(true, services.GetRequiredService<UpdateCommandService>().GetUpdateInfo());
            default:
                return new InteractiveCommandResult(true, $"Unknown slash command: {args[0]}");
        }
    }

    private static string HandleDirectorySlashCommand(SessionState session, string[] args)
    {
        if (args.Length < 2 || args[1].Equals("show", StringComparison.OrdinalIgnoreCase))
            return string.Join(Environment.NewLine, session.WorkspaceDirectories);

        if (args[1].Equals("add", StringComparison.OrdinalIgnoreCase) && args.Length >= 3)
        {
            var updated = session.WorkspaceDirectories.Concat(args[2].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            session.SetWorkspaceDirectories(updated);
            return "Workspace directories updated.";
        }

        return "Usage: /directory [show|add <dir1,dir2>]";
    }

    private static async Task<string> CaptureCommandOutputAsync(Func<Task<int>> action)
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;
        using var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);

        try
        {
            var exitCode = await action();
            var output = writer.ToString().Trim();
            return string.IsNullOrWhiteSpace(output) ? $"Command completed with exit code {exitCode}." : output;
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }
}
