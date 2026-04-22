using System.Text;
using Microsoft.Extensions.DependencyInjection;
using NimCli.App;
using NimCli.Contracts;
using NimCli.Core;
using NimCli.Infrastructure.Config;

namespace NimTui.App;

public static class TuiApplication
{
    public static async Task<int> RunAsync(NimCliOptions options)
    {
        var services = await ServiceConfiguration.BuildServicesAsync(options);
        var orchestrator = services.GetRequiredService<AgentOrchestrator>();
        var session = services.GetRequiredService<SessionState>();
        var sessionManager = services.GetRequiredService<SessionManager>();
        var summaryFormatter = services.GetRequiredService<ExecutionSummaryFormatter>();
        var policySummaryService = services.GetRequiredService<PolicySummaryService>();

        sessionManager.InitializeNewSession(session, Directory.GetCurrentDirectory(), []);
        session.UserPreferences["approval_mode"] = "default";
        session.UserPreferences["tui_focus"] = "input";
        session.UserPreferences["tui_busy"] = "idle";

        RenderOpening(options, session);

        ApprovalRequest? pendingApproval = null;
        var lastStatus = "Ready. Use /help for slash commands, /palette for quick actions.";

        orchestrator.ApprovalRequestCallback = request =>
        {
            pendingApproval = request;
            RenderConsole(BuildRenderState(session, options, summaryFormatter, policySummaryService, lastStatus, pendingApproval));
            Console.Write("Approve [a]llow / [d]eny / [v]details ? ");
            var answer = (Console.ReadLine() ?? string.Empty).Trim().ToLowerInvariant();
            if (answer is "v" or "details")
            {
                Console.WriteLine();
                Console.WriteLine(FormatApprovalDialog(request, expanded: true));
                Console.Write("Approve [a]llow / [d]eny ? ");
                answer = (Console.ReadLine() ?? string.Empty).Trim().ToLowerInvariant();
            }

            pendingApproval = null;
            var approved = answer is "a" or "allow" or "y" or "yes";
            lastStatus = approved ? $"Approved: {request.ToolName}" : $"Denied: {request.ToolName}";
            session.UserPreferences["tui_busy"] = approved ? "running" : "idle";
            return Task.FromResult(approved);
        };

        while (true)
        {
            RenderConsole(BuildRenderState(session, options, summaryFormatter, policySummaryService, lastStatus, pendingApproval));
            var input = ReadInteractiveInput(session)?.Trim();

            if (string.IsNullOrWhiteSpace(input))
                continue;

            if (TryHandleTuiCommand(input, session, sessionManager, options, policySummaryService, out var tuiOutput, out var shouldContinue))
            {
                lastStatus = tuiOutput;
                if (!shouldContinue)
                    return 0;

                continue;
            }

            if (input.StartsWith('/'))
            {
                session.UserPreferences["tui_busy"] = "running";
                var result = await services.GetRequiredService<InteractiveCommandService>().ExecuteAsync(
                    services,
                    input,
                    session.UserPreferences.GetValueOrDefault("approval_mode", "default"),
                    onExit: () => Task.FromResult(new InteractiveCommandResult(false, lastStatus)));
                session.UserPreferences["tui_busy"] = "idle";
                lastStatus = string.IsNullOrWhiteSpace(result.Output) ? lastStatus : result.Output;
                session.AddRecentAction($"slash:{input}");
                if (!result.ShouldContinue)
                    return 0;

                continue;
            }

            try
            {
                session.UserPreferences["tui_busy"] = "running";
                var response = await orchestrator.RunAsync(input);
                sessionManager.SaveSession(session);
                lastStatus = summaryFormatter.FormatExecutionSummary(
                    response.Summary ?? summaryFormatter.BuildExecutionSummary(response, session, 0));
            }
            catch (Exception ex)
            {
                lastStatus = $"Error: {ex.Message}";
            }
            finally
            {
                session.UserPreferences["tui_busy"] = "idle";
            }
        }
    }

    public static string RenderOpeningFrame(NimCliOptions options, SessionState session)
        => BuildOpeningAnimationFrames(options, session).Last();

    public static IReadOnlyList<string> BuildOpeningAnimationFrames(NimCliOptions options, SessionState session)
    {
        var logo = new[]
        {
            "##    ## #### ##     ##        ######## ##     ## ####",
            "###   ##  ##  ###   ###           ##    ##     ##  ## ",
            "####  ##  ##  #### ####           ##    ##     ##  ## ",
            "## ## ##  ##  ## ### ##           ##    ##     ##  ## ",
            "##  ####  ##  ##     ##           ##    ##     ##  ## ",
            "##   ###  ##  ##     ##           ##    ##     ##  ## ",
            "##    ## #### ##     ##           ##     #######  ####"
        };

        return
        [
            string.Join(Environment.NewLine,
            [
                string.Join(Environment.NewLine, logo),
                string.Empty,
                "NIM-TUI // command deck",
                $"Provider  : {options.Provider.Name}",
                $"Model     : {options.Provider.DefaultModel}",
                $"Workspace : {session.WorkingDirectory}",
                "Hotkeys   : /palette  /help  /session  /workspace"
            ])
        ];
    }

    public static IReadOnlyList<string> BuildSlashCommandSuggestions()
        =>
        [
            "/palette",
            "/help",
            "/recent",
            "/sessions",
            "/policy",
            "/mode coding",
            "/mode ops",
            "/mode analysis",
            "/build --project <path>",
            "/run-project --project <path>",
            "/analyze --directory <dir>",
            "/browser open",
            "/browser navigate --url <url>",
            "/screenshot --url <url>",
            "/git status --working-dir <dir>",
            "/db query --table <name>",
            "/workspace show",
            "/session show"
        ];

    public static string BuildInputHint(string input, SessionState session)
    {
        var trimmed = input.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return session.Mode switch
            {
                AgentMode.Coding => "Coding mode: describe the fix, or type /build, /analyze, /run-project.",
                AgentMode.Ops => "Ops mode: ask for browser/db/git actions, or type /policy, /browser, /db.",
                _ => "Type a prompt, or start with / for commands. Try /palette, /recent, /sessions."
            };
        }

        if (trimmed.StartsWith('/') || trimmed.StartsWith(':'))
        {
            var normalized = trimmed.Replace(':', '/');
            var matches = BuildSlashCommandSuggestions()
                .Where(suggestion => suggestion.StartsWith(normalized, StringComparison.OrdinalIgnoreCase)
                    || suggestion.Contains(normalized.TrimStart('/'), StringComparison.OrdinalIgnoreCase))
                .Take(4)
                .ToList();

            return matches.Count == 0
                ? "No exact slash match. Try /palette or /help."
                : "Suggestions: " + string.Join("  |  ", matches);
        }

        return session.Mode switch
        {
            AgentMode.Coding => "Enter to send. Coding context budget and recent repo state stay prioritized.",
            AgentMode.Ops => "Enter to send. Policy trail and recent tool outputs stay visible.",
            _ => "Enter to send. Use /palette for quick actions."
        };
    }

    public static string ApplySuggestion(string input)
    {
        var trimmed = input.Trim();
        if (!(trimmed.StartsWith('/') || trimmed.StartsWith(':')))
            return input;

        var normalized = trimmed.Replace(':', '/');
        var match = BuildSlashCommandSuggestions()
            .FirstOrDefault(suggestion => suggestion.StartsWith(normalized, StringComparison.OrdinalIgnoreCase));
        return match ?? input;
    }

    public static IReadOnlyList<string> BuildCommandPaletteEntries()
        =>
        [
            "Build",
            "Run Project",
            "Screenshot",
            "Analyze",
            "Repo Map",
            "Doctor",
            "Resume Session",
            "Model List",
            "Tools List",
            "Policy Summary",
            "DB Query",
            "Git Status",
            "Git Push",
            "FTP Upload",
            "Plan Mode",
            "Settings",
            "Extensions",
            "Skills",
            "Hooks"
        ];

    public static string FormatCommandPalette()
        => string.Join(Environment.NewLine,
            BuildCommandPaletteEntries().Select((entry, index) => $"{index + 1,2}. {entry}"));

    public static string FormatApprovalDialog(ApprovalRequest request, bool expanded = false)
    {
        var builder = new StringBuilder();
        builder.AppendLine("[Approval Dialog]");
        builder.AppendLine($"Tool      : {request.ToolName}");
        builder.AppendLine($"Risk      : {request.RiskLevel}");
        builder.AppendLine($"Dry-Run   : {request.DryRun}");
        builder.AppendLine($"Reason    : {request.Reason}");
        if (expanded)
            builder.AppendLine($"Input     : {request.InputSummary}");
        else
            builder.AppendLine($"Input     : {SingleLine(request.InputSummary, 90)}");
        builder.AppendLine("Actions   : allow / deny / details");
        return builder.ToString().TrimEnd();
    }

    public static string BuildRecentSessionsPanel(SessionManager sessionManager, SessionState session)
    {
        var sessions = sessionManager.ListSessions(session.WorkspaceKey).Take(5).ToList();
        if (sessions.Count == 0)
            return "(no recent sessions)";

        return string.Join(Environment.NewLine,
            sessions.Select((item, index) => $"{index + 1}. {item.SessionId[..Math.Min(8, item.SessionId.Length)]} | {item.Title} | messages={item.MessageCount}"));
    }

    public static string BuildPolicyPanel(PolicySummaryService policySummaryService)
        => string.Join(Environment.NewLine,
            policySummaryService.GetSummaries().Take(8)
                .Select(item => $"- {item.ToolName}: {item.Decision}/{item.RiskLevel} dry-run={item.DryRun}"));

    public static string BuildLayout(
        SessionState session,
        NimCliOptions options,
        ExecutionSummaryFormatter summaryFormatter,
        PolicySummaryService policySummaryService,
        string lastStatus,
        ApprovalRequest? pendingApproval,
        SessionManager? sessionManager = null)
    {
        var state = BuildRenderState(session, options, summaryFormatter, policySummaryService, lastStatus, pendingApproval, sessionManager);
        return state.Layout;
    }

    public static TuiUiCommandResult HandleUiCommandForTest(
        string input,
        SessionState session,
        SessionManager sessionManager,
        NimCliOptions options,
        PolicySummaryService policySummaryService)
    {
        var handled = TryHandleTuiCommand(input, session, sessionManager, options, policySummaryService, out var output, out var shouldContinue);
        return new TuiUiCommandResult(handled, shouldContinue, output);
    }

    public static TuiRenderState BuildRenderState(
        SessionState session,
        NimCliOptions options,
        ExecutionSummaryFormatter summaryFormatter,
        PolicySummaryService policySummaryService,
        string lastStatus,
        ApprovalRequest? pendingApproval,
        SessionManager? sessionManager = null)
    {
        var sessionSummary = summaryFormatter.BuildSessionSummary(session);
        var transcript = new StringBuilder();
        foreach (var message in session.ConversationHistory.TakeLast(10))
            transcript.AppendLine($"[{message.Role}] {SingleLine(message.Content, 110)}");

        if (transcript.Length == 0)
            transcript.AppendLine("(no transcript yet)");

        var status = new StringBuilder();
        status.AppendLine($"Task: {SingleLine(sessionSummary.CurrentTask ?? "(none)", 96)}");
        status.AppendLine($"Context: {sessionSummary.LastContextStrategy ?? "general"}");
        status.AppendLine($"Busy: {session.UserPreferences.GetValueOrDefault("tui_busy", "idle")}");
        status.AppendLine($"Focus: {session.UserPreferences.GetValueOrDefault("tui_focus", "input")}");
        status.AppendLine($"Mode: {sessionSummary.Mode}");
        status.AppendLine($"Last Status: {SingleLine(lastStatus, 96)}");
        status.AppendLine($"Recent Actions: {(sessionSummary.RecentActions?.Count > 0 ? string.Join(" | ", sessionSummary.RecentActions.TakeLast(4)) : "(none)")}");
        status.AppendLine();
        status.AppendLine("Policy");
        status.AppendLine(BuildPolicyPanel(policySummaryService));
        status.AppendLine();
        status.AppendLine("Recent Sessions");
        status.AppendLine(sessionManager is null ? "(session manager unavailable)" : BuildRecentSessionsPanel(sessionManager, session));
        status.AppendLine();
        status.AppendLine("Palette");
        status.AppendLine(string.Join(" | ", BuildCommandPaletteEntries().Take(6)) + " | ...");

        var footer = "Hotkeys: /palette /focus transcript|status|input /mode analysis|coding|ops /recent /sessions /help /exit";
        var approval = pendingApproval is null ? null : FormatApprovalDialog(pendingApproval);
        var outputPane = BuildOutputPane(lastStatus);

        var layout = new StringBuilder();
        layout.AppendLine($"Nim-Tui | Provider={options.Provider.Name} | Model={options.Provider.DefaultModel} | Mode={sessionSummary.Mode} | Session={sessionSummary.SessionId[..8]}");
        layout.AppendLine($"Workspace: {sessionSummary.WorkingDirectory}");
        layout.AppendLine(new string('=', 110));
        layout.AppendLine("[Transcript Pane]");
        layout.AppendLine(transcript.ToString().TrimEnd());
        layout.AppendLine(new string('-', 110));
        layout.AppendLine("[Status Pane]");
        layout.AppendLine(status.ToString().TrimEnd());
        if (!string.IsNullOrWhiteSpace(outputPane))
        {
            layout.AppendLine(new string('-', 110));
            layout.AppendLine("[Output Pane]");
            layout.AppendLine(outputPane);
        }
        if (!string.IsNullOrWhiteSpace(approval))
        {
            layout.AppendLine(new string('-', 110));
            layout.AppendLine(approval);
        }
        layout.AppendLine(new string('-', 110));
        layout.AppendLine("[Input Pane]");
        layout.AppendLine("Type a prompt, slash command, or /palette for quick actions.");
        layout.AppendLine(new string('=', 110));
        layout.AppendLine(footer);

        return new TuiRenderState(
            Header: $"Nim-Tui | Provider={options.Provider.Name} | Model={options.Provider.DefaultModel}",
            TranscriptPane: transcript.ToString().TrimEnd(),
            StatusPane: status.ToString().TrimEnd(),
            OutputPane: outputPane,
            InputPane: "Type a prompt, slash command, or /palette for quick actions.",
            Footer: footer,
            ApprovalDialog: approval,
            Layout: layout.ToString().TrimEnd());
    }

    private static bool TryHandleTuiCommand(
        string input,
        SessionState session,
        SessionManager sessionManager,
        NimCliOptions options,
        PolicySummaryService policySummaryService,
        out string output,
        out bool shouldContinue)
    {
        output = string.Empty;
        shouldContinue = true;

        if (input.Equals(":palette", StringComparison.OrdinalIgnoreCase) || input.Equals("/palette", StringComparison.OrdinalIgnoreCase))
        {
            output = FormatCommandPalette();
            session.AddRecentAction("tui:palette");
            return true;
        }

        if (input.Equals(":recent", StringComparison.OrdinalIgnoreCase) || input.Equals("/recent", StringComparison.OrdinalIgnoreCase))
        {
            output = session.RecentActions.Count == 0 ? "(no recent actions)" : string.Join(Environment.NewLine, session.RecentActions.TakeLast(10));
            return true;
        }

        if (input.Equals(":sessions", StringComparison.OrdinalIgnoreCase) || input.Equals("/sessions", StringComparison.OrdinalIgnoreCase))
        {
            output = BuildRecentSessionsPanel(sessionManager, session);
            return true;
        }

        if (input.StartsWith(":focus ", StringComparison.OrdinalIgnoreCase) || input.StartsWith("/focus ", StringComparison.OrdinalIgnoreCase))
        {
            var focus = input.Split(' ', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? "input";
            session.UserPreferences["tui_focus"] = focus;
            output = $"Focus switched to {focus}.";
            return true;
        }

        if (input.StartsWith(":mode ", StringComparison.OrdinalIgnoreCase) || input.StartsWith("/mode ", StringComparison.OrdinalIgnoreCase))
        {
            var modeToken = input.Split(' ', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
            if (Enum.TryParse<AgentMode>(modeToken, ignoreCase: true, out var mode))
            {
                session.Mode = mode;
                output = $"Mode switched to {mode}.";
                return true;
            }

            output = "Usage: /mode analysis|coding|ops";
            return true;
        }

        if (input.Equals(":policy", StringComparison.OrdinalIgnoreCase) || input.Equals("/policy", StringComparison.OrdinalIgnoreCase))
        {
            output = policySummaryService.FormatSummaries();
            return true;
        }

        if (input.Equals(":quit", StringComparison.OrdinalIgnoreCase))
        {
            shouldContinue = false;
            return true;
        }

        return false;
    }

    private static void RenderOpening(NimCliOptions options, SessionState session)
    {
        var skipOpening = string.Equals(Environment.GetEnvironmentVariable("NIM_TUI_SKIP_OPENING"), "1", StringComparison.OrdinalIgnoreCase);
        if (skipOpening)
            return;

        var frame = RenderOpeningFrame(options, session);
        Console.Clear();
        WriteOpeningFrame(frame);

        var pauseMs = 1400;
        if (int.TryParse(Environment.GetEnvironmentVariable("NIM_TUI_OPENING_PAUSE_MS"), out var configuredPause))
            pauseMs = Math.Clamp(configuredPause, 0, 5000);

        if (pauseMs > 0 && !Console.IsInputRedirected)
            Thread.Sleep(pauseMs);
    }

    private static void RenderConsole(TuiRenderState state)
    {
        Console.Clear();
        WriteColoredLine(state.Header, ConsoleColor.Cyan);
        WriteColoredLine(new string('=', 110), ConsoleColor.DarkGray);
        WriteColoredLine("[Transcript Pane]", ConsoleColor.Magenta);
        Console.WriteLine(state.TranscriptPane);
        WriteColoredLine(new string('-', 110), ConsoleColor.DarkGray);
        WriteColoredLine("[Status Pane]", ConsoleColor.Green);
        WriteStatusPane(state.StatusPane);

        if (!string.IsNullOrWhiteSpace(state.OutputPane))
        {
            WriteColoredLine(new string('-', 110), ConsoleColor.DarkGray);
            WriteColoredLine("[Output Pane]", ConsoleColor.Blue);
            Console.WriteLine(state.OutputPane);
        }

        if (!string.IsNullOrWhiteSpace(state.ApprovalDialog))
        {
            WriteColoredLine(new string('-', 110), ConsoleColor.DarkGray);
            WriteColoredBlock(state.ApprovalDialog!, ConsoleColor.Yellow);
        }

        WriteColoredLine(new string('-', 110), ConsoleColor.DarkGray);
        WriteColoredLine("[Input Pane]", ConsoleColor.Cyan);
        Console.WriteLine(state.InputPane);
        WriteColoredLine(new string('=', 110), ConsoleColor.DarkGray);
        WriteColoredLine(state.Footer, ConsoleColor.DarkGray);
        Console.WriteLine();
    }

    private static string? ReadInteractiveInput(SessionState session)
    {
        if (Console.IsInputRedirected)
            return Console.ReadLine();

        var buffer = new StringBuilder();
        var promptRow = Console.CursorTop;
        var renderedLines = 0;

        while (true)
        {
            RenderPrompt(session, buffer.ToString(), promptRow, ref renderedLines);
            var key = Console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.Enter)
            {
                ClearPromptArea(promptRow, renderedLines);
                Console.SetCursorPosition(0, promptRow);
                Console.WriteLine();
                return buffer.ToString();
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (buffer.Length > 0)
                    buffer.Remove(buffer.Length - 1, 1);
                continue;
            }

            if (key.Key == ConsoleKey.Tab)
            {
                var completed = ApplySuggestion(buffer.ToString());
                buffer.Clear();
                buffer.Append(completed);
                continue;
            }

            if (key.Key == ConsoleKey.Escape)
            {
                buffer.Clear();
                continue;
            }

            if (!char.IsControl(key.KeyChar))
                buffer.Append(key.KeyChar);
        }
    }

    private static void RenderPrompt(SessionState session, string input, int promptRow, ref int renderedLines)
    {
        ClearPromptArea(promptRow, renderedLines);
        Console.SetCursorPosition(0, promptRow);

        var promptColor = session.Mode switch
        {
            AgentMode.Coding => ConsoleColor.Cyan,
            AgentMode.Ops => ConsoleColor.Yellow,
            _ => ConsoleColor.Green
        };

        Console.ForegroundColor = promptColor;
        Console.Write("Input> ");
        Console.ResetColor();
        Console.WriteLine(input);

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("Hint > ");
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine(SingleLine(BuildInputHint(input, session), Math.Max(40, Console.WindowWidth - 8)));
        Console.ResetColor();
        renderedLines = 2;
    }

    private static void ClearPromptArea(int promptRow, int lines)
    {
        if (lines <= 0)
            return;

        var width = Math.Max(20, Console.WindowWidth - 1);
        for (var index = 0; index < lines; index++)
        {
            var row = Math.Min(promptRow + index, Console.BufferHeight - 1);
            Console.SetCursorPosition(0, row);
            Console.Write(new string(' ', width));
        }

        Console.SetCursorPosition(0, promptRow);
    }

    private static void WriteOpeningFrame(string frame)
    {
        foreach (var line in frame.Split(new[] { '\r', '\n' }, StringSplitOptions.None))
        {
            if (line.StartsWith("#", StringComparison.Ordinal))
                WriteColoredLine(line, ConsoleColor.Green);
            else if (line.StartsWith("NIM-TUI", StringComparison.OrdinalIgnoreCase))
                WriteColoredLine(line, ConsoleColor.Green);
            else if (line.StartsWith("Provider", StringComparison.OrdinalIgnoreCase) || line.StartsWith("Model", StringComparison.OrdinalIgnoreCase) || line.StartsWith("Workspace", StringComparison.OrdinalIgnoreCase))
                WriteColoredLine(line, ConsoleColor.Gray);
            else if (line.StartsWith("Hotkeys", StringComparison.OrdinalIgnoreCase))
                WriteColoredLine(line, ConsoleColor.DarkGray);
            else
                Console.WriteLine(line);
        }
    }

    private static void WriteStatusPane(string content)
    {
        foreach (var line in content.Split(new[] { '\r', '\n' }, StringSplitOptions.None))
        {
            if (line.StartsWith("Busy:", StringComparison.OrdinalIgnoreCase))
                WriteColoredLine(line, line.Contains("running", StringComparison.OrdinalIgnoreCase) ? ConsoleColor.Yellow : ConsoleColor.Green);
            else if (line.StartsWith("Task:", StringComparison.OrdinalIgnoreCase) || line.StartsWith("Context:", StringComparison.OrdinalIgnoreCase) || line.StartsWith("Mode:", StringComparison.OrdinalIgnoreCase) || line.StartsWith("Last Status:", StringComparison.OrdinalIgnoreCase))
                WriteColoredLine(line, ConsoleColor.Cyan);
            else if (line.Equals("Policy", StringComparison.OrdinalIgnoreCase))
                WriteColoredLine(line, ConsoleColor.Magenta);
            else if (line.Equals("Recent Sessions", StringComparison.OrdinalIgnoreCase))
                WriteColoredLine(line, ConsoleColor.Blue);
            else if (line.Equals("Palette", StringComparison.OrdinalIgnoreCase))
                WriteColoredLine(line, ConsoleColor.DarkCyan);
            else if (line.Contains("Ask/High", StringComparison.OrdinalIgnoreCase) || line.Contains("Deny/", StringComparison.OrdinalIgnoreCase))
                WriteColoredLine(line, ConsoleColor.Yellow);
            else
                Console.WriteLine(line);
        }
    }

    private static void WriteColoredBlock(string content, ConsoleColor color)
    {
        foreach (var line in content.Split(new[] { '\r', '\n' }, StringSplitOptions.None))
            WriteColoredLine(line, color);
    }

    private static void WriteColoredLine(string content, ConsoleColor color)
    {
        var previous = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(content);
        Console.ForegroundColor = previous;
    }

    private static string? BuildOutputPane(string lastStatus)
    {
        if (string.IsNullOrWhiteSpace(lastStatus))
            return null;

        var trimmed = lastStatus.Trim();
        var hasMultipleLines = trimmed.Contains('\n') || trimmed.Contains('\r');
        if (!hasMultipleLines && trimmed.Length <= 120)
            return null;

        return trimmed;
    }

    private static string SingleLine(string value, int maxLength)
    {
        var line = value.Replace("\r", " ").Replace("\n", " ").Trim();
        if (line.Length <= maxLength)
            return line;

        return line[..Math.Max(0, maxLength - 3)] + "...";
    }
}

public sealed record TuiRenderState(
    string Header,
    string TranscriptPane,
    string StatusPane,
    string? OutputPane,
    string InputPane,
    string Footer,
    string? ApprovalDialog,
    string Layout);

public sealed record TuiUiCommandResult(bool Handled, bool ShouldContinue, string Output);
