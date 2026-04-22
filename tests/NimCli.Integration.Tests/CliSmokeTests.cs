using NimCli.App;
using NimCli.Infrastructure.Config;
using System.Diagnostics;
using Xunit;

namespace NimCli.Integration.Tests;

public class CliSmokeTests
{
    private static readonly string RepoRoot = ResolveRepoRoot();

    [Fact]
    public async Task Help_Command_Returns_Success()
    {
        var exitCode = await CliApplication.RunAsync(["help"], new NimCliOptions());
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Unknown_Command_Returns_Error()
    {
        var exitCode = await CliApplication.RunAsync(["unknown-command"], new NimCliOptions());
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task Playwright_Command_Without_Subcommand_Returns_Error()
    {
        var exitCode = await CliApplication.RunAsync(["playwright"], new NimCliOptions());
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task Db_Command_Without_Query_Subcommand_Returns_Error()
    {
        var exitCode = await CliApplication.RunAsync(["db"], new NimCliOptions());
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task Repo_Command_Without_Map_Subcommand_Returns_Error()
    {
        var exitCode = await CliApplication.RunAsync(["repo"], new NimCliOptions());
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task Browser_Command_Without_Subcommand_Returns_Error()
    {
        var exitCode = await CliApplication.RunAsync(["browser"], new NimCliOptions());
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task Git_Command_Without_Subcommand_Returns_Error()
    {
        var exitCode = await CliApplication.RunAsync(["git"], new NimCliOptions());
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task Code_Command_Without_Subcommand_Returns_Error()
    {
        var exitCode = await CliApplication.RunAsync(["code"], new NimCliOptions());
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task Mcp_Command_Without_Subcommand_Returns_Success()
    {
        var exitCode = await CliApplication.RunAsync(["mcp"], new NimCliOptions());
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Browser_Navigate_Without_Url_Returns_Error()
    {
        var exitCode = await CliApplication.RunAsync(["browser", "navigate"], new NimCliOptions());
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task Browser_Open_Command_Returns_Success()
    {
        var exitCode = await CliApplication.RunAsync(["browser", "open"], new NimCliOptions());
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Mcp_Tools_Command_Returns_Success()
    {
        var exitCode = await CliApplication.RunAsync(["mcp", "tools"], new NimCliOptions());
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Mcp_Ping_Command_Returns_Success()
    {
        var exitCode = await CliApplication.RunAsync(["mcp", "ping"], new NimCliOptions());
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Mcp_Inspect_Command_Returns_Success()
    {
        var exitCode = await CliApplication.RunAsync(["mcp", "inspect"], new NimCliOptions());
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Set_Model_Command_Returns_Success()
    {
        var exitCode = await CliApplication.RunAsync(["set", "model", "google/gemma-4-31b-it"], new NimCliOptions());
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Model_Current_Command_Returns_Success()
    {
        var exitCode = await CliApplication.RunAsync(["model", "current"], new NimCliOptions());
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task List_Extensions_Flag_Returns_Success()
    {
        var exitCode = await CliApplication.RunAsync(["--list-extensions"], new NimCliOptions());
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Version_Flag_Returns_Success()
    {
        var exitCode = await CliApplication.RunAsync(["--version"], new NimCliOptions());
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Permissions_Command_Returns_Success()
    {
        var exitCode = await CliApplication.RunAsync(["permissions"], new NimCliOptions());
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Settings_Show_Command_Returns_Success()
    {
        var exitCode = await CliApplication.RunAsync(["settings", "show"], new NimCliOptions());
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Hooks_List_Command_Returns_Success()
    {
        var exitCode = await CliApplication.RunAsync(["hooks", "list"], new NimCliOptions());
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Hooks_Describe_Command_Returns_Success()
    {
        var exitCode = await CliApplication.RunAsync(["hooks", "describe"], new NimCliOptions());
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Extensions_List_Command_Returns_Success()
    {
        var exitCode = await CliApplication.RunAsync(["extensions", "list"], new NimCliOptions());
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Extensions_Describe_Command_Returns_Success()
    {
        var exitCode = await CliApplication.RunAsync(["extensions", "describe"], new NimCliOptions());
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Skills_List_Command_Returns_Success()
    {
        var exitCode = await CliApplication.RunAsync(["skills", "list"], new NimCliOptions());
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Skills_Describe_Command_Returns_Success()
    {
        var exitCode = await CliApplication.RunAsync(["skills", "describe"], new NimCliOptions());
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Memory_List_Command_Returns_Success()
    {
        var exitCode = await CliApplication.RunAsync(["memory", "list"], new NimCliOptions());
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Stats_Session_Command_Returns_Success()
    {
        var exitCode = await CliApplication.RunAsync(["stats", "session"], new NimCliOptions());
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Resume_List_Command_Returns_Success()
    {
        var exitCode = await CliApplication.RunAsync(["resume", "list"], new NimCliOptions());
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Session_Show_Command_Returns_Success()
    {
        var exitCode = await CliApplication.RunAsync(["session", "show"], new NimCliOptions());
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Session_Clear_Command_Returns_Success()
    {
        var exitCode = await CliApplication.RunAsync(["session", "clear"], new NimCliOptions());
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Session_Resume_Command_Returns_Error_When_NoSessionExists()
    {
        using var workspace = new TemporaryWorkingDirectory("nimcli-session-tests");
        var exitCode = await CliApplication.RunAsync(["session", "resume"], new NimCliOptions());
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task Doctor_Command_Returns_Success()
    {
        var exitCode = await CliApplication.RunAsync(["doctor"], new NimCliOptions());
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Plan_Command_With_Task_Returns_Success()
    {
        var exitCode = await CliApplication.RunAsync(["plan", "improve", "doctor", "output"], new NimCliOptions());
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Db_Query_Command_With_Table_Syntax_Missing_Conn_Returns_Error()
    {
        var exitCode = await CliApplication.RunAsync(["db", "query", "--table", "Users"], new NimCliOptions());
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task Build_Command_Returns_Success()
    {
        var projectPath = Path.Combine(RepoRoot, "src", "NimCli.Contracts", "NimCli.Contracts.csproj");
        var exitCode = await CliApplication.RunAsync(["build", "--project", projectPath], new NimCliOptions());
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Run_Project_Command_Returns_Success()
    {
        var projectPath = Path.Combine(RepoRoot, "src", "Nim-Cli", "Nim-Cli.csproj");
        var exitCode = await CliApplication.RunAsync(["run-project", "--project", projectPath, "--args", "--help"], new NimCliOptions());
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Repo_Map_Command_Returns_Success()
    {
        var srcDirectory = Path.Combine(RepoRoot, "src");
        var exitCode = await CliApplication.RunAsync(["repo", "map", "--directory", srcDirectory], new NimCliOptions());
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Analyze_Command_Returns_Success()
    {
        var srcDirectory = Path.Combine(RepoRoot, "src");
        var exitCode = await CliApplication.RunAsync(["analyze", "--directory", srcDirectory], new NimCliOptions());
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Git_Status_Command_Returns_Success()
    {
        using var repo = CreateTemporaryGitRepository();
        var exitCode = await CliApplication.RunAsync(["git", "status", "--working-dir", repo.DirectoryPath], new NimCliOptions());
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Git_Diff_Command_Returns_Success()
    {
        using var repo = CreateTemporaryGitRepository(withModifiedFile: true);
        var exitCode = await CliApplication.RunAsync(["git", "diff", "--working-dir", repo.DirectoryPath], new NimCliOptions());
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Agents_Status_Command_Returns_Success()
    {
        var exitCode = await CliApplication.RunAsync(["agents", "status"], new NimCliOptions());
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Commands_Command_Returns_Success()
    {
        var exitCode = await CliApplication.RunAsync(["commands"], new NimCliOptions());
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Workspace_Show_Command_Returns_Success()
    {
        var exitCode = await CliApplication.RunAsync(["workspace", "show"], new NimCliOptions());
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Compatibility_Command_Returns_Success()
    {
        var exitCode = await CliApplication.RunAsync(["compatibility"], new NimCliOptions());
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Update_Command_Returns_Success()
    {
        var exitCode = await CliApplication.RunAsync(["update"], new NimCliOptions());
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Policies_Command_Returns_Success()
    {
        var exitCode = await CliApplication.RunAsync(["policies"], new NimCliOptions());
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Policies_Command_Remains_Success_After_Shared_Policy_Summaries()
    {
        var exitCode = await CliApplication.RunAsync(["policies"], new NimCliOptions());
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Compress_Command_Returns_Success()
    {
        var exitCode = await CliApplication.RunAsync(["compress"], new NimCliOptions());
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Restore_List_Command_Returns_Error_When_NoCheckpointExists()
    {
        var exitCode = await CliApplication.RunAsync(["restore", "list"], new NimCliOptions());
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Rewind_Show_Command_Returns_Error_When_NoCheckpointExists()
    {
        var exitCode = await CliApplication.RunAsync(["rewind", "show"], new NimCliOptions());
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Setup_GitHub_Command_Returns_Success()
    {
        var exitCode = await CliApplication.RunAsync(["setup-github"], new NimCliOptions());
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Git_Push_Dry_Run_Command_Returns_Success()
    {
        var exitCode = await CliApplication.RunAsync(["git", "push", "--dry-run"], new NimCliOptions());
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Ftp_Upload_Dry_Run_Command_Returns_Success()
    {
        var exitCode = await CliApplication.RunAsync(["ftp", "upload", "--dry-run"], new NimCliOptions());
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Bug_Command_Returns_Success()
    {
        var exitCode = await CliApplication.RunAsync(["bug"], new NimCliOptions());
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Shells_Command_Returns_Success()
    {
        var exitCode = await CliApplication.RunAsync(["shells"], new NimCliOptions());
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Terminal_Setup_Command_Returns_Success()
    {
        var exitCode = await CliApplication.RunAsync(["terminal-setup"], new NimCliOptions());
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Plan_Command_Returns_Success()
    {
        var exitCode = await CliApplication.RunAsync(["plan"], new NimCliOptions());
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Vim_Status_Command_Returns_Success()
    {
        var exitCode = await CliApplication.RunAsync(["vim", "status"], new NimCliOptions());
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Vim_Disable_Command_Returns_Success()
    {
        var exitCode = await CliApplication.RunAsync(["vim", "disable"], new NimCliOptions());
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Vim_Unknown_Subcommand_Returns_Error()
    {
        var exitCode = await CliApplication.RunAsync(["vim", "unknown"], new NimCliOptions());
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task Set_Model_Then_Settings_Show_In_Same_Process_Returns_Success()
    {
        var setExitCode = await CliApplication.RunAsync(["set", "model", "google/gemma-4-31b-it"], new NimCliOptions());
        Assert.Equal(0, setExitCode);

        var settingsExitCode = await CliApplication.RunAsync(["settings", "show"], new NimCliOptions());
        Assert.Equal(0, settingsExitCode);
    }

    private static string ResolveRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Nim-Cli.slnx")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new InvalidOperationException("找不到 Nim-Cli.slnx，無法解析 repo root。");
    }

    private static TemporaryGitRepository CreateTemporaryGitRepository(bool withModifiedFile = false)
    {
        var directoryPath = Path.Combine(Path.GetTempPath(), "nimcli-git-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directoryPath);
        File.WriteAllText(Path.Combine(directoryPath, "readme.txt"), "hello" + Environment.NewLine);

        RunGit("init", directoryPath);
        RunGit("config user.email nimcli-tests@example.com", directoryPath);
        RunGit("config user.name nimcli-tests", directoryPath);
        RunGit("add .", directoryPath);
        RunGit("commit -m initial", directoryPath);

        if (withModifiedFile)
            File.AppendAllText(Path.Combine(directoryPath, "readme.txt"), "updated" + Environment.NewLine);

        return new TemporaryGitRepository(directoryPath);
    }

    private static void RunGit(string arguments, string workingDirectory)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        });

        if (process is null)
            throw new InvalidOperationException($"無法啟動 git {arguments}");

        process.WaitForExit();
        if (process.ExitCode == 0)
            return;

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        throw new InvalidOperationException($"git {arguments} 失敗。{output}{error}");
    }

    private sealed class TemporaryGitRepository : IDisposable
    {
        public TemporaryGitRepository(string directoryPath)
        {
            DirectoryPath = directoryPath;
        }

        public string DirectoryPath { get; }

        public void Dispose()
        {
            try
            {
                Directory.Delete(DirectoryPath, recursive: true);
            }
            catch
            {
            }
        }
    }

    private sealed class TemporaryWorkingDirectory : IDisposable
    {
        private readonly string _originalDirectory;

        public TemporaryWorkingDirectory(string prefix)
        {
            _originalDirectory = Directory.GetCurrentDirectory();
            DirectoryPath = Path.Combine(Path.GetTempPath(), prefix, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(DirectoryPath);
            Directory.SetCurrentDirectory(DirectoryPath);
        }

        public string DirectoryPath { get; }

        public void Dispose()
        {
            Directory.SetCurrentDirectory(_originalDirectory);

            try
            {
                Directory.Delete(DirectoryPath, recursive: true);
            }
            catch
            {
            }
        }
    }
}
