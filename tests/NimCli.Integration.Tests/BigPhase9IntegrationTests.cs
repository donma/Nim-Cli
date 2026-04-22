using NimCli.App;
using NimCli.Contracts;
using NimCli.Core;
using Xunit;

namespace NimCli.Integration.Tests;

public sealed class BigPhase9IntegrationTests
{
    [Fact]
    public void Long_Context_Resume_Workflow_Preserves_Task_Policy_And_Audit_Metadata()
    {
        var session = new SessionState { Mode = AgentMode.Analysis };
        session.RecordCurrentTask("resume shell safety review for quoted commit message");
        session.RecordRepoMap(string.Join('\n', Enumerable.Range(0, 120).Select(index => $"src/File{index}.cs")));
        session.RecordBuildSummary(new string('b', 1800));
        session.RecordShellResult("git_commit", new string('s', 2200));
        session.AddRecentAction("tool:git_commit");
        session.AddPolicyAudit(new PolicyAuditEntry("run_shell", "Ask", "High", false, "High risk requires approval", "command=git log --stat"));
        for (var index = 0; index < 8; index++)
        {
            session.AddUserMessage($"resume user {index}");
            session.AddAssistantMessage($"resume assistant {index}");
        }

        var builder = new ContextBuilder();
        builder.AddSessionState(session, AgentMode.Analysis);
        var context = builder.Build(1600);

        Assert.Contains("resume shell safety review", context);
        Assert.Contains("[Recent Conversation]", context);
        Assert.Contains("[Policy Audit]", context);
        Assert.Contains("[Context Audit]", context);
    }

    [Fact]
    public void Summary_Consistency_Workflow_Carries_Context_And_Db_Mode_Evidence()
    {
        var session = new SessionState();
        session.RecordCurrentTask("verify db safety boundary summary");
        session.RecordContextStrategy("resume");
        session.RecordDbResult("structured:Users where=Id=1 top=20", "Id | Name\n1 | Alice");
        session.RecordShellResult("run_shell", "git status ok");
        session.AddPolicyAudit(new PolicyAuditEntry("query_db", "Allow", "Low", false, "Structured query allowed", "table=Users, where=Id=1"));

        var formatter = new ExecutionSummaryFormatter();
        var summary = formatter.BuildExecutionSummary(
            new AgentResponse(
                "done",
                ToolResults:
                [
                    new ToolCallResult("1", "query_db", "{\"success\":true,\"mode\":\"structured\"}"),
                    new ToolCallResult("2", "run_shell", "{\"success\":true,\"dry_run\":false}")
                ]),
            session,
            210);
        var text = formatter.FormatExecutionSummary(summary);

        Assert.Contains("Context: resume", text);
        Assert.Contains("DB: structured:Users", text);
        Assert.Contains("Policy [query_db]: Allow/Low", text);
        Assert.Contains("Tool Result: query_db:", text);
    }
}
