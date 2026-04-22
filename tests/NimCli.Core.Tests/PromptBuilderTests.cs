using NimCli.Contracts;
using NimCli.Core;
using Xunit;

namespace NimCli.Core.Tests;

public class PromptBuilderTests
{
    [Fact]
    public void BuildMessages_Includes_System_Context_History_And_User_Input()
    {
        var session = new SessionState
        {
            WorkingDirectory = "D:\\repo",
            Mode = AgentMode.Coding
        };
        session.ConversationHistory.Add(new ChatMessage("assistant", "previous answer"));

        var builder = new PromptBuilder();
        var messages = builder.BuildMessages(session, "current request", "extra context");

        Assert.Equal("system", messages[0].Role);
        Assert.Contains("Current working directory: D:\\repo", messages[0].Content);
        Assert.Contains("Current mode: Coding", messages[0].Content);
        Assert.Contains("Additional Context:\nextra context", messages[0].Content);
        Assert.Contains(messages, message => message.Role == "assistant" && message.Content == "previous answer");
        Assert.Equal("user", messages[^1].Role);
        Assert.Equal("current request", messages[^1].Content);
    }

    [Fact]
    public void BuildMessages_Keeps_Recent_History_Window()
    {
        var session = new SessionState();
        for (var index = 0; index < 50; index++)
            session.ConversationHistory.Add(new ChatMessage("user", $"message-{index}"));

        var builder = new PromptBuilder();
        var messages = builder.BuildMessages(session, "tail");

        Assert.Equal(42, messages.Count);
        Assert.DoesNotContain(messages, message => message.Content == "message-0");
        Assert.Contains(messages, message => message.Content == "message-49");
    }
}
