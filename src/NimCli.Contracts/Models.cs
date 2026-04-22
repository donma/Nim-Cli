namespace NimCli.Contracts;

public record ChatMessage(string Role, string Content);

public record ToolCallRequest(string Id, string Name, string ArgumentsJson);

public record ToolCallResult(string Id, string Name, string ResultJson, bool IsError = false);

public record ApprovalRequest(
    string ToolName,
    string RiskLevel,
    string InputSummary,
    bool DryRun,
    string Reason,
    string Prompt);

public record PolicyAuditEntry(
    string ToolName,
    string Decision,
    string RiskLevel,
    bool DryRun,
    string Reason,
    string InputSummary);

public record ExecutionArtifact(string Kind, string Value);

public record ExecutionWarning(string Source, string Message);

public record ExecutionSummary(
    bool Success,
    string FinalMessage,
    List<string>? ToolsUsed = null,
    List<string>? OutputSummaries = null,
    List<ExecutionWarning>? Warnings = null,
    List<ExecutionArtifact>? Artifacts = null,
    List<string>? ApprovalActions = null,
    List<PolicyAuditEntry>? PolicyDecisions = null,
    List<string>? ToolResultSummaries = null,
    long ElapsedMilliseconds = 0
);

public record SessionSummary(
    string SessionId,
    string WorkspaceKey,
    string WorkingDirectory,
    string Mode,
    int MessageCount,
    int ToolExecutionCount,
    IReadOnlyList<string> WorkspaceDirectories,
    string? LastShellCommand,
    string? LastShellOutput,
    string? LastDbQuery,
    string? LastDbResult,
    string? LastWebUrl,
    string? LastWebContent,
    string? LastBuildSummary,
    string? LastTestSummary,
    string? LastScreenshotPath,
    string? LastSuggestedCommitMessage,
    bool HasDebugRequest,
    string? CurrentTask,
    string? LastContextStrategy,
    IReadOnlyList<string>? RecentActions,
    IReadOnlyList<PolicyAuditEntry>? PolicyAuditTrail
);

public record ToolPolicySummary(
    string ToolName,
    string Description,
    string RiskLevel,
    string Decision,
    bool DryRun = false,
    string? Reason = null
);

public record ChatCompletionRequest(
    string Model,
    List<ChatMessage> Messages,
    List<ToolDefinition>? Tools = null,
    double Temperature = 0.7,
    int MaxTokens = 4096,
    bool Stream = false
);

public record ChatCompletionResponse(
    string Id,
    string Model,
    string? Content,
    List<ToolCallRequest>? ToolCalls,
    string FinishReason,
    int PromptTokens,
    int CompletionTokens
);

public record ToolDefinition(
    string Name,
    string Description,
    object InputSchema
);

public record ModelInfo(string Id, string OwnedBy, string? Description = null);

public record AgentResponse(
    string Content,
    bool RequiresApproval = false,
    string? ApprovalPrompt = null,
    ApprovalRequest? ApprovalRequest = null,
    List<ToolCallResult>? ToolResults = null,
    ExecutionSummary? Summary = null
);
