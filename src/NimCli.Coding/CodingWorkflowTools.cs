using System.Text;
using NimCli.Tools.Abstractions;
using NimCli.Tools.Shell;

namespace NimCli.Coding;

public class RepoMapTool : ITool
{
    private readonly RepoMapBuilder _repoMapBuilder;

    public RepoMapTool(RepoMapBuilder repoMapBuilder)
    {
        _repoMapBuilder = repoMapBuilder;
    }

    public string Name => "repo_map";
    public string Description => "Build a lightweight repository map for a project or solution";
    public RiskLevel RiskLevel => RiskLevel.Low;
    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            directory = new { type = "string", description = "Root directory to scan" },
            max_files = new { type = "integer", description = "Maximum files to scan (default: 200)" }
        }
    };

    public Task<ToolExecuteResult> ExecuteAsync(Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var directory = input.GetValueOrDefault("directory")?.ToString() ?? Directory.GetCurrentDirectory();
        var maxFiles = int.TryParse(input.GetValueOrDefault("max_files")?.ToString(), out var parsed) ? parsed : 200;
        var map = _repoMapBuilder.BuildMap(directory, maxFiles);
        return Task.FromResult(new ToolExecuteResult(true, map));
    }
}

public class TestProjectTool : ITool
{
    private readonly IShellProvider _shell;

    public TestProjectTool(IShellProvider shell)
    {
        _shell = shell;
    }

    public string Name => "test_project";
    public string Description => "Run dotnet test for a project or solution";
    public RiskLevel RiskLevel => RiskLevel.Low;
    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            project = new { type = "string", description = "Path to .csproj or .sln file (optional)" },
            working_dir = new { type = "string", description = "Working directory (optional)" },
            timeout_seconds = new { type = "integer", description = "Timeout in seconds (default: 180)" }
        }
    };

    public async Task<ToolExecuteResult> ExecuteAsync(Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var project = input.GetValueOrDefault("project")?.ToString();
        var workingDir = input.GetValueOrDefault("working_dir")?.ToString();
        var timeout = int.TryParse(input.GetValueOrDefault("timeout_seconds")?.ToString(), out var parsed) ? parsed : 180;

        var cmd = string.IsNullOrWhiteSpace(project)
            ? "dotnet test"
            : $"dotnet test \"{project}\"";

        var result = await _shell.ExecuteAsync(cmd, workingDir, timeout, cancellationToken);
        var output = result.StandardOutput + (string.IsNullOrWhiteSpace(result.StandardError) ? string.Empty : "\n" + result.StandardError);
        return new ToolExecuteResult(result.Success, output, result.Success ? null : $"Tests failed (exit code {result.ExitCode})");
    }
}

public class LintProjectTool : ITool
{
    private readonly IShellProvider _shell;

    public LintProjectTool(IShellProvider shell)
    {
        _shell = shell;
    }

    public string Name => "lint_project";
    public string Description => "Run dotnet format verification for a project or solution";
    public RiskLevel RiskLevel => RiskLevel.Low;
    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            project = new { type = "string", description = "Path to .csproj or .sln file (optional)" },
            working_dir = new { type = "string", description = "Working directory (optional)" },
            timeout_seconds = new { type = "integer", description = "Timeout in seconds (default: 180)" }
        }
    };

    public async Task<ToolExecuteResult> ExecuteAsync(Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var project = input.GetValueOrDefault("project")?.ToString();
        var workingDir = input.GetValueOrDefault("working_dir")?.ToString();
        var timeout = int.TryParse(input.GetValueOrDefault("timeout_seconds")?.ToString(), out var parsed) ? parsed : 180;

        var cmd = string.IsNullOrWhiteSpace(project)
            ? "dotnet format --verify-no-changes"
            : $"dotnet format \"{project}\" --verify-no-changes";

        var result = await _shell.ExecuteAsync(cmd, workingDir, timeout, cancellationToken);
        var output = result.StandardOutput + (string.IsNullOrWhiteSpace(result.StandardError) ? string.Empty : "\n" + result.StandardError);
        return new ToolExecuteResult(result.Success, output, result.Success ? null : $"Lint failed (exit code {result.ExitCode})");
    }
}

public class EditFilesTool : ITool
{
    private readonly PatchApplier _patchApplier;

    public EditFilesTool(PatchApplier patchApplier)
    {
        _patchApplier = patchApplier;
    }

    public string Name => "edit_files";
    public string Description => "Apply a structured search and replace edit to a file";
    public RiskLevel RiskLevel => RiskLevel.Medium;
    public object InputSchema => new
    {
        type = "object",
        required = new[] { "file_path", "search", "replace" },
        properties = new
        {
            file_path = new { type = "string", description = "Target file path" },
            search = new { type = "string", description = "Exact text to replace" },
            replace = new { type = "string", description = "Replacement text" },
            create_if_missing = new { type = "boolean", description = "Create the file if it does not exist" }
        }
    };

    public Task<ToolExecuteResult> ExecuteAsync(Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var filePath = input.GetValueOrDefault("file_path")?.ToString();
        var search = input.GetValueOrDefault("search")?.ToString();
        var replace = input.GetValueOrDefault("replace")?.ToString() ?? string.Empty;
        var createIfMissing = string.Equals(input.GetValueOrDefault("create_if_missing")?.ToString(), "true", StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(filePath) || search is null)
            return Task.FromResult(new ToolExecuteResult(false, string.Empty, "file_path and search are required"));

        var fullPath = Path.GetFullPath(filePath);
        if (!File.Exists(fullPath))
        {
            if (!createIfMissing)
                return Task.FromResult(new ToolExecuteResult(false, string.Empty, $"File not found: {fullPath}"));

            Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? Directory.GetCurrentDirectory());
            File.WriteAllText(fullPath, replace, Encoding.UTF8);
            return Task.FromResult(new ToolExecuteResult(true, $"Created file: {fullPath}"));
        }

        var result = _patchApplier.ApplyExactReplace(fullPath, search, replace);
        return Task.FromResult(new ToolExecuteResult(result.Success, result.Summary, result.Error,
            result.BackupPath == null ? null : new Dictionary<string, object> { ["backup_path"] = result.BackupPath }));
    }
}
