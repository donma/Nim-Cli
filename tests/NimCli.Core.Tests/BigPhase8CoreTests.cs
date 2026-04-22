using NimCli.App;
using NimCli.Contracts;
using NimCli.Core;
using NimCli.Infrastructure.Config;
using NimCli.Tools.Db;
using NimCli.Tools.Shell;
using Xunit;

namespace NimCli.Core.Tests;

public sealed class BigPhase8CoreTests
{
    [Fact]
    public void BuildDotNetRunCommand_Quotes_Project_Path_With_Spaces()
    {
        var command = ShellCommandComposer.BuildDotNetRunCommand("D:\\Repo Root\\app\\Demo.csproj", "-- --help");

        Assert.Contains("'D:\\Repo Root\\app\\Demo.csproj'", command, StringComparison.Ordinal);
        Assert.Contains("'--' '--help'", command, StringComparison.Ordinal);
    }

    [Fact]
    public void TokenizeArguments_Preserves_Quoted_Segments_And_Special_Characters()
    {
        var tokens = PowerShellCommandBuilder.TokenizeArguments("--filter \"name with spaces\" --message \"fix user's path\" --literal a;b");

        Assert.Equal(["--filter", "name with spaces", "--message", "fix user's path", "--literal", "a;b"], tokens);
    }

    [Fact]
    public async Task QueryDbTool_Rejects_Raw_Query_Without_Explicit_Raw_Mode()
    {
        var tool = new QueryDbTool();
        var result = await tool.ExecuteAsync(new Dictionary<string, object?>
        {
            ["connection_string"] = "Data Source=:memory:",
            ["query"] = "SELECT * FROM Users"
        });

        Assert.False(result.Success);
        Assert.Contains("raw_mode=true", result.ErrorMessage ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task QueryDbTool_Allows_Explicit_Advanced_Raw_Query_Mode()
    {
        var tool = new QueryDbTool();
        var result = await tool.ExecuteAsync(new Dictionary<string, object?>
        {
            ["connection_string"] = "Data Source=:memory:",
            ["query"] = "SELECT 1",
            ["db_type"] = "sqlite",
            ["raw_mode"] = true
        });

        Assert.True(result.Success);
        Assert.Contains("[1 row(s) returned]", result.Output);
    }

    [Fact]
    public async Task QueryDbTool_Builds_Structured_Query_With_Columns()
    {
        var tool = new QueryDbTool();
        var result = await tool.ExecuteAsync(new Dictionary<string, object?>
        {
            ["connection_string"] = "Data Source=:memory:",
            ["query"] = null,
            ["table"] = "Users",
            ["columns"] = "Id,Name",
            ["db_type"] = "sqlite"
        });

        Assert.False(result.Success);
        Assert.Contains("Query failed", result.ErrorMessage ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task QueryDbTool_Rejects_Raw_Mode_Without_Explicit_Query()
    {
        var tool = new QueryDbTool();
        var result = await tool.ExecuteAsync(new Dictionary<string, object?>
        {
            ["connection_string"] = "Data Source=:memory:",
            ["table"] = "Users",
            ["raw_mode"] = true,
            ["db_type"] = "sqlite"
        });

        Assert.False(result.Success);
        Assert.Contains("only valid with explicit raw query", result.ErrorMessage ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task QueryDbTool_Rejects_Readonly_Boundary_Bypass_Patterns()
    {
        var tool = new QueryDbTool();
        var result = await tool.ExecuteAsync(new Dictionary<string, object?>
        {
            ["connection_string"] = "Data Source=:memory:",
            ["query"] = "SELECT * FROM Users CROSS APPLY OPENROWSET('x', 'y')",
            ["db_type"] = "sqlite",
            ["raw_mode"] = true
        });

        Assert.False(result.Success);
        Assert.Contains("blocked readonly-boundary", result.ErrorMessage ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExecutionSummaryFormatter_Includes_Artifacts_Approval_And_Tool_Result_Summaries()
    {
        var session = new SessionState();
        session.RecordContextStrategy("coding");
        session.RecordCurrentTask("補 BigPhase8 summary");
        session.RecordRepoMap("RepoMap: src, tests");
        session.RecordBuildSummary("Build passed");
        session.RecordTestSummary("Tests passed");
        session.RecordScreenshotPath("D:\\tmp\\shot.png");
        session.RecordSuggestedCommitMessage("refine summary visibility");
        session.AddPolicyAudit(new PolicyAuditEntry("run_shell", "Ask", "High", false, "High risk requires approval", "command=dir"));

        var response = new AgentResponse(
            "done",
            RequiresApproval: true,
            ApprovalPrompt: "Allow run_shell?",
            ApprovalRequest: new ApprovalRequest("run_shell", "High", "command=dir", false, "High risk requires approval", "Allow run_shell?"),
            ToolResults:
            [
                new ToolCallResult("1", "build_project", "Build passed"),
                new ToolCallResult("2", "run_shell", "dir output")
            ]);

        var formatter = new ExecutionSummaryFormatter();
        var summary = formatter.BuildExecutionSummary(response, session, 88);
        var text = formatter.FormatExecutionSummary(summary);

        Assert.Contains("Approval:", text);
        Assert.Contains("Artifact [repo_map]", text);
        Assert.Contains("Artifact [build_summary]", text);
        Assert.Contains("Tool Result: run_shell:", text);
    }

    [Fact]
    public void UserConfigStore_LoadUserConfig_Allows_Workspace_Override_Over_User_Level_Base()
    {
        var originalHome = Environment.GetEnvironmentVariable("NIMCLI_HOME");
        var originalDirectory = Directory.GetCurrentDirectory();
        var tempHome = Path.Combine(Path.GetTempPath(), "nimcli-phase8-home", Guid.NewGuid().ToString("N"));
        var workspace = Path.Combine(Path.GetTempPath(), "nimcli-phase8-workspace", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempHome);
        Directory.CreateDirectory(workspace);

        try
        {
            Environment.SetEnvironmentVariable("NIMCLI_HOME", tempHome);
            Directory.SetCurrentDirectory(workspace);

            File.WriteAllText(Path.Combine(tempHome, "appsettings.secret.json"), """
{
  "NimCli": {
    "Provider": {
      "DefaultModel": "user-model",
      "BaseUrl": "https://user.example"
    }
  }
}
""");

            File.WriteAllText(Path.Combine(workspace, "appsettings.json"), """
{
  "NimCli": {
    "Provider": {
      "DefaultModel": "workspace-model"
    }
  }
}
""");

            var options = UserConfigStore.LoadUserConfig();

            Assert.Equal("workspace-model", options.Provider.DefaultModel);
            Assert.Equal("https://user.example", options.Provider.BaseUrl);
        }
        finally
        {
            Environment.SetEnvironmentVariable("NIMCLI_HOME", originalHome);
            if (Directory.Exists(originalDirectory))
                Directory.SetCurrentDirectory(originalDirectory);
            else
                Directory.SetCurrentDirectory(Path.GetTempPath());
            try { Directory.Delete(tempHome, recursive: true); } catch { }
            try { Directory.Delete(workspace, recursive: true); } catch { }
        }
    }
}
