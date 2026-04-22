using System.Text.Json;
using NimCli.Tools.Abstractions;

namespace NimCli.Coding;

public class PlanEditTool : ITool
{
    private readonly CodeEditPlanner _planner;

    public PlanEditTool(CodeEditPlanner planner)
    {
        _planner = planner;
    }

    public string Name => "plan_edit";
    public string Description => "Plan candidate files for a coding task before editing";
    public RiskLevel RiskLevel => RiskLevel.Low;
    public object InputSchema => new
    {
        type = "object",
        required = new[] { "task" },
        properties = new
        {
            task = new { type = "string", description = "Requested coding task" },
            directory = new { type = "string", description = "Root directory to inspect" }
        }
    };

    public Task<ToolExecuteResult> ExecuteAsync(Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var task = input.GetValueOrDefault("task")?.ToString();
        if (string.IsNullOrWhiteSpace(task))
            return Task.FromResult(new ToolExecuteResult(false, string.Empty, "task is required"));

        var directory = input.GetValueOrDefault("directory")?.ToString() ?? Directory.GetCurrentDirectory();
        var plan = _planner.Plan(task, directory);
        return Task.FromResult(new ToolExecuteResult(true, JsonSerializer.Serialize(plan, new JsonSerializerOptions { WriteIndented = true })));
    }
}

public class ApplyPatchTool : ITool
{
    private readonly PatchApplier _patchApplier;
    private readonly CodingPipeline _codingPipeline;

    public ApplyPatchTool(PatchApplier patchApplier, CodingPipeline codingPipeline)
    {
        _patchApplier = patchApplier;
        _codingPipeline = codingPipeline;
    }

    public string Name => "apply_patch_edit";
    public string Description => "Apply an exact search and replace patch with backup";
    public RiskLevel RiskLevel => RiskLevel.Medium;
    public object InputSchema => new
    {
        type = "object",
        required = new[] { "file_path", "search", "replace" },
        properties = new
        {
            file_path = new { type = "string", description = "File to update" },
            search = new { type = "string", description = "Exact text to replace" },
            replace = new { type = "string", description = "Replacement text" },
            verify_build = new { type = "boolean", description = "Run dotnet build after patch (default: true)" },
            verify_tests = new { type = "boolean", description = "Run dotnet test after patch (default: false)" }
        }
    };

    public async Task<ToolExecuteResult> ExecuteAsync(Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var filePath = input.GetValueOrDefault("file_path")?.ToString();
        var search = input.GetValueOrDefault("search")?.ToString();
        var replace = input.GetValueOrDefault("replace")?.ToString() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(filePath) || search is null)
            return new ToolExecuteResult(false, string.Empty, "file_path and search are required");

        var result = _patchApplier.ApplyExactReplace(filePath, search, replace);
        if (!result.Success)
            return new ToolExecuteResult(false, result.Summary, result.Error);

        var metadata = new Dictionary<string, object>();
        if (result.BackupPath != null)
            metadata["backup_path"] = result.BackupPath;

        var verifyBuild = !string.Equals(input.GetValueOrDefault("verify_build")?.ToString(), "false", StringComparison.OrdinalIgnoreCase);
        var verifyTests = string.Equals(input.GetValueOrDefault("verify_tests")?.ToString(), "true", StringComparison.OrdinalIgnoreCase);
        var directory = Path.GetDirectoryName(Path.GetFullPath(filePath)) ?? Directory.GetCurrentDirectory();
        var summary = result.Summary;

        if (verifyBuild)
        {
            var buildOk = await _codingPipeline.VerifyBuildAsync(directory, cancellationToken);
            metadata["build_ok"] = buildOk;
            summary += buildOk ? "\nBuild verification: SUCCESS" : "\nBuild verification: FAILED";
        }

        if (verifyTests)
        {
            var testsOk = await _codingPipeline.VerifyTestsAsync(directory, cancellationToken);
            metadata["tests_ok"] = testsOk;
            summary += testsOk ? "\nTest verification: SUCCESS" : "\nTest verification: FAILED";
        }

        return new ToolExecuteResult(true, summary, Metadata: metadata);
    }
}
