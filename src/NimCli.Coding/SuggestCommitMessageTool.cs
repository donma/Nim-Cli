using NimCli.Tools.Abstractions;
using NimCli.Tools.Shell;

namespace NimCli.Coding;

public class SuggestCommitMessageTool : ITool
{
    private readonly IShellProvider _shell;

    public SuggestCommitMessageTool(IShellProvider shell)
    {
        _shell = shell;
    }

    public string Name => "suggest_commit_message";
    public string Description => "Suggest a concise git commit message from current repository changes";
    public RiskLevel RiskLevel => RiskLevel.Low;
    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            working_dir = new { type = "string", description = "Repository directory" }
        }
    };

    public async Task<ToolExecuteResult> ExecuteAsync(Dictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var dir = input.GetValueOrDefault("working_dir")?.ToString();
        var status = await _shell.ExecuteAsync("git status --short", dir, cancellationToken: cancellationToken);
        if (!status.Success)
            return new ToolExecuteResult(false, status.StandardOutput + status.StandardError, "git status failed");

        var diff = await _shell.ExecuteAsync("git diff --stat", dir, cancellationToken: cancellationToken);
        var raw = string.Join("\n", new[] { status.StandardOutput, diff.StandardOutput }
            .Where(static value => !string.IsNullOrWhiteSpace(value))).Trim();

        if (string.IsNullOrWhiteSpace(raw))
            return new ToolExecuteResult(true, "No changes detected. Nothing to commit.");

        var firstLine = raw.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(static line => line.Trim())
            .FirstOrDefault();

        var message = string.IsNullOrWhiteSpace(firstLine)
            ? "update project files"
            : $"update {Sanitize(firstLine).ToLowerInvariant()}";

        return new ToolExecuteResult(true, message, Metadata: new Dictionary<string, object>
        {
            ["status"] = raw
        });
    }

    private static string Sanitize(string line)
        => line.Replace("M ", string.Empty)
            .Replace("A ", string.Empty)
            .Replace("D ", string.Empty)
            .Replace("?? ", string.Empty)
            .Replace("|", " ")
            .Trim();
}
