using System.Text;
using NimCli.Tools.Abstractions;

namespace NimCli.Tools.Shell;

public class BuildProjectTool : ITool
{
    private readonly IShellProvider _shell;

    public BuildProjectTool(IShellProvider shell) => _shell = shell;

    public string Name => "build_project";
    public string Description => "Build a .NET project or solution using dotnet build";
    public RiskLevel RiskLevel => RiskLevel.Low;
    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            project = new { type = "string", description = "Path to .csproj or .sln file (optional)" },
            configuration = new { type = "string", description = "Build configuration: Debug or Release", @enum = new[] { "Debug", "Release" } },
            working_dir = new { type = "string", description = "Working directory (optional)" }
        }
    };

    public async Task<ToolExecuteResult> ExecuteAsync(
        Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var project = input.GetValueOrDefault("project")?.ToString();
        var config = input.GetValueOrDefault("configuration")?.ToString() ?? "Debug";
        var workingDir = input.GetValueOrDefault("working_dir")?.ToString();

        var cmd = project != null
            ? PowerShellCommandBuilder.BuildExternalCommand("dotnet", ["build", project, "-c", config])
            : PowerShellCommandBuilder.BuildExternalCommand("dotnet", ["build", "-c", config]);

        var result = await _shell.ExecuteAsync(cmd, workingDir, timeoutSeconds: 120, cancellationToken);
        var output = result.StandardOutput + (result.StandardError.Length > 0 ? "\n" + result.StandardError : "");

        if (result.TimedOut)
            return new ToolExecuteResult(false, output, "Build timed out");

        return new ToolExecuteResult(result.Success, output,
            result.Success ? null : $"Build failed (exit code {result.ExitCode})");
    }
}

public class RunProjectTool : ITool
{
    private readonly IShellProvider _shell;

    public RunProjectTool(IShellProvider shell) => _shell = shell;

    public string Name => "run_project";
    public string Description => "Run a .NET project using dotnet run";
    public RiskLevel RiskLevel => RiskLevel.Low;
    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            project = new { type = "string", description = "Path to .csproj file (optional)" },
            args = new { type = "string", description = "Arguments to pass to the project" },
            working_dir = new { type = "string", description = "Working directory (optional)" },
            timeout_seconds = new { type = "integer", description = "Timeout in seconds (default: 30)" }
        }
    };

    public async Task<ToolExecuteResult> ExecuteAsync(
        Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var project = input.GetValueOrDefault("project")?.ToString();
        var extraArgs = input.GetValueOrDefault("args")?.ToString() ?? "";
        var workingDir = input.GetValueOrDefault("working_dir")?.ToString();
        var timeout = int.TryParse(input.GetValueOrDefault("timeout_seconds")?.ToString(), out var t) ? t : 30;

        var cmd = ShellCommandComposer.BuildDotNetRunCommand(project, extraArgs);

        var result = await _shell.ExecuteAsync(cmd, workingDir, timeoutSeconds: timeout, cancellationToken);
        var output = result.StandardOutput + (result.StandardError.Length > 0 ? "\n" + result.StandardError : "");

        if (result.TimedOut)
            return new ToolExecuteResult(false, output + "\n[Process timed out and was terminated]", "Run timed out");

        return new ToolExecuteResult(result.Success, output,
            result.Success ? null : $"Run failed (exit code {result.ExitCode})");
    }
}

public class RunShellTool : ITool
{
    private readonly IShellProvider _shell;

    public RunShellTool(IShellProvider shell) => _shell = shell;

    public string Name => "run_shell";
    public string Description => "Execute a raw PowerShell command (advanced/high risk)";
    public RiskLevel RiskLevel => RiskLevel.High;
    public object InputSchema => new
    {
        type = "object",
        required = new[] { "command" },
        properties = new
        {
            command = new { type = "string", description = "The PowerShell command to execute" },
            working_dir = new { type = "string", description = "Working directory (optional)" },
            timeout_seconds = new { type = "integer", description = "Timeout in seconds (default: 60)" }
        }
    };

    public async Task<ToolExecuteResult> ExecuteAsync(
        Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var command = input.GetValueOrDefault("command")?.ToString();
        if (string.IsNullOrWhiteSpace(command))
            return new ToolExecuteResult(false, "", "Command is required");

        var workingDir = input.GetValueOrDefault("working_dir")?.ToString();
        var timeout = int.TryParse(input.GetValueOrDefault("timeout_seconds")?.ToString(), out var t) ? t : 60;

        var result = await _shell.ExecuteAsync(command, workingDir, timeoutSeconds: timeout, cancellationToken);
        var output = result.StandardOutput + (result.StandardError.Length > 0 ? "\nSTDERR:\n" + result.StandardError : "");

        if (result.TimedOut)
            return new ToolExecuteResult(false, output + "\n[Command timed out and was terminated]", "Command timed out");

        return new ToolExecuteResult(result.Success, output,
            result.Success ? null : $"Command failed (exit code {result.ExitCode})");
    }
}

public static class ShellCommandComposer
{
    public static string BuildDotNetRunCommand(string? project, string? extraArgs)
    {
        var arguments = new List<string?> { "run" };
        if (!string.IsNullOrWhiteSpace(project))
        {
            arguments.Add("--project");
            arguments.Add(project);
        }

        if (!string.IsNullOrWhiteSpace(extraArgs))
        {
            arguments.Add("--");
            arguments.AddRange(PowerShellCommandBuilder.TokenizeArguments(extraArgs));
        }

        return PowerShellCommandBuilder.BuildExternalCommand("dotnet", arguments);
    }
}
