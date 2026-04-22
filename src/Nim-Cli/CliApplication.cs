using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using NimCli.Contracts;
using NimCli.Core;
using NimCli.Infrastructure;
using NimCli.Infrastructure.Config;

namespace NimCli.App;

public static class CliApplication
{
    public static async Task<int> RunAsync(string[] args, NimCliOptions options)
    {
        try
        {
            var effectiveOptions = CloneOptions(options);
            var parsed = ParseCliArguments(args, effectiveOptions);

            if (!string.IsNullOrWhiteSpace(parsed.ModelOverride))
                effectiveOptions.Provider.DefaultModel = parsed.ModelOverride;

            if (parsed.ShowVersion)
            {
                PrintVersion();
                return 0;
            }

            if (parsed.ShowHelp)
            {
                PrintHelp();
                return 0;
            }

            if (parsed.RemainingArgs.Length > 0 && IsUnknownCommandLikeToken(parsed.RemainingArgs[0]))
            {
                Console.WriteLine($"Unknown command: {parsed.RemainingArgs[0]}");
                PrintHelp();
                return 1;
            }

            var services = await ServiceConfiguration.BuildServicesAsync(effectiveOptions);
            try
            {
                var sessionManager = services.GetRequiredService<SessionManager>();
                InitializeSession(services, parsed, sessionManager);

                if (parsed.ListSessions)
                    return HandleListSessions(services, sessionManager);

                if (!string.IsNullOrWhiteSpace(parsed.DeleteSessionId))
                    return HandleDeleteSession(services, sessionManager, parsed.DeleteSessionId);

                if (!string.IsNullOrWhiteSpace(parsed.ResumeTarget))
                {
                    var loaded = TryResumeSession(services, sessionManager, parsed.ResumeTarget);
                    if (!loaded)
                        return 1;
                }

                if (parsed.ListExtensions)
                    return HandleExtensionsCommand(services, Array.Empty<string>());

                if (!string.IsNullOrWhiteSpace(parsed.HeadlessPrompt))
                {
                    EnsureAuthenticated();
                    var prompt = await BuildPromptWithRedirectedInputAsync(parsed.HeadlessPrompt);
                    return await RunHeadlessAsync(services, prompt, parsed.OutputFormat, parsed.ApprovalMode);
                }

                if (!string.IsNullOrWhiteSpace(parsed.InteractivePrompt))
                {
                    EnsureAuthenticated();
                    var prompt = await BuildPromptWithRedirectedInputAsync(parsed.InteractivePrompt);
                    await Commands.ChatCommands.RunChatAsync(services, prompt, continueInteractive: true, parsed.ApprovalMode);
                    return 0;
                }

                if (parsed.RemainingArgs.Length == 0)
                {
                    if (Console.IsInputRedirected)
                    {
                        EnsureAuthenticated();
                        var pipedPrompt = await BuildPromptWithRedirectedInputAsync(null);
                        return await RunHeadlessAsync(services, pipedPrompt, parsed.OutputFormat, parsed.ApprovalMode);
                    }

                    EnsureAuthenticated();
                    await Commands.ChatCommands.RunChatAsync(services, approvalMode: parsed.ApprovalMode);
                    return 0;
                }

                if (LooksLikePositionalPrompt(parsed.RemainingArgs[0]))
                {
                    EnsureAuthenticated();
                    await Commands.ChatCommands.RunChatAsync(services, string.Join(" ", parsed.RemainingArgs), continueInteractive: true, parsed.ApprovalMode);
                    return 0;
                }

                switch (parsed.RemainingArgs[0].ToLowerInvariant())
                {
                case "auth":
                    return await HandleAuthAsync(parsed.RemainingArgs);

                case "models":
                    await HandleModelsAsync(parsed.RemainingArgs);
                    return 0;

                case "model":
                    return await HandleModelCommandAsync(parsed.RemainingArgs, effectiveOptions);

                case "set":
                    return await HandleSetCommandAsync(parsed.RemainingArgs, effectiveOptions);

                case "get":
                    return HandleGetCommand(parsed.RemainingArgs, effectiveOptions);

                case "chat":
                    EnsureAuthenticated();
                    await Commands.ChatCommands.RunChatAsync(services, approvalMode: parsed.ApprovalMode);
                    return 0;

                case "run":
                    EnsureAuthenticated();
                    var runPrompt = parsed.RemainingArgs.Length > 1 ? string.Join(" ", parsed.RemainingArgs.Skip(1)) : null;
                    if (string.IsNullOrWhiteSpace(runPrompt))
                    {
                        Console.WriteLine("Usage: nim-cli run \"<prompt>\"");
                        return 1;
                    }

                    await Commands.ChatCommands.RunChatAsync(services, runPrompt, approvalMode: parsed.ApprovalMode);
                    return 0;

                case "config":
                    HandleConfig(parsed.RemainingArgs, effectiveOptions);
                    return 0;

                case "doctor":
                    await services.GetRequiredService<DoctorCommandService>().RunAsync(effectiveOptions);
                    return 0;

                case "build":
                    return await Commands.ToolCommands.BuildAsync(services, parsed.RemainingArgs);

                case "run-project":
                    return await Commands.ToolCommands.RunProjectAsync(services, parsed.RemainingArgs);

                case "screenshot":
                    return await Commands.ToolCommands.ScreenshotAsync(services, parsed.RemainingArgs);

                case "browser":
                    return await HandleBrowserAsync(services, parsed.RemainingArgs);

                case "db":
                    return await HandleDbAsync(services, parsed.RemainingArgs);

                case "ftp":
                    return await HandleFtpAsync(services, parsed.RemainingArgs);

                case "git":
                    return await HandleGitAsync(services, parsed.RemainingArgs);

                case "repo":
                    return await HandleRepoAsync(services, parsed.RemainingArgs);

                case "analyze":
                    return await Commands.ToolCommands.AnalyzeAsync(services, parsed.RemainingArgs);

                case "code":
                    return await HandleCodeAsync(services, parsed.RemainingArgs);

                case "mcp":
                    return await HandleMcpAsync(services, parsed.RemainingArgs);

                case "tools":
                    return HandleToolsCommand(services, parsed.RemainingArgs);

                case "permissions":
                    return HandlePermissionsCommand(services, parsed.ApprovalMode, parsed.IncludeDirectories, parsed.RemainingArgs);

                case "stats":
                    return HandleStatsCommand(services, parsed.RemainingArgs);

                case "memory":
                    return HandleMemoryCommand(services, parsed.RemainingArgs);

                case "settings":
                    return HandleSettingsCommand(services, parsed.RemainingArgs);

                case "hooks":
                    return HandleHooksCommand(services, parsed.RemainingArgs);

                case "agents":
                    return HandleAgentsCommand(services, parsed.RemainingArgs.Skip(1).ToArray());

                case "commands":
                    return HandleCommandsCommand(services);

                case "workspace":
                    return HandleWorkspaceCommand(services, parsed.RemainingArgs.Skip(1).ToArray());

                case "compatibility":
                    return HandleCompatibilityCommand(services);

                case "compress":
                    return HandleCompressCommand(services, parsed.RemainingArgs.Skip(1).ToArray());

                case "policies":
                    return HandlePoliciesCompatibilityCommand(services, parsed.ApprovalMode, parsed.IncludeDirectories);

                case "restore":
                    return HandleRestoreCompatibilityCommand(services, parsed.RemainingArgs.Skip(1).ToArray(), rewind: false);

                case "rewind":
                    return HandleRestoreCompatibilityCommand(services, parsed.RemainingArgs.Skip(1).ToArray(), rewind: true);

                case "bug":
                    return HandleBugCommand(services, parsed.RemainingArgs.Skip(1).ToArray());

                case "shells":
                case "bashes":
                    return HandleShellsCommand(services);

                case "ide":
                case "terminal-setup":
                    return await HandleTerminalSetupCommand(services);

                case "setup-github":
                    return await HandleSetupGitHubCommandAsync(services);

                case "vim":
                    return await HandleVimCommandAsync(services, parsed.RemainingArgs.Skip(1).ToArray());

                case "init":
                    return HandleInitCommand(services);

                case "plan":
                    return HandlePlanCommand(services, parsed.RemainingArgs.Skip(1).ToArray());

                case "update":
                    return HandleUpdateCommand(services);

                case "extensions":
                    return HandleExtensionsCommand(services, parsed.RemainingArgs.Skip(1).ToArray());

                case "skills":
                    return HandleSkillsCommand(services, parsed.RemainingArgs.Skip(1).ToArray());

                case "resume":
                    return HandleResumeCommand(services, sessionManager, parsed.RemainingArgs);

                case "session":
                    return HandleSessionCommand(services, parsed.RemainingArgs.Skip(1).ToArray());

                case "playwright":
                    return await HandlePlaywrightAsync(parsed.RemainingArgs);

                case "--help":
                case "-h":
                case "help":
                    PrintHelp();
                    return 0;

                default:
                    Console.WriteLine($"Unknown command: {parsed.RemainingArgs[0]}");
                    PrintHelp();
                    return 1;
                }
            }
            finally
            {
                switch (services)
                {
                    case IAsyncDisposable asyncDisposable:
                        await asyncDisposable.DisposeAsync();
                        break;
                    case IDisposable disposable:
                        disposable.Dispose();
                        break;
                }
            }
        }
        catch (AuthenticationRequiredException)
        {
            return 1;
        }
    }

    public static string GetInteractiveHelpText()
    {
        return """
            Interactive slash commands:
              /help, /?                     Show slash command help
              /clear                        Clear screen and reset session context
              /quit, /exit                  Exit interactive chat
              /about                        Show Nim-Cli version and model info
              /model                        Show current model
              /model set <model-id>         Persist a new default model
              /models                       List available models
              /tools [desc]                 List registered tools
              /doctor                       Run doctor checks
              /run <prompt>                 Run a one-shot prompt via shared headless path
              /build [--project <path>]     Run dotnet build via shared tool path
              /run-project [--project <path>] [--args <args>]
                                            Run dotnet run via shared tool path
              /screenshot --url <url>       Capture a browser screenshot
              /analyze [--directory <dir>]  Analyze project state and repo context
              /browser ...                  Use shared browser commands
              /db ...                       Use shared read-only database query commands
              /git ...                      Use shared git commands
              /ftp ...                      Use shared ftp commands
              /mcp [status|tools|list|ls]   Inspect MCP client state
              /memory [list|show|refresh]   Inspect Nim.md files in workspace
              /init                         Create Nim.md in the current workspace
              /session [show|clear|resume]  Manage current session state
              /directory show               Show current workspace directories
              /directory add <a,b>          Add workspace directories for current session
              /stats [session|tools|model]  Show simple session stats
              /permissions                  Show approval mode and included directories
              /settings                     Show runtime settings
              /workspace [show|switch <dir>] Inspect or switch current workspace
              /compatibility                Show compatibility mapping summary
              /hooks [list|enable|disable]  Manage persisted hooks registry
              /update                       Show version and update guidance
              /vim [status|install|enable|disable]
                                            Manage Neovim-backed Vim mode integration
              /plan [task]                  Switch session mode or build a structured plan
              !<command>                    Execute a shell command immediately
              @path                         Inject file or directory content into the prompt

            Compatibility-recognized slash commands:
              /auth, /chat, /resume, /agents, /commands, /extensions, /hooks, /ide,
              /policies, /privacy, /restore, /rewind, /shells, /workspace, /compatibility,
              /skills, /theme, /upgrade, /vim
            """;
    }

    private static async Task<int> HandleModelCommandAsync(string[] args, NimCliOptions options)
    {
        if (args.Length < 2 || args[1].Equals("current", StringComparison.OrdinalIgnoreCase) || args[1].Equals("show", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine(options.Provider.DefaultModel);
            return 0;
        }

        switch (args[1].ToLowerInvariant())
        {
            case "set":
                if (args.Length < 3)
                {
                    Console.WriteLine("Usage: nim-cli model set <model-id>");
                    return 1;
                }

                options.Provider.DefaultModel = args[2];
                UserConfigStore.SaveUserConfig(options);
                Console.WriteLine($"Model set to {args[2]}");
                return 0;

            case "list":
                await Commands.ModelsCommands.ListAsync();
                return 0;

            default:
                Console.WriteLine("Usage: nim-cli model [current|show|set <model-id>|list]");
                return 1;
        }
    }

    private static async Task<int> HandleSetCommandAsync(string[] args, NimCliOptions options)
    {
        if (args.Length >= 3 && args[1].Equals("model", StringComparison.OrdinalIgnoreCase))
        {
            options.Provider.DefaultModel = args[2];
            UserConfigStore.SaveUserConfig(options);
            Console.WriteLine($"Model set to {args[2]}");
            return 0;
        }

        if (args.Length >= 3 && args[1].Equals("baseurl", StringComparison.OrdinalIgnoreCase))
        {
            options.Provider.BaseUrl = args[2];
            UserConfigStore.SaveUserConfig(options);
            Console.WriteLine($"BaseUrl set to {args[2]}");
            return 0;
        }

        Console.WriteLine("Usage: nim-cli set [model|baseurl] <value>");
        return 1;
    }

    private static int HandleGetCommand(string[] args, NimCliOptions options)
    {
        if (args.Length >= 2 && args[1].Equals("model", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine(options.Provider.DefaultModel);
            return 0;
        }

        if (args.Length >= 2 && args[1].Equals("baseurl", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine(options.Provider.BaseUrl);
            return 0;
        }

        Console.WriteLine("Usage: nim-cli get [model|baseurl]");
        return 1;
    }

    private static async Task<int> HandleAuthAsync(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: nim-cli auth [login|status]");
            return 1;
        }

        switch (args[1].ToLowerInvariant())
        {
            case "login":
                return await Commands.AuthCommands.LoginAsync(GetOption(args, "--api-key"));
            case "status":
                return await Commands.AuthCommands.StatusAsync();
            default:
                Console.WriteLine($"Unknown auth command: {args[1]}");
                return 1;
        }
    }

    private static async Task HandleModelsAsync(string[] args)
    {
        if (args.Length < 2 || args[1].ToLowerInvariant() == "list")
            await Commands.ModelsCommands.ListAsync();
    }

    private static async Task<int> HandlePlaywrightAsync(string[] args)
    {
        if (args.Length >= 3 && args[1].ToLowerInvariant() == "install" && args[2].ToLowerInvariant() == "chromium")
            return await Commands.PlaywrightCommands.InstallChromiumAsync();

        Console.WriteLine("Usage: nim-cli playwright install chromium");
        return 1;
    }

    private static async Task<int> HandleDbAsync(IServiceProvider services, string[] args)
    {
        if (args.Length >= 2 && args[1].ToLowerInvariant() == "query")
            return await Commands.ToolCommands.DbQueryAsync(services, args);

        Console.WriteLine("Usage: nim-cli db query --connection <conn> --query <sql>");
        return 1;
    }

    private static async Task<int> HandleBrowserAsync(IServiceProvider services, string[] args)
    {
        if (args.Length >= 2)
        {
            switch (args[1].ToLowerInvariant())
            {
                case "open":
                    return await Commands.ToolCommands.BrowserOpenAsync(services, args);
                case "navigate":
                    return await Commands.ToolCommands.BrowserNavigateAsync(services, args);
                case "wait":
                    return await Commands.ToolCommands.BrowserWaitAsync(services, args);
                case "close":
                    return await Commands.ToolCommands.BrowserCloseAsync(services);
            }
        }

        Console.WriteLine("Usage: nim-cli browser [open|navigate|wait|close]");
        return 1;
    }

    private static async Task<int> HandleFtpAsync(IServiceProvider services, string[] args)
    {
        if (args.Length >= 2 && args[1].ToLowerInvariant() == "upload")
            return await Commands.ToolCommands.FtpUploadAsync(services, args);

        Console.WriteLine("Usage: nim-cli ftp upload --host <host> --user <user> --password <pwd> --local <file> --remote <path>");
        return 1;
    }

    private static async Task<int> HandleGitAsync(IServiceProvider services, string[] args)
    {
        if (args.Length >= 2)
        {
            switch (args[1].ToLowerInvariant())
            {
                case "status":
                    return await Commands.ToolCommands.GitStatusAsync(services, args);
                case "diff":
                    return await Commands.ToolCommands.GitDiffAsync(services, args);
                case "commit":
                    return await Commands.ToolCommands.GitCommitAsync(services, args);
                case "push":
                    return await Commands.ToolCommands.GitPushAsync(services, args);
            }
        }

        Console.WriteLine("Usage: nim-cli git [status|diff|commit|push]");
        return 1;
    }

    private static async Task<int> HandleCodeAsync(IServiceProvider services, string[] args)
    {
        if (args.Length >= 2)
        {
            switch (args[1].ToLowerInvariant())
            {
                case "plan":
                    return await Commands.ToolCommands.PlanEditAsync(services, args);
                case "apply":
                    return await Commands.ToolCommands.ApplyPatchAsync(services, args);
            }
        }

        Console.WriteLine("Usage: nim-cli code [plan|apply]");
        return 1;
    }

    private static async Task<int> HandleMcpAsync(IServiceProvider services, string[] args)
    {
        var mcpCommands = services.GetRequiredService<McpCommandService>();

        if (args.Length < 2)
        {
            Console.WriteLine(mcpCommands.ListServers(includeDescriptions: true));
            return 0;
        }

        switch (args[1].ToLowerInvariant())
        {
            case "status":
                return await Commands.ToolCommands.McpStatusAsync(services);
            case "tools":
            case "list":
            case "ls":
                Console.WriteLine(args[1].Equals("tools", StringComparison.OrdinalIgnoreCase)
                    ? await FormatMcpToolsAsync(services)
                    : mcpCommands.ListServers(includeDescriptions: true));
                return 0;
            case "reload":
                Console.WriteLine(mcpCommands.Reload());
                return 0;
            case "inspect":
                Console.WriteLine(await mcpCommands.InspectAsync(args.Length >= 3 ? args[2] : null));
                return 0;
            case "ping":
            case "test":
                Console.WriteLine(await mcpCommands.PingAsync());
                return 0;
            case "add":
                Console.WriteLine(mcpCommands.AddServer(args));
                return 0;
            case "remove":
                if (args.Length < 3)
                {
                    Console.WriteLine("Usage: nim-cli mcp remove <name>");
                    return 1;
                }

                Console.WriteLine(mcpCommands.RemoveServer(args[2]));
                return 0;
            case "auth":
                Console.WriteLine(mcpCommands.Auth(args.Length >= 3 ? args[2] : null));
                return 0;
            case "enable":
                if (args.Length < 3)
                {
                    Console.WriteLine("Usage: nim-cli mcp enable <name>");
                    return 1;
                }

                Console.WriteLine(mcpCommands.SetEnabled(args[2], enabled: true));
                return 0;
            case "disable":
                if (args.Length < 3)
                {
                    Console.WriteLine("Usage: nim-cli mcp disable <name>");
                    return 1;
                }

                Console.WriteLine(mcpCommands.SetEnabled(args[2], enabled: false));
                return 0;
            case "schema":
                Console.WriteLine(mcpCommands.ListServers(includeDescriptions: true, includeSchema: true));
                return 0;
            case "desc":
                Console.WriteLine(mcpCommands.ListServers(includeDescriptions: true));
                return 0;
            default:
                Console.WriteLine("Usage: nim-cli mcp [status|tools|list|ls|reload|inspect|ping|test|add|remove|auth|enable|disable|schema|desc]");
                return 1;
        }
    }

    private static async Task<string> FormatMcpToolsAsync(IServiceProvider services)
    {
        var client = services.GetRequiredService<NimCli.Mcp.IMcpClient>();
        var tools = await client.ListToolsAsync();
        if (tools.Count == 0)
            return "No MCP tools available.";

        return string.Join(Environment.NewLine, tools.Select(tool => $"{tool.Name}: {tool.Description}"));
    }

    private static async Task<int> HandleRepoAsync(IServiceProvider services, string[] args)
    {
        if (args.Length >= 2 && args[1].ToLowerInvariant() == "map")
            return await Commands.ToolCommands.RepoMapAsync(services, args);

        Console.WriteLine("Usage: nim-cli repo map [--directory <dir>]");
        return 1;
    }

    private static int HandleToolsCommand(IServiceProvider services, string[] args)
    {
        var registry = services.GetRequiredService<ToolRegistry>();
        var descriptions = args.Length >= 2 && args[1].Equals("desc", StringComparison.OrdinalIgnoreCase);
        var tools = registry.GetToolDefinitions().OrderBy(tool => tool.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var tool in tools)
            Console.WriteLine(descriptions ? $"{tool.Name}: {tool.Description}" : tool.Name);

        return 0;
    }

    private static int HandlePermissionsCommand(IServiceProvider services, string approvalMode, IReadOnlyList<string> includeDirectories, string[] args)
    {
        var workspace = services.GetRequiredService<WorkspaceCommandService>();

        if (args.Length >= 2 && args[1].Equals("trust", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine(workspace.TrustFolder(args.Length >= 3 ? args[2] : null));
            return 0;
        }

        Console.WriteLine($"Approval mode: {approvalMode}");
        Console.WriteLine($"Included directories: {(includeDirectories.Count == 0 ? "(none)" : string.Join(", ", includeDirectories))}");
        Console.WriteLine($"Trusted folders: {workspace.ShowTrustedFolders()}");
        return 0;
    }

    private static int HandleListSessions(IServiceProvider services, SessionManager sessionManager)
    {
        var session = services.GetRequiredService<SessionState>();
        var sessions = sessionManager.ListSessions(session.WorkspaceKey);
        if (sessions.Count == 0)
        {
            Console.WriteLine("No saved sessions for the current workspace.");
            return 0;
        }

        for (var index = 0; index < sessions.Count; index++)
        {
            var item = sessions[index];
            Console.WriteLine($"{index + 1}. {item.SessionId} | {item.LastUpdatedUtc:yyyy-MM-dd HH:mm:ss}Z | {item.MessageCount} msgs | {item.Title}");
        }

        return 0;
    }

    private static int HandleDeleteSession(IServiceProvider services, SessionManager sessionManager, string deleteSessionId)
    {
        var session = services.GetRequiredService<SessionState>();
        var stored = sessionManager.LoadByReference(session.WorkspaceKey, deleteSessionId);
        if (stored is null)
        {
            Console.WriteLine($"Session not found: {deleteSessionId}");
            return 1;
        }

        var deleted = sessionManager.DeleteSession(stored.SessionId);
        Console.WriteLine(deleted ? $"Deleted session {stored.SessionId}" : $"Failed to delete session {stored.SessionId}");
        return deleted ? 0 : 1;
    }

    private static bool TryResumeSession(IServiceProvider services, SessionManager sessionManager, string reference)
    {
        var session = services.GetRequiredService<SessionState>();
        var stored = sessionManager.LoadByReference(session.WorkspaceKey, reference);
        if (stored is null)
        {
            Console.WriteLine($"Session not found: {reference}");
            return false;
        }

        sessionManager.RestoreSession(session, stored);
        Console.WriteLine($"Resumed session {stored.SessionId}");
        return true;
    }

    private static int HandleResumeCommand(IServiceProvider services, SessionManager sessionManager, string[] args)
    {
        if (args.Length == 1)
        {
            return HandleListSessions(services, sessionManager);
        }

        var subcommand = args[1].ToLowerInvariant();
        var session = services.GetRequiredService<SessionState>();

        switch (subcommand)
        {
            case "list":
                return HandleListSessions(services, sessionManager);
            case "resume":
            case "load":
                if (args.Length < 3)
                {
                    Console.WriteLine("Usage: nim-cli resume resume <tag|session-id|latest>");
                    return 1;
                }

                return TryResumeSession(services, sessionManager, args[2]) ? 0 : 1;
            case "save":
                if (args.Length < 3)
                {
                    Console.WriteLine("Usage: nim-cli resume save <tag>");
                    return 1;
                }

                sessionManager.SaveCheckpoint(session, args[2]);
                Console.WriteLine($"Saved checkpoint '{args[2]}'");
                return 0;
            case "delete":
                if (args.Length < 3)
                {
                    Console.WriteLine("Usage: nim-cli resume delete <tag>");
                    return 1;
                }

                var checkpoint = sessionManager.LoadCheckpoint(session.WorkspaceKey, args[2]);
                if (checkpoint is null)
                {
                    Console.WriteLine($"Checkpoint not found: {args[2]}");
                    return 1;
                }

                Console.WriteLine(sessionManager.DeleteCheckpoint(checkpoint.CheckpointId)
                    ? $"Deleted checkpoint '{args[2]}'"
                    : $"Failed to delete checkpoint '{args[2]}'");
                return 0;
            case "share":
                var path = sessionManager.ExportSession(session, args.Length >= 3 ? args[2] : null);
                Console.WriteLine($"Exported session to {path}");
                return 0;
            case "debug":
                Console.WriteLine(string.IsNullOrWhiteSpace(session.LastDebugRequestJson)
                    ? "No debug request available yet."
                    : session.LastDebugRequestJson);
                return 0;
            default:
                var checkpointToResume = sessionManager.LoadCheckpoint(session.WorkspaceKey, args[1]);
                if (checkpointToResume is not null)
                {
                    sessionManager.RestoreSession(session, checkpointToResume.Session);
                    Console.WriteLine($"Resumed checkpoint '{checkpointToResume.Tag}'");
                    return 0;
                }

                return TryResumeSession(services, sessionManager, args[1]) ? 0 : 1;
        }
    }

    private static int HandleStatsCommand(IServiceProvider services, string[] args)
    {
        var session = services.GetRequiredService<SessionState>();
        var mode = args.Length >= 2 ? args[1].ToLowerInvariant() : "session";

        switch (mode)
        {
            case "session":
                Console.WriteLine($"Working directory: {session.WorkingDirectory}");
                Console.WriteLine($"Mode: {session.Mode}");
                Console.WriteLine($"Messages: {session.ConversationHistory.Count}");
                Console.WriteLine($"Tool executions: {session.ToolExecutionHistory.Count}");
                return 0;
            case "tools":
                if (session.ToolExecutionHistory.Count == 0)
                    Console.WriteLine("No tool executions recorded in this session.");
                else
                    foreach (var entry in session.ToolExecutionHistory)
                        Console.WriteLine(entry);
                return 0;
            case "model":
                Console.WriteLine($"Model: {services.GetRequiredService<NimCliOptions>().Provider.DefaultModel}");
                return 0;
            default:
                Console.WriteLine("Usage: nim-cli stats [session|tools|model]");
                return 1;
        }
    }

    private static int HandleMemoryCommand(IServiceProvider services, string[] args)
    {
        var workspace = services.GetRequiredService<WorkspaceCommandService>();
        var command = args.Length >= 2 ? args[1].ToLowerInvariant() : "list";
        var currentDirectory = Directory.GetCurrentDirectory();
        var memoryFiles = workspace.FindMemoryFiles(currentDirectory);

        switch (command)
        {
            case "list":
            case "refresh":
                if (memoryFiles.Count == 0)
                    Console.WriteLine("No Nim.md files found in the current workspace.");
                else
                    foreach (var file in memoryFiles)
                        Console.WriteLine(file);
                return 0;
            case "show":
                if (memoryFiles.Count == 0)
                {
                    Console.WriteLine("No Nim.md files found in the current workspace.");
                    return 0;
                }

                foreach (var file in memoryFiles)
                {
                    Console.WriteLine($"# {file}");
                    Console.WriteLine(File.ReadAllText(file));
                    Console.WriteLine();
                }
                return 0;
            default:
                Console.WriteLine("Usage: nim-cli memory [list|show|refresh]");
                return 1;
        }
    }

    private static int HandleSettingsCommand(IServiceProvider services, string[] args)
    {
        var workspace = services.GetRequiredService<WorkspaceCommandService>();
        if (args.Length < 2 || args[1].Equals("show", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine(workspace.ShowSettings());
            return 0;
        }

        if (args.Length >= 4 && args[1].Equals("set", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine(workspace.SetSetting(args[2], args[3]));
            return 0;
        }

        Console.WriteLine("Usage: nim-cli settings [show|set <key> <value>]");
        return 1;
    }

    private static int HandleHooksCommand(IServiceProvider services, string[] args)
    {
        var registry = services.GetRequiredService<RegistryCommandService>();
        var subcommand = args.Length < 2 ? "list" : args[1].ToLowerInvariant();
        switch (subcommand)
        {
            case "list":
            case "show":
            case "panel":
                Console.WriteLine(registry.List(RegistryKind.Hooks));
                return 0;
            case "enable-all":
                Console.WriteLine(registry.SetEnabled(RegistryKind.Hooks, null, true, all: true));
                return 0;
            case "disable-all":
                Console.WriteLine(registry.SetEnabled(RegistryKind.Hooks, null, false, all: true));
                return 0;
            case "enable":
                Console.WriteLine(registry.SetEnabled(RegistryKind.Hooks, args.Length >= 3 ? args[2] : null, true));
                return 0;
            case "disable":
                Console.WriteLine(registry.SetEnabled(RegistryKind.Hooks, args.Length >= 3 ? args[2] : null, false));
                return 0;
            case "describe":
                Console.WriteLine(registry.Describe(RegistryKind.Hooks, args.Length >= 3 ? args[2] : null));
                return 0;
            case "rename":
                if (args.Length < 4)
                {
                    Console.WriteLine("Usage: nim-cli hooks rename <old-name> <new-name>");
                    return 1;
                }

                Console.WriteLine(registry.Rename(RegistryKind.Hooks, args[2], args[3]));
                return 0;
            case "set-description":
                if (args.Length < 4)
                {
                    Console.WriteLine("Usage: nim-cli hooks set-description <name> <description>");
                    return 1;
                }

                Console.WriteLine(registry.SetDescription(RegistryKind.Hooks, args[2], string.Join(" ", args.Skip(3))));
                return 0;
            default:
                Console.WriteLine("Usage: nim-cli hooks [list|show|panel|describe [name]|enable <name>|disable <name>|enable-all|disable-all|rename <old> <new>|set-description <name> <text>]");
                return 1;
        }
    }

    private static async Task<int> HandleVimCommandAsync(IServiceProvider services, string[] args)
    {
        var vim = services.GetRequiredService<VimCommandService>();
        var result = await vim.HandleAsync(args);
        Console.WriteLine(result.Output);
        return result.ExitCode;
    }

    private static int HandleAgentsCommand(IServiceProvider services, string[] args)
    {
        var compatibility = services.GetRequiredService<CompatibilityCommandService>();
        var session = services.GetRequiredService<SessionState>();
        var result = compatibility.HandleAgents(session, args);
        Console.WriteLine(result.Output);
        return result.ExitCode;
    }

    private static int HandleCommandsCommand(IServiceProvider services)
    {
        var compatibility = services.GetRequiredService<CompatibilityCommandService>();
        var result = compatibility.HandleCommands();
        Console.WriteLine(result.Output);
        return result.ExitCode;
    }

    private static int HandleCompressCommand(IServiceProvider services, string[] args)
    {
        var compatibility = services.GetRequiredService<CompatibilityCommandService>();
        var session = services.GetRequiredService<SessionState>();
        var result = compatibility.HandleCompress(session, args);
        Console.WriteLine(result.Output);
        return result.ExitCode;
    }

    private static int HandlePoliciesCompatibilityCommand(IServiceProvider services, string approvalMode, IReadOnlyList<string> includeDirectories)
    {
        var compatibility = services.GetRequiredService<CompatibilityCommandService>();
        var session = services.GetRequiredService<SessionState>();
        var result = compatibility.HandlePolicies(session, approvalMode, includeDirectories);
        Console.WriteLine(result.Output);
        return result.ExitCode;
    }

    private static int HandleRestoreCompatibilityCommand(IServiceProvider services, string[] args, bool rewind)
    {
        var compatibility = services.GetRequiredService<CompatibilityCommandService>();
        var session = services.GetRequiredService<SessionState>();
        var result = compatibility.HandleRestore(session, args, rewind);
        Console.WriteLine(result.Output);
        return result.ExitCode;
    }

    private static int HandleBugCommand(IServiceProvider services, string[] args)
    {
        var compatibility = services.GetRequiredService<CompatibilityCommandService>();
        var session = services.GetRequiredService<SessionState>();
        var result = compatibility.HandleBug(session, args);
        Console.WriteLine(result.Output);
        return result.ExitCode;
    }

    private static int HandleShellsCommand(IServiceProvider services)
    {
        var compatibility = services.GetRequiredService<CompatibilityCommandService>();
        var session = services.GetRequiredService<SessionState>();
        var result = compatibility.HandleShells(session);
        Console.WriteLine(result.Output);
        return result.ExitCode;
    }

    private static async Task<int> HandleTerminalSetupCommand(IServiceProvider services)
    {
        var compatibility = services.GetRequiredService<CompatibilityCommandService>();
        var result = await compatibility.HandleTerminalSetupDetailedAsync();
        Console.WriteLine(result.Output);
        return result.ExitCode;
    }

    private static async Task<int> HandleSetupGitHubCommandAsync(IServiceProvider services)
    {
        var compatibility = services.GetRequiredService<CompatibilityCommandService>();
        var result = await compatibility.HandleSetupGitHubAsync();
        Console.WriteLine(result.Output);
        return result.ExitCode;
    }

    private static int HandleSessionCommand(IServiceProvider services, string[] args)
    {
        var sessionService = services.GetRequiredService<SessionCommandService>();
        var session = services.GetRequiredService<SessionState>();
        var result = sessionService.Handle(session, args);
        Console.WriteLine(result.Output);
        return result.ExitCode;
    }

    private static int HandleInitCommand(IServiceProvider services)
    {
        var workspace = services.GetRequiredService<WorkspaceCommandService>();
        Console.WriteLine(workspace.InitializeMemoryFile(Directory.GetCurrentDirectory()));
        return 0;
    }

    private static int HandlePlanCommand(IServiceProvider services, string[] args)
    {
        var session = services.GetRequiredService<SessionState>();
        var planner = services.GetRequiredService<PlanCommandService>();

        if (args.Length == 0)
        {
            Console.WriteLine(planner.EnablePlanMode(session));
            return 0;
        }

        var directory = GetOption(args, "--directory") ?? session.WorkingDirectory;
        var taskParts = new List<string>();
        for (var index = 0; index < args.Length; index++)
        {
            if (args[index].Equals("--directory", StringComparison.OrdinalIgnoreCase))
            {
                index++;
                continue;
            }

            taskParts.Add(args[index]);
        }

        Console.WriteLine(planner.BuildPlan(string.Join(" ", taskParts).Trim(), directory));
        return 0;
    }

    private static int HandleWorkspaceCommand(IServiceProvider services, string[] args)
    {
        var workspace = services.GetRequiredService<WorkspaceCommandService>();
        var session = services.GetRequiredService<SessionState>();
        var sessionManager = services.GetRequiredService<SessionManager>();

        if (args.Length == 0 || args[0].Equals("show", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine(workspace.ShowWorkspaceSummary(session.WorkingDirectory));
            return 0;
        }

        if (args[0].Equals("switch", StringComparison.OrdinalIgnoreCase) && args.Length >= 2)
        {
            Console.WriteLine(workspace.SwitchWorkspace(session, sessionManager, args[1]));
            return 0;
        }

        Console.WriteLine("Usage: nim-cli workspace [show|switch <directory>]");
        return 1;
    }

    private static int HandleCompatibilityCommand(IServiceProvider services)
    {
        var compatibility = services.GetRequiredService<CompatibilityCommandService>();
        var result = compatibility.HandleCompatibility();
        Console.WriteLine(result.Output);
        return result.ExitCode;
    }

    private static int HandleUpdateCommand(IServiceProvider services)
    {
        Console.WriteLine(services.GetRequiredService<UpdateCommandService>().GetUpdateInfo());
        return 0;
    }

    private static int HandleExtensionsCommand(IServiceProvider services, string[] args)
    {
        var registry = services.GetRequiredService<RegistryCommandService>();
        var subcommand = args.Length == 0 ? "list" : args[0].ToLowerInvariant();
        switch (subcommand)
        {
            case "list":
                Console.WriteLine(registry.List(RegistryKind.Extensions));
                return 0;
            case "install":
            case "link":
                if (args.Length < 2)
                {
                    Console.WriteLine($"Usage: nim-cli extensions {subcommand} <source> [--ref <ref>] [--auto-update]");
                    return 1;
                }

                Console.WriteLine(registry.Add(
                    RegistryKind.Extensions,
                    args[1],
                    reference: GetOption(args, "--ref"),
                    autoUpdate: args.Contains("--auto-update", StringComparer.OrdinalIgnoreCase)));
                return 0;
            case "uninstall":
            case "remove":
                if (args.Length < 2)
                {
                    Console.WriteLine("Usage: nim-cli extensions uninstall <name>");
                    return 1;
                }

                Console.WriteLine(registry.Remove(RegistryKind.Extensions, args[1]));
                return 0;
            case "enable":
                Console.WriteLine(registry.SetEnabled(RegistryKind.Extensions, args.Length >= 2 ? args[1] : null, true, all: args.Contains("--all", StringComparer.OrdinalIgnoreCase)));
                return 0;
            case "disable":
                Console.WriteLine(registry.SetEnabled(RegistryKind.Extensions, args.Length >= 2 ? args[1] : null, false, all: args.Contains("--all", StringComparer.OrdinalIgnoreCase)));
                return 0;
            case "update":
                Console.WriteLine(registry.Update(args.Length >= 2 ? args[1] : null, all: args.Contains("--all", StringComparer.OrdinalIgnoreCase)));
                return 0;
            case "describe":
                Console.WriteLine(registry.Describe(RegistryKind.Extensions, args.Length >= 2 ? args[1] : null));
                return 0;
            case "rename":
                if (args.Length < 3)
                {
                    Console.WriteLine("Usage: nim-cli extensions rename <old-name> <new-name>");
                    return 1;
                }

                Console.WriteLine(registry.Rename(RegistryKind.Extensions, args[1], args[2]));
                return 0;
            case "set-description":
                if (args.Length < 3)
                {
                    Console.WriteLine("Usage: nim-cli extensions set-description <name> <description>");
                    return 1;
                }

                Console.WriteLine(registry.SetDescription(RegistryKind.Extensions, args[1], string.Join(" ", args.Skip(2))));
                return 0;
            case "new":
            case "validate":
            case "config":
            case "restart":
            case "explore":
                Console.WriteLine($"Extension subcommand '{subcommand}' 目前透過本機 manifest/runtime registry 管理。請使用 'extensions list' 檢查狀態，並用 'extensions install/link/remove/enable/disable' 管理項目。");
                return 0;
            default:
                Console.WriteLine($"Unknown extensions subcommand: {subcommand}");
                return 1;
        }
    }

    private static int HandleSkillsCommand(IServiceProvider services, string[] args)
    {
        var registry = services.GetRequiredService<RegistryCommandService>();
        var subcommand = args.Length == 0 ? "list" : args[0].ToLowerInvariant();
        switch (subcommand)
        {
            case "list":
                Console.WriteLine(registry.List(RegistryKind.Skills));
                return 0;
            case "install":
            case "link":
                if (args.Length < 2)
                {
                    Console.WriteLine($"Usage: nim-cli skills {subcommand} <source>");
                    return 1;
                }

                Console.WriteLine(registry.Add(RegistryKind.Skills, args[1]));
                return 0;
            case "uninstall":
            case "remove":
                if (args.Length < 2)
                {
                    Console.WriteLine("Usage: nim-cli skills uninstall <name>");
                    return 1;
                }

                Console.WriteLine(registry.Remove(RegistryKind.Skills, args[1]));
                return 0;
            case "enable":
                Console.WriteLine(registry.SetEnabled(RegistryKind.Skills, args.Length >= 2 ? args[1] : null, true, all: args.Contains("--all", StringComparer.OrdinalIgnoreCase)));
                return 0;
            case "disable":
                Console.WriteLine(registry.SetEnabled(RegistryKind.Skills, args.Length >= 2 ? args[1] : null, false, all: args.Contains("--all", StringComparer.OrdinalIgnoreCase)));
                return 0;
            case "reload":
                Console.WriteLine(registry.Reload(RegistryKind.Skills));
                return 0;
            case "describe":
                Console.WriteLine(registry.Describe(RegistryKind.Skills, args.Length >= 2 ? args[1] : null));
                return 0;
            case "rename":
                if (args.Length < 3)
                {
                    Console.WriteLine("Usage: nim-cli skills rename <old-name> <new-name>");
                    return 1;
                }

                Console.WriteLine(registry.Rename(RegistryKind.Skills, args[1], args[2]));
                return 0;
            case "set-description":
                if (args.Length < 3)
                {
                    Console.WriteLine("Usage: nim-cli skills set-description <name> <description>");
                    return 1;
                }

                Console.WriteLine(registry.SetDescription(RegistryKind.Skills, args[1], string.Join(" ", args.Skip(2))));
                return 0;
            default:
                Console.WriteLine($"Unknown skills subcommand: {subcommand}");
                return 1;
        }
    }

    private static void EnsureAuthenticated()
    {
        if (!UserConfigStore.HasApiKey())
        {
            Console.WriteLine("No API key found in appsettings.secret.json. Run 'nim-cli auth login' first.");
            throw new AuthenticationRequiredException();
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
            Nim-CLI - Terminal Agent powered by NVIDIA NIM

            Gemini-compatible entrypoints:
              nim-cli                              Start interactive chat in the current directory
              nim-cli "<prompt>"                   Send a prompt, then continue interactively
              nim-cli -p "<prompt>"                Run a prompt in headless mode
              nim-cli -i "<prompt>"                Run a prompt, then stay interactive
              nim-cli -m <model-id> "<prompt>"     Override the model for this invocation
              nim-cli --include-directories a,b    Add extra workspace directories to the session
              nim-cli -o text|json|stream-json     Choose headless output format
              nim-cli --version                    Show version info

            Model commands:
              nim-cli models list
              nim-cli model current
              nim-cli model show
              nim-cli model list
              nim-cli model set <model-id>
              nim-cli set model <model-id>
              nim-cli get model
              nim-cli config show
              nim-cli config set model <model-id>
              nim-cli config set baseurl <url>

            Auth and health:
              nim-cli auth login
              nim-cli auth status
              nim-cli doctor

            Project and coding:
              nim-cli analyze [--directory <dir>] [--build true]
              nim-cli repo map [--directory <dir>]
              nim-cli build [--project <path>] [--config Debug|Release]
              nim-cli run-project [--project <path>] [--args "..."]
              nim-cli code plan --task "<description>" [--directory <dir>]
              nim-cli code apply --file <path> --search <text> --replace <text>
              nim-cli test [--project <path>] [--working-dir <dir>]
              nim-cli lint [--project <path>] [--working-dir <dir>]

            Browser, web, database, git, ftp:
              nim-cli browser open [--width <n>] [--height <n>]
              nim-cli browser navigate --url <url> [--wait <seconds>]
              nim-cli browser wait [--seconds <n>]
              nim-cli browser close
              nim-cli screenshot --url <url> [--out screenshot.png]
              nim-cli db query --connection <conn> --query <sql>
              nim-cli git status [--working-dir <dir>]
              nim-cli git diff [--working-dir <dir>]
              nim-cli git commit [-m <message>] [--working-dir <dir>]
              nim-cli git push [--remote <name>] [--branch <name>] [--working-dir <dir>]
              nim-cli ftp upload --host <host> --user <user> --password <pwd> --local <file> --remote <path>

            MCP and tool inspection:
              nim-cli tools [desc]
              nim-cli mcp status
              nim-cli mcp tools
              nim-cli mcp list
              nim-cli mcp ls

            Gemini-compatible commands recognized by Nim-Cli:
              nim-cli update
              nim-cli extensions [list|install|uninstall|enable|disable|update|link|new|validate]
              nim-cli skills [list|install|uninstall|enable|disable|link]
              nim-cli memory [list|show|refresh]
              nim-cli init
              nim-cli settings [show|set <key> <value>]
              nim-cli hooks [list|show|panel|enable <name>|disable <name>|enable-all|disable-all]
              nim-cli agents [list|status|set <analysis|coding|ops>]
              nim-cli commands
              nim-cli compress [tag]
              nim-cli policies
              nim-cli restore [list|show <tag|index|latest>|<tag|index|latest>]
              nim-cli rewind [list|show <tag|index|latest>|<tag|index|latest>]
              nim-cli bug [file]
              nim-cli shells
              nim-cli terminal-setup
              nim-cli setup-github
              nim-cli vim [status|install|enable|disable]
              nim-cli session [show|clear|resume [latest|session-id|index]]
              nim-cli stats [session|tools|model]
              nim-cli permissions
              nim-cli permissions trust [<directory>]
              nim-cli plan
              nim-cli resume

            Gemini-compatible flags currently parsed:
              -h, --help
              -v, --version
              -m, --model <model-id>
              -p, --prompt <text>
              -i, --prompt-interactive <text>
              -o, --output-format text|json|stream-json
              --include-directories <dir1,dir2>
              --approval-mode default|auto_edit|yolo|plan
              -y, --yolo
              --list-extensions
              -r, --resume <id|latest>
              --list-sessions
              --delete-session <id>

            Interactive slash commands supported now:
            """);

        Console.WriteLine(GetInteractiveHelpText());

        Console.WriteLine("""

            Notes:
              - High-risk operations such as git push and ftp upload still require approval.
              - appsettings.secret.json in the execution directory is the writable config source.
              - Project memory files are named Nim.md in this application.
              - Use '-m <model-id>' for a one-off run, or 'set model' / 'model set' to persist.

            Examples:
              nim-cli
              nim-cli "analyze this project"
              nim-cli -m google/gemma-4-31b-it -p "summarize the architecture"
              nim-cli set model google/gemma-4-31b-it
              nim-cli analyze --build true
              nim-cli repo map --directory D:\src\my-project
            """);
    }

    private static void PrintVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
        Console.WriteLine($"Nim-Cli {version}");
    }

    private static void HandleConfig(string[] args, NimCliOptions options)
    {
        if (args.Length < 2 || args[1].ToLowerInvariant() == "show")
        {
            Console.WriteLine($"Provider:   {options.Provider.Name}");
            Console.WriteLine($"BaseUrl:    {options.Provider.BaseUrl}");
            Console.WriteLine($"Model:      {options.Provider.DefaultModel}");
            Console.WriteLine($"Streaming:  {options.Provider.Streaming}");
            Console.WriteLine($"Shell:      {options.Shell.Default}");
            return;
        }

        if (args[1].ToLowerInvariant() == "set" && args.Length >= 4)
        {
            var key = args[2].ToLowerInvariant();
            var value = args[3];
            switch (key)
            {
                case "model":
                    options.Provider.DefaultModel = value;
                    break;
                case "baseurl":
                    options.Provider.BaseUrl = value;
                    break;
                default:
                    Console.WriteLine($"Unknown config key: {key}");
                    return;
            }

            UserConfigStore.SaveUserConfig(options);
            Console.WriteLine($"Set {key} = {value}");
        }
    }

    private static ParsedCliArguments ParseCliArguments(string[] args, NimCliOptions options)
    {
        var remaining = new List<string>();
        var parsed = new ParsedCliArguments();

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-h":
                case "--help":
                    parsed.ShowHelp = true;
                    break;
                case "-v":
                case "--version":
                    parsed.ShowVersion = true;
                    break;
                case "-m":
                case "--model":
                    if (TryReadValue(args, ref i, out var model))
                        parsed.ModelOverride = model;
                    else
                        parsed.ShowHelp = true;
                    break;
                case "-p":
                case "--prompt":
                    if (TryReadValue(args, ref i, out var prompt))
                        parsed.HeadlessPrompt = prompt;
                    else
                        parsed.ShowHelp = true;
                    break;
                case "-i":
                case "--prompt-interactive":
                    if (TryReadValue(args, ref i, out var interactivePrompt))
                        parsed.InteractivePrompt = interactivePrompt;
                    else
                        parsed.ShowHelp = true;
                    break;
                case "-o":
                case "--output-format":
                    if (TryReadValue(args, ref i, out var outputFormat))
                        parsed.OutputFormat = outputFormat;
                    else
                        parsed.ShowHelp = true;
                    break;
                case "--include-directories":
                    if (TryReadValue(args, ref i, out var includeDirectories))
                        parsed.IncludeDirectories = SplitCommaSeparated(includeDirectories);
                    else
                        parsed.ShowHelp = true;
                    break;
                case "--approval-mode":
                    if (TryReadValue(args, ref i, out var approvalMode))
                        parsed.ApprovalMode = approvalMode;
                    else
                        parsed.ShowHelp = true;
                    break;
                case "-y":
                case "--yolo":
                    parsed.ApprovalMode = "yolo";
                    break;
                case "--list-extensions":
                case "-l":
                    parsed.ListExtensions = true;
                    break;
                case "-r":
                case "--resume":
                    if (TryReadValue(args, ref i, out var resumeTarget))
                        parsed.ResumeTarget = resumeTarget;
                    else
                        parsed.ShowHelp = true;
                    break;
                case "--list-sessions":
                    parsed.ListSessions = true;
                    break;
                case "--delete-session":
                    if (TryReadValue(args, ref i, out var deleteSession))
                        parsed.DeleteSessionId = deleteSession;
                    else
                        parsed.ShowHelp = true;
                    break;
                default:
                    remaining.Add(args[i]);
                    break;
            }
        }

        if (!IsValidOutputFormat(parsed.OutputFormat))
        {
            Console.WriteLine($"Unsupported output format: {parsed.OutputFormat}");
            parsed.ShowHelp = true;
        }

        if (!string.IsNullOrWhiteSpace(parsed.ModelOverride))
            options.Provider.DefaultModel = parsed.ModelOverride;

        parsed.RemainingArgs = remaining.ToArray();
        return parsed;
    }

    private static void InitializeSession(IServiceProvider services, ParsedCliArguments parsed, SessionManager sessionManager)
    {
        var session = services.GetRequiredService<SessionState>();
        sessionManager.InitializeNewSession(session, Directory.GetCurrentDirectory(), parsed.IncludeDirectories);
        session.UserPreferences["approval_mode"] = parsed.ApprovalMode;
    }

    private static async Task<int> RunHeadlessAsync(IServiceProvider services, string prompt, string outputFormat, string approvalMode)
    {
        var orchestrator = services.GetRequiredService<AgentOrchestrator>();
        var session = services.GetRequiredService<SessionState>();
        var sessionManager = services.GetRequiredService<SessionManager>();
        ConfigureApproval(orchestrator, approvalMode);

        var chunks = new List<string>();
        if (outputFormat.Equals("stream-json", StringComparison.OrdinalIgnoreCase))
        {
            var initPayload = JsonSerializer.Serialize(new { type = "init", model = services.GetRequiredService<NimCliOptions>().Provider.DefaultModel, cwd = session.WorkingDirectory });
            Console.WriteLine(initPayload);
            orchestrator.OnChunk = chunk =>
            {
                chunks.Add(chunk);
                Console.WriteLine(JsonSerializer.Serialize(new { type = "message", role = "assistant", chunk }));
            };
        }

        try
        {
            var response = await orchestrator.RunAsync(prompt);
            orchestrator.OnChunk = null;
            sessionManager.SaveSession(session);

            return outputFormat.ToLowerInvariant() switch
            {
                "json" => PrintJsonResult(response),
                "stream-json" => PrintStreamJsonResult(response),
                _ => PrintTextResult(response)
            };
        }
        catch (Exception ex)
        {
            orchestrator.OnChunk = null;
            if (outputFormat.Equals("json", StringComparison.OrdinalIgnoreCase) || outputFormat.Equals("stream-json", StringComparison.OrdinalIgnoreCase))
                Console.WriteLine(JsonSerializer.Serialize(new { error = new { message = ex.Message } }));
            else
                Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private static int PrintTextResult(AgentResponse response)
    {
        Console.WriteLine(response.Content);
        return 0;
    }

    private static int PrintJsonResult(AgentResponse response)
    {
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            response = response.Content,
            toolResults = response.ToolResults?.Select(result => new { result.Name, result.IsError }),
            stats = new { toolCalls = response.ToolResults?.Count ?? 0 }
        }));
        return 0;
    }

    private static int PrintStreamJsonResult(AgentResponse response)
    {
        if (response.ToolResults != null)
        {
            foreach (var tool in response.ToolResults)
                Console.WriteLine(JsonSerializer.Serialize(new { type = "tool_result", name = tool.Name, isError = tool.IsError }));
        }

        Console.WriteLine(JsonSerializer.Serialize(new { type = "result", response = response.Content, stats = new { toolCalls = response.ToolResults?.Count ?? 0 } }));
        return 0;
    }

    private static void ConfigureApproval(AgentOrchestrator orchestrator, string approvalMode)
    {
        if (approvalMode.Equals("yolo", StringComparison.OrdinalIgnoreCase) || approvalMode.Equals("auto_edit", StringComparison.OrdinalIgnoreCase))
        {
            orchestrator.ApprovalCallback = _ => Task.FromResult(true);
            return;
        }

        if (approvalMode.Equals("plan", StringComparison.OrdinalIgnoreCase))
        {
            orchestrator.ApprovalCallback = _ => Task.FromResult(false);
            return;
        }

        orchestrator.ApprovalCallback = _ => Task.FromResult(false);
    }

    private static async Task<string> BuildPromptWithRedirectedInputAsync(string? prompt)
    {
        var redirectedInput = Console.IsInputRedirected ? await Console.In.ReadToEndAsync() : string.Empty;
        if (string.IsNullOrWhiteSpace(redirectedInput))
            return prompt ?? string.Empty;

        if (string.IsNullOrWhiteSpace(prompt))
            return redirectedInput.Trim();

        return $"{redirectedInput.Trim()}\n\n{prompt}";
    }

    private static bool LooksLikePositionalPrompt(string firstArg)
        => !firstArg.StartsWith("-", StringComparison.Ordinal) && !IsKnownCommand(firstArg);

    private static bool IsKnownCommand(string firstArg)
    {
        var knownCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "auth", "models", "model", "set", "get", "chat", "run", "config", "doctor", "build", "run-project", "screenshot",
            "browser", "db", "ftp", "git", "repo", "analyze", "code", "mcp", "test", "lint", "playwright", "help", "tools",
            "permissions", "stats", "memory", "settings", "hooks", "agents", "commands", "compress", "policies", "restore", "rewind", "bug", "shells", "bashes", "terminal-setup", "ide", "setup-github", "vim", "init", "plan", "update", "extensions", "skills", "resume", "session", "workspace", "compatibility"
        };

        return knownCommands.Contains(firstArg);
    }

    private static bool IsUnknownCommandLikeToken(string firstArg)
        => !string.IsNullOrWhiteSpace(firstArg)
           && !firstArg.StartsWith("-", StringComparison.Ordinal)
           && firstArg.Contains('-', StringComparison.Ordinal)
           && !IsKnownCommand(firstArg);

    private static bool TryReadValue(string[] args, ref int index, out string value)
    {
        if (index + 1 < args.Length)
        {
            value = args[++index];
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static string? GetOption(string[] args, string name)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        }

        return null;
    }

    private static bool IsValidOutputFormat(string outputFormat)
        => outputFormat.Equals("text", StringComparison.OrdinalIgnoreCase) ||
           outputFormat.Equals("json", StringComparison.OrdinalIgnoreCase) ||
           outputFormat.Equals("stream-json", StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyList<string> SplitCommaSeparated(string value)
        => value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static NimCliOptions CloneOptions(NimCliOptions options)
        => JsonSerializer.Deserialize<NimCliOptions>(JsonSerializer.Serialize(options)) ?? new NimCliOptions();

    private sealed class ParsedCliArguments
    {
        public bool ShowHelp { get; set; }
        public bool ShowVersion { get; set; }
        public string? ModelOverride { get; set; }
        public string? HeadlessPrompt { get; set; }
        public string? InteractivePrompt { get; set; }
        public string OutputFormat { get; set; } = "text";
        public IReadOnlyList<string> IncludeDirectories { get; set; } = [];
        public string ApprovalMode { get; set; } = "default";
        public bool ListExtensions { get; set; }
        public string? ResumeTarget { get; set; }
        public bool ListSessions { get; set; }
        public string? DeleteSessionId { get; set; }
        public string[] RemainingArgs { get; set; } = [];
    }

    private sealed class AuthenticationRequiredException : Exception;
}
