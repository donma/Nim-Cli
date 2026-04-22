using NimCli.Core;
using NimCli.Tools.Db;
using NimCli.Tools.Shell;
using Xunit;

namespace NimCli.Core.Tests;

public sealed class BigPhase9CoreTests
{
    [Fact]
    public void ContextBuilder_Adds_Audit_Block_And_Compressed_Conversation_For_Resume_Workflows()
    {
        var session = new SessionState { Mode = AgentMode.Analysis };
        session.RecordCurrentTask("resume last coding task around shell escaping");
        for (var index = 0; index < 12; index++)
        {
            session.AddUserMessage($"user message {index} {new string('u', 80)}");
            session.AddAssistantMessage($"assistant message {index} {new string('a', 80)}");
        }

        var builder = new ContextBuilder();
        builder.AddSessionState(session, AgentMode.Analysis);
        var context = builder.Build(1200);

        Assert.Equal("resume", builder.LastStrategy);
        Assert.Contains("[Recent Conversation]", context);
        Assert.Contains("[Context Audit]", context);
        Assert.Contains("Compressed:", context);
    }

    [Fact]
    public void BuildDotNetRunCommand_Quotes_Arguments_With_Spaces_And_Semicolons()
    {
        var command = ShellCommandComposer.BuildDotNetRunCommand("D:\\Repo Root\\app\\Demo.csproj", "--filter \"name with spaces\" --literal a;b");

        Assert.Contains("'--' '--filter' 'name with spaces' '--literal' 'a;b'", command, StringComparison.Ordinal);
    }

    [Fact]
    public async Task QueryDbTool_Rejects_Too_Complex_Structured_Where_Clause()
    {
        var tool = new QueryDbTool();
        var result = await tool.ExecuteAsync(new Dictionary<string, object?>
        {
            ["connection_string"] = "Data Source=:memory:",
            ["table"] = "Users",
            ["where"] = "Id=1 AND A=1 AND B=2 AND C=3 AND D=4 AND E=5 AND F=6 AND G=7 AND H=8",
            ["db_type"] = "sqlite"
        });

        Assert.False(result.Success);
        Assert.Contains("too complex", result.ErrorMessage ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }
}
