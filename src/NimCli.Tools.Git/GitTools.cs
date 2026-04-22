using NimCli.Tools.Abstractions;
using NimCli.Tools.Shell;

namespace NimCli.Tools.Git;

public class GitStatusTool : ITool
{
    private readonly IShellProvider _shell;
    public GitStatusTool(IShellProvider shell) => _shell = shell;

    public string Name => "git_status";
    public string Description => "Show git status of the repository";
    public RiskLevel RiskLevel => RiskLevel.Low;
    public object InputSchema => new
    {
        type = "object",
        properties = new { working_dir = new { type = "string", description = "Repository directory" } }
    };

    public async Task<ToolExecuteResult> ExecuteAsync(
        Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var dir = input.GetValueOrDefault("working_dir")?.ToString();
        var result = await _shell.ExecuteAsync("& git status", dir, cancellationToken: cancellationToken);
        return new ToolExecuteResult(result.Success, result.StandardOutput + result.StandardError);
    }
}

public class GitDiffTool : ITool
{
    private readonly IShellProvider _shell;
    public GitDiffTool(IShellProvider shell) => _shell = shell;

    public string Name => "git_diff";
    public string Description => "Show git diff of uncommitted changes";
    public RiskLevel RiskLevel => RiskLevel.Low;
    public object InputSchema => new
    {
        type = "object",
        properties = new { working_dir = new { type = "string", description = "Repository directory" } }
    };

    public async Task<ToolExecuteResult> ExecuteAsync(
        Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var dir = input.GetValueOrDefault("working_dir")?.ToString();
        var result = await _shell.ExecuteAsync("& git diff", dir, cancellationToken: cancellationToken);
        return new ToolExecuteResult(result.Success, result.StandardOutput + result.StandardError);
    }
}

public class GitCommitTool : ITool
{
    private readonly IShellProvider _shell;
    public GitCommitTool(IShellProvider shell) => _shell = shell;

    public string Name => "git_commit";
    public string Description => "Stage all changes and create a git commit";
    public RiskLevel RiskLevel => RiskLevel.Medium;
    public object InputSchema => new
    {
        type = "object",
        required = new[] { "message" },
        properties = new
        {
            message = new { type = "string", description = "Commit message" },
            working_dir = new { type = "string", description = "Repository directory" }
        }
    };

    public async Task<ToolExecuteResult> ExecuteAsync(
        Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var message = input.GetValueOrDefault("message")?.ToString();
        var dir = input.GetValueOrDefault("working_dir")?.ToString();

        if (string.IsNullOrWhiteSpace(message))
            return new ToolExecuteResult(false, "", "Commit message is required");

        var addResult = await _shell.ExecuteAsync("git add -A", dir, cancellationToken: cancellationToken);
        if (!addResult.Success)
            return new ToolExecuteResult(false, addResult.StandardError, "git add failed");

        var commitCommand = PowerShellCommandBuilder.BuildExternalCommand("git", ["commit", "-m", message]);
        var commitResult = await _shell.ExecuteAsync(commitCommand, dir, cancellationToken: cancellationToken);
        var output = commitResult.StandardOutput + commitResult.StandardError;
        return new ToolExecuteResult(commitResult.Success, output,
            commitResult.Success ? null : "git commit failed");
    }
}

public class GitPushTool : ITool
{
    private readonly IShellProvider _shell;
    public GitPushTool(IShellProvider shell) => _shell = shell;

    public string Name => "git_push";
    public string Description => "Push commits to remote repository (requires approval)";
    public RiskLevel RiskLevel => RiskLevel.High;
    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            remote = new { type = "string", description = "Remote name (default: origin)" },
            branch = new { type = "string", description = "Branch name (default: current branch)" },
            working_dir = new { type = "string", description = "Repository directory" }
        }
    };

    public async Task<ToolExecuteResult> ExecuteAsync(
        Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var remote = input.GetValueOrDefault("remote")?.ToString() ?? "origin";
        var branch = input.GetValueOrDefault("branch")?.ToString() ?? "";
        var dir = input.GetValueOrDefault("working_dir")?.ToString();
        var dryRun = string.Equals(input.GetValueOrDefault("dry_run")?.ToString(), "true", StringComparison.OrdinalIgnoreCase);

        var arguments = new List<string?> { "push" };
        if (dryRun)
            arguments.Add("--dry-run");

        arguments.Add(remote);
        if (!string.IsNullOrWhiteSpace(branch))
            arguments.Add(branch);

        var cmd = PowerShellCommandBuilder.BuildExternalCommand("git", arguments);

        var result = await _shell.ExecuteAsync(cmd, dir, timeoutSeconds: 60, cancellationToken: cancellationToken);
        var output = result.StandardOutput + result.StandardError;
        if (dryRun)
            output = string.IsNullOrWhiteSpace(output)
                ? $"git push dry-run OK: remote={remote}, branch={(string.IsNullOrWhiteSpace(branch) ? "(current)" : branch)}"
                : output;

        return new ToolExecuteResult(result.Success, output,
            result.Success ? null : "git push failed");
    }
}
