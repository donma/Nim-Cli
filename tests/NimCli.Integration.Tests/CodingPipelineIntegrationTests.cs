using NimCli.Coding;
using NimCli.Tools.Shell;
using Xunit;

namespace NimCli.Integration.Tests;

public sealed class CodingPipelineIntegrationTests
{
    [Fact]
    public async Task CodingPipeline_Project_Level_Workflow_Runs_Analyze_Plan_Edit_Build_Test_Summary()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "nimcli-coding-integration", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var projectDir = Path.Combine(tempDir, "App");
        Directory.CreateDirectory(projectDir);
        var filePath = Path.Combine(projectDir, "Worker.cs");
        File.WriteAllText(filePath, "class Worker { string Name = \"old\"; }");

        try
        {
            var repoMap = new RepoMapBuilder();
            var planner = new CodeEditPlanner(repoMap);
            var patcher = new PatchApplier();
            var shell = new FakeShellProvider();
            var pipeline = new CodingPipeline(repoMap, shell, planner, patcher);

            var result = await pipeline.ExecuteEditWorkflowAsync(
                "rename worker field",
                filePath,
                "old",
                "new",
                verifyBuild: true,
                verifyTests: true);

            Assert.True(result.Success);
            Assert.True(result.PatchApplied);
            Assert.True(result.BuildVerified);
            Assert.True(result.TestVerified);
            Assert.Contains("Worker.cs", result.FilePath, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Plan -> edit -> build -> test -> summarize", result.Summary);
            Assert.False(string.IsNullOrWhiteSpace(result.RepoMap));
            Assert.Contains("Repo Map", result.RepoMap, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(2, shell.Commands.Count);
            Assert.Contains("dotnet build", shell.Commands[0]);
            Assert.Contains("dotnet test", shell.Commands[1]);
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }

    private sealed class FakeShellProvider : IShellProvider
    {
        public List<string> Commands { get; } = [];

        public Task<ShellResult> ExecuteAsync(string command, string? workingDir = null, int timeoutSeconds = 60, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            return Task.FromResult(new ShellResult(0, $"OK: {command}", string.Empty));
        }
    }
}
