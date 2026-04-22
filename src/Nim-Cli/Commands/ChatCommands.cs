using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using NimCli.Core;
using NimCli.Infrastructure;
using NimCli.Infrastructure.Config;

namespace NimCli.App.Commands;

public class ChatCommands
{
    public static async Task RunChatAsync(IServiceProvider services, string? initialPrompt = null, bool continueInteractive = false, string approvalMode = "default")
    {
        var orchestrator = services.GetRequiredService<AgentOrchestrator>();
        var session = services.GetRequiredService<SessionState>();
        var sessionManager = services.GetRequiredService<SessionManager>();

        orchestrator.ApprovalCallback = approvalMode.ToLowerInvariant() switch
        {
            "yolo" or "auto_edit" => _ => Task.FromResult(true),
            "plan" => _ => Task.FromResult(false),
            _ => async prompt =>
            {
                Console.Write($"\n[APPROVAL REQUIRED] {prompt}");
                var answer = Console.ReadLine()?.Trim().ToLowerInvariant();
                return answer is "y" or "yes";
            }
        };

        Console.WriteLine("Nim-CLI Chat (type '/help' for slash commands, 'exit' or '/quit' to leave)");
        Console.WriteLine(new string('-', 60));

        if (!string.IsNullOrWhiteSpace(initialPrompt))
        {
            var shouldContinue = await ProcessInputAsync(orchestrator, services, initialPrompt, approvalMode);
            sessionManager.SaveSession(session);
            if (!continueInteractive || !shouldContinue)
                return;
        }

        while (true)
        {
            Console.Write("\n> ");
            var input = Console.ReadLine()?.Trim();

            if (string.IsNullOrWhiteSpace(input))
                continue;

            var shouldContinue = await ProcessInputAsync(orchestrator, services, input, approvalMode);
            sessionManager.SaveSession(session);
            if (!shouldContinue)
                break;
        }
    }

    private static async Task<bool> ProcessInputAsync(AgentOrchestrator orchestrator, IServiceProvider services, string input, string approvalMode)
    {
        if (input == "!")
        {
            Console.WriteLine("Shell mode toggle is recognized. Use '!<command>' to run a one-shot shell command.");
            return true;
        }

        if (input.StartsWith('!'))
        {
            var workspace = services.GetRequiredService<WorkspaceCommandService>();
            var command = input[1..].Trim();
            if (string.IsNullOrWhiteSpace(command))
            {
                Console.WriteLine("Usage: !<shell command>");
                return true;
            }

            Console.WriteLine(await workspace.RunShellPassthroughAsync(command, services.GetRequiredService<SessionState>().WorkingDirectory));
            return true;
        }

        if (input.Contains('@'))
            input = ExpandAtPathContext(services, input);

        if (input.StartsWith('/'))
            return await HandleSlashCommandAsync(orchestrator, services, input, approvalMode);

        Console.WriteLine();
        using var cts = new CancellationTokenSource();
        var streamed = false;

        orchestrator.OnChunk = chunk =>
        {
            streamed = true;
            Console.Write(chunk);
        };

        ConsoleCancelEventHandler? handler = null;
        handler = (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };
        Console.CancelKeyPress += handler;

        try
        {
            var response = await orchestrator.RunAsync(input, cts.Token);

            if (response.ToolResults?.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine($"[Tools executed: {string.Join(", ", response.ToolResults.Select(r => r.Name))}]");
                Console.ResetColor();
            }

            if (streamed)
                Console.WriteLine();
            else
                Console.WriteLine(response.Content);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("\n[Cancelled]");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {ex.Message}");
            Console.ResetColor();
        }
        finally
        {
            orchestrator.OnChunk = null;
            Console.CancelKeyPress -= handler;
        }

        return true;
    }

    private static async Task<bool> HandleSlashCommandAsync(AgentOrchestrator orchestrator, IServiceProvider services, string input, string approvalMode)
    {
        var result = await services.GetRequiredService<InteractiveCommandService>().ExecuteAsync(
            services,
            input,
            approvalMode,
            onExit: () => Task.FromResult(new InteractiveCommandResult(false, string.Empty)));

        if (result.ClearScreen)
            Console.Clear();

        if (!string.IsNullOrWhiteSpace(result.Output))
            Console.WriteLine(result.Output);

        return result.ShouldContinue;
    }

    private static string ExpandAtPathContext(IServiceProvider services, string input)
    {
        var workspace = services.GetRequiredService<WorkspaceCommandService>();
        var session = services.GetRequiredService<SessionState>();
        var tokens = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var expanded = new List<string>();

        foreach (var token in tokens)
        {
            if (token.StartsWith('@') && token.Length > 1)
                expanded.Add(workspace.ReadPathContext(session.WorkingDirectory, token[1..]));
            else
                expanded.Add(token);
        }

        return string.Join(" ", expanded);
    }
}
