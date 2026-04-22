using NimCli.Coding;
using NimCli.Tools.Shell;
using Xunit;

namespace NimCli.Core.Tests;

public class CodingPipelineTests
{
    [Fact]
    public async Task ExecuteEditWorkflowAsync_Returns_Structured_Summary()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "nimcli-core-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var filePath = Path.Combine(tempDir, "Sample.cs");
        File.WriteAllText(filePath, "class Sample { string Value = \"old\"; }");

        try
        {
            var repoMap = new RepoMapBuilder();
            var planner = new CodeEditPlanner(repoMap);
            var shell = new FakeShellProvider();
            var patcher = new PatchApplier();
            var pipeline = new CodingPipeline(repoMap, shell, planner, patcher);

            var result = await pipeline.ExecuteEditWorkflowAsync(
                "update sample value",
                filePath,
                "old",
                "new",
                verifyBuild: true,
                verifyTests: true);

            Assert.True(result.Success);
            Assert.True(result.PatchApplied);
            Assert.True(result.BuildVerified);
            Assert.True(result.TestVerified);
            Assert.Contains("Plan -> edit -> build -> test -> summarize", result.Summary);
            Assert.Equal("new", File.ReadAllText(filePath).Contains("new") ? "new" : "missing");
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }

    private sealed class FakeShellProvider : IShellProvider
    {
        public Task<ShellResult> ExecuteAsync(string command, string? workingDir = null, int timeoutSeconds = 60, CancellationToken cancellationToken = default)
            => Task.FromResult(new ShellResult(0, $"OK: {command}", string.Empty));
    }
}
