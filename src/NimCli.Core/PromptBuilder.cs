using NimCli.Contracts;

namespace NimCli.Core;

public class PromptBuilder
{
    private const string SystemPromptTemplate = """
        You are Nim-CLI, a powerful terminal agent assistant running on Windows with PowerShell.
        You help users with: project analysis, building/running .NET projects, web research,
        browser automation, database queries, FTP uploads, Git operations, and general coding tasks.

        IMPORTANT RULES:
        - Always use available tools rather than generating shell scripts directly
        - For file operations, use structured tool calls
        - For high-risk operations (git push, ftp upload, file deletion), always inform the user before proceeding
        - Keep responses concise and actionable
        - When working directory context is provided, use it to ground your responses
        - If the user asks for a commit summary, derive it from recent verified tool output and repo context

        Current working directory: {workingDir}
        Current mode: {mode}
        """;

    public List<ChatMessage> BuildMessages(
        SessionState session,
        string userInput,
        string? additionalContext = null)
    {
        var systemContent = SystemPromptTemplate
            .Replace("{workingDir}", session.WorkingDirectory)
            .Replace("{mode}", session.Mode.ToString());

        if (!string.IsNullOrWhiteSpace(additionalContext))
            systemContent += $"\n\nAdditional Context:\n{additionalContext}";

        var messages = new List<ChatMessage>
        {
            new("system", systemContent)
        };

        // Add conversation history (keep last 20 turns to stay within token budget)
        var history = session.ConversationHistory.TakeLast(40).ToList();
        messages.AddRange(history);

        // Add current user input
        messages.Add(new("user", userInput));

        return messages;
    }
}
