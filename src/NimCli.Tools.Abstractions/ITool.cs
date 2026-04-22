namespace NimCli.Tools.Abstractions;

public enum RiskLevel
{
    Low,
    Medium,
    High,
    Critical
}

public record ToolExecuteResult(
    bool Success,
    string Output,
    string? ErrorMessage = null,
    Dictionary<string, object>? Metadata = null
);

public interface ITool
{
    string Name { get; }
    string Description { get; }
    RiskLevel RiskLevel { get; }
    object InputSchema { get; }
    Task<ToolExecuteResult> ExecuteAsync(Dictionary<string, object?> input, CancellationToken cancellationToken = default);
}
