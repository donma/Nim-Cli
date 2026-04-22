using NimCli.Tools.Abstractions;
using NimCli.Tools.Shell;

namespace NimCli.Coding;

public sealed record CodingWorkflowResult(
    bool Success,
    string Summary,
    CodeEditPlan Plan,
    string FilePath,
    string RepoMap,
    bool PatchApplied,
    bool BuildVerified,
    bool TestVerified,
    string? SuggestedCommitMessage);

public class AnalyzeProjectTool : ITool
{
    private readonly RepoMapBuilder _repoMap;
    private readonly IShellProvider _shell;

    public AnalyzeProjectTool(RepoMapBuilder repoMap, IShellProvider shell)
    {
        _repoMap = repoMap;
        _shell = shell;
    }

    public string Name => "analyze_project";
    public string Description => "Analyze a .NET project or solution structure, build status, and provide suggestions";
    public RiskLevel RiskLevel => RiskLevel.Low;
    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            directory = new { type = "string", description = "Root directory to analyze (default: current directory)" },
            include_build = new { type = "boolean", description = "Run dotnet build to check for errors (default: false)" }
        }
    };

    public async Task<ToolExecuteResult> ExecuteAsync(
        Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var directory = input.GetValueOrDefault("directory")?.ToString() ?? Directory.GetCurrentDirectory();
        var includeBuild = input.GetValueOrDefault("include_build")?.ToString()?.ToLower() == "true";

        var sb = new System.Text.StringBuilder();

        // Repo map
        var map = _repoMap.BuildMap(directory);
        sb.AppendLine(map);

        // Optionally run build
        if (includeBuild)
        {
            sb.AppendLine("## Build Status");
            var result = await _shell.ExecuteAsync("dotnet build", directory, timeoutSeconds: 120, cancellationToken);
            sb.AppendLine(result.Success ? "Build: SUCCESS" : "Build: FAILED");
            sb.AppendLine(result.StandardOutput);
            if (!string.IsNullOrEmpty(result.StandardError))
                sb.AppendLine(result.StandardError);
        }

        return new ToolExecuteResult(true, sb.ToString());
    }
}

public class CodingPipeline
{
    private readonly RepoMapBuilder _repoMap;
    private readonly IShellProvider _shell;
    private readonly CodeEditPlanner _planner;
    private readonly PatchApplier _patchApplier;

    public CodingPipeline(RepoMapBuilder repoMap, IShellProvider shell, CodeEditPlanner planner, PatchApplier patchApplier)
    {
        _repoMap = repoMap;
        _shell = shell;
        _planner = planner;
        _patchApplier = patchApplier;
    }

    public string GetRepoMap(string directory)
        => _repoMap.BuildMap(directory);

    public async Task<bool> VerifyBuildAsync(string directory, CancellationToken cancellationToken = default)
    {
        var result = await _shell.ExecuteAsync("dotnet build", directory, timeoutSeconds: 120, cancellationToken);
        return result.Success;
    }

    public async Task<bool> VerifyTestsAsync(string directory, CancellationToken cancellationToken = default)
    {
        var result = await _shell.ExecuteAsync("dotnet test", directory, timeoutSeconds: 180, cancellationToken);
        return result.Success;
    }

    public CodeEditPlan PlanEdit(string task, string directory)
        => _planner.Plan(task, directory);

    public PatchApplyResult ApplyExactPatch(string filePath, string search, string replace)
        => _patchApplier.ApplyExactReplace(filePath, search, replace);

    public async Task<CodingWorkflowResult> ExecuteEditWorkflowAsync(
        string task,
        string filePath,
        string search,
        string replace,
        bool verifyBuild,
        bool verifyTests,
        CancellationToken cancellationToken = default)
    {
        var fullPath = Path.GetFullPath(filePath);
        var directory = Path.GetDirectoryName(fullPath) ?? Directory.GetCurrentDirectory();
        var repoMap = GetRepoMap(directory);
        var plan = PlanEdit(task, directory);
        var patchResult = ApplyExactPatch(fullPath, search, replace);
        if (!patchResult.Success)
        {
            return new CodingWorkflowResult(
                false,
                $"Plan -> edit -> verify 失敗：{patchResult.Error ?? patchResult.Summary}",
                plan,
                fullPath,
                repoMap,
                false,
                false,
                false,
                null);
        }

        var buildOk = !verifyBuild || await VerifyBuildAsync(directory, cancellationToken);
        var testsOk = !verifyTests || await VerifyTestsAsync(directory, cancellationToken);
        var suggestedCommitMessage = $"update {Path.GetFileNameWithoutExtension(fullPath).ToLowerInvariant()} workflow";
        var summary = $"Plan -> edit -> build -> test -> summarize 完成。Patch={(patchResult.Success ? "SUCCESS" : "FAILED")}, Build={(buildOk ? "SUCCESS" : "FAILED")}, Test={(testsOk ? "SUCCESS" : "FAILED")}.";

        return new CodingWorkflowResult(
            patchResult.Success && buildOk && testsOk,
            summary,
            plan,
            fullPath,
            repoMap,
            patchResult.Success,
            buildOk,
            testsOk,
            suggestedCommitMessage);
    }
}
