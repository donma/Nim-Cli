using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NimCli.Contracts;
using NimCli.Infrastructure.Config;
using NimCli.Tools.Abstractions;

namespace NimCli.Core;

public class AgentOrchestrator
{
    private readonly ProviderRouter _provider;
    private readonly ToolRegistry _toolRegistry;
    private readonly ToolPolicyService _policyService;
    private readonly PromptBuilder _promptBuilder;
    private readonly ContextBuilder _contextBuilder;
    private readonly CommandIntentResolver _intentResolver;
    private readonly SessionState _session;
    private readonly NimCliOptions _options;
    private readonly ILogger<AgentOrchestrator> _logger;

    // Approval callback: returns true if user approves
    public Func<string, Task<bool>>? ApprovalCallback { get; set; }
    public Func<ApprovalRequest, Task<bool>>? ApprovalRequestCallback { get; set; }
    // Output callback for streaming text
    public Action<string>? OnChunk { get; set; }

    public AgentOrchestrator(
        ProviderRouter provider,
        ToolRegistry toolRegistry,
        ToolPolicyService policyService,
        PromptBuilder promptBuilder,
        ContextBuilder contextBuilder,
        CommandIntentResolver intentResolver,
        SessionState session,
        NimCliOptions options,
        ILogger<AgentOrchestrator> logger)
    {
        _provider = provider;
        _toolRegistry = toolRegistry;
        _policyService = policyService;
        _promptBuilder = promptBuilder;
        _contextBuilder = contextBuilder;
        _intentResolver = intentResolver;
        _session = session;
        _options = options;
        _logger = logger;
    }

    public async Task<AgentResponse> RunAsync(string userInput, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Agent run started. Input: {Input}", userInput[..Math.Min(80, userInput.Length)]);

        // Pre-LLM: resolve intent
        var intent = _intentResolver.Resolve(userInput);
        _logger.LogDebug("Resolved intent: {Intent}", intent.Type);

        _session.Mode = intent.Type switch
        {
            IntentType.AnalyzeProject => AgentMode.Analysis,
            IntentType.EditFiles or IntentType.BuildProject or IntentType.RunProject => AgentMode.Coding,
            IntentType.ScreenshotPage or IntentType.QueryDb or IntentType.GitPush or IntentType.GitCommit or IntentType.GitStatus or IntentType.UploadFtp or IntentType.WebFetch or IntentType.WebSearch => AgentMode.Ops,
            _ => _session.Mode
        };

        _session.RecordCurrentTask(userInput);
        _session.AddRecentAction($"input:{TrimForDisplay(userInput, 80)}");

        // Build context
        _contextBuilder.Clear();
        _contextBuilder.AddSessionState(_session, _session.Mode);
        _session.RecordContextStrategy(_contextBuilder.LastStrategy);

        // Build messages
        var messages = _promptBuilder.BuildMessages(_session, userInput, _contextBuilder.Build());

        // Get tool definitions
        var toolDefs = _toolRegistry.GetToolDefinitions();

        var model = _options.Provider.DefaultModel;
        var request = new ChatCompletionRequest(model, messages, toolDefs.Count > 0 ? toolDefs : null);
        _session.RecordDebugRequest(JsonSerializer.Serialize(request, new JsonSerializerOptions { WriteIndented = true }));

        // Call provider
        ChatCompletionResponse response;
        try
        {
            response = await ExecuteWithRetryAsync(
                ct => _provider.ChatProvider.CompleteAsync(request, ct),
                "initial provider call",
                cancellationToken);
            _logger.LogDebug("Provider response. Tokens: {Prompt}+{Completion}, FinishReason: {Reason}",
                response.PromptTokens, response.CompletionTokens, response.FinishReason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Provider call failed");
            return new AgentResponse($"Error calling NIM provider: {ex.Message}");
        }

        // Post-LLM: handle tool calls or direct answer
        var toolResults = new List<ToolCallResult>();
        var sw = System.Diagnostics.Stopwatch.StartNew();

        if (response.ToolCalls != null && response.ToolCalls.Count > 0)
        {
            foreach (var toolCall in response.ToolCalls)
            {
                var result = await ExecuteToolCallAsync(toolCall, cancellationToken);
                toolResults.Add(result);
                _session.AddToolResultMessage(toolCall.Name, result.ResultJson);
                _session.AddRecentAction($"tool:{toolCall.Name}");
            }

            // Feed tool results back for final answer
            var followUpMessages = _promptBuilder.BuildMessages(_session, userInput, _contextBuilder.Build());
            followUpMessages.Add(new ChatMessage("assistant",
                response.Content ?? string.Join("\n", response.ToolCalls.Select(tc => $"Called {tc.Name}"))));

            foreach (var tr in toolResults)
            {
                followUpMessages.Add(new ChatMessage("tool", tr.ResultJson));
            }

            var followUpRequest = new ChatCompletionRequest(model, followUpMessages);
            _session.RecordDebugRequest(JsonSerializer.Serialize(followUpRequest, new JsonSerializerOptions { WriteIndented = true }));
            try
            {
                var finalAnswer = await GetFinalAnswerAsync(followUpRequest, cancellationToken);
                _session.AddUserMessage(userInput);
                _session.AddAssistantMessage(finalAnswer);
                sw.Stop();
                return new AgentResponse(finalAnswer, ToolResults: toolResults,
                    Summary: BuildExecutionSummary(finalAnswer, toolResults, sw.ElapsedMilliseconds));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Follow-up provider call failed");
                sw.Stop();
                return new AgentResponse(
                    $"Tools executed. Summary:\n{string.Join("\n", toolResults.Select(r => r.ResultJson))}",
                    ToolResults: toolResults,
                    Summary: BuildExecutionSummary($"Tools executed. Summary:\n{string.Join("\n", toolResults.Select(r => r.ResultJson))}", toolResults, sw.ElapsedMilliseconds));
            }
        }

        var content = response.Content ?? "";
        _session.AddUserMessage(userInput);
        _session.AddAssistantMessage(content);
        sw.Stop();
        return new AgentResponse(content, Summary: BuildExecutionSummary(content, toolResults, sw.ElapsedMilliseconds));
    }

    private async Task<string> GetFinalAnswerAsync(ChatCompletionRequest request, CancellationToken cancellationToken)
    {
        if (_options.Provider.Streaming && OnChunk != null)
        {
            var builder = new System.Text.StringBuilder();
            await foreach (var chunk in _provider.ChatProvider.StreamAsync(request with { Stream = true }, cancellationToken))
            {
                builder.Append(chunk);
                OnChunk(chunk);
            }

            return builder.ToString();
        }

        var followUp = await ExecuteWithRetryAsync(
            ct => _provider.ChatProvider.CompleteAsync(request, ct),
            "follow-up provider call",
            cancellationToken);
        return followUp.Content ?? "";
    }

    private async Task<T> ExecuteWithRetryAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        string operationName,
        CancellationToken cancellationToken)
    {
        var maxAttempts = Math.Max(1, _options.Retry.MaxAttempts);
        var delay = Math.Max(0, _options.Retry.DelayMilliseconds);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return await operation(cancellationToken);
            }
            catch (Exception ex) when (attempt < maxAttempts && !cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "{Operation} failed on attempt {Attempt}/{MaxAttempts}", operationName, attempt, maxAttempts);
                if (delay > 0)
                    await Task.Delay(delay, cancellationToken);
            }
        }

        return await operation(cancellationToken);
    }
    private async Task<ToolCallResult> ExecuteToolCallAsync(
        ToolCallRequest toolCall, CancellationToken cancellationToken)
    {
        var tool = _toolRegistry.Get(toolCall.Name);
        if (tool == null)
        {
            _logger.LogWarning("Unknown tool: {Tool}", toolCall.Name);
            return new ToolCallResult(toolCall.Id, toolCall.Name,
                $"{{\"error\": \"Tool '{toolCall.Name}' not found\"}}", IsError: true);
        }

        // Approval check
        var input = ParseInput(toolCall.ArgumentsJson);
        var decision = _policyService.EvaluateDetailed(tool, input);
        _session.AddPolicyAudit(_policyService.BuildAuditEntry(tool, decision, input));

        if (decision.Decision == ApprovalDecision.Deny)
        {
            return new ToolCallResult(toolCall.Id, toolCall.Name,
                JsonSerializer.Serialize(new { success = false, error = $"Policy denied '{tool.Name}': {decision.Reason}" }), IsError: true);
        }

        if (decision.Decision == ApprovalDecision.Ask)
        {
            var approvalRequest = new ApprovalRequest(
                tool.Name,
                decision.RiskLevel.ToString(),
                _policyService.BuildInputSummary(input),
                decision.DryRun,
                decision.Reason,
                $"Allow '{tool.Name}' ({tool.RiskLevel} risk)?");

            var approved = ApprovalRequestCallback != null
                ? await ApprovalRequestCallback(approvalRequest)
                : ApprovalCallback != null
                    ? await ApprovalCallback($"{approvalRequest.Prompt} [y/N] ")
                    : false;

            if (!approved)
            {
                return new ToolCallResult(toolCall.Id, toolCall.Name,
                    $"{{\"error\": \"User denied execution of '{tool.Name}'\"}}", IsError: true);
            }
        }

        if (decision.DryRun)
            input["dry_run"] = true;

        // Execute
        _logger.LogInformation("Executing tool: {Tool}", tool.Name);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var result = await tool.ExecuteAsync(input, cancellationToken);
            sw.Stop();
            _logger.LogInformation("Tool {Tool} completed in {Ms}ms. Success: {Ok}",
                tool.Name, sw.ElapsedMilliseconds, result.Success);

            var resultJson = JsonSerializer.Serialize(new
            {
                success = result.Success,
                output = result.Output,
                error = result.ErrorMessage
            });

            UpdateSessionContext(tool.Name, input, result);
            return new ToolCallResult(toolCall.Id, toolCall.Name, resultJson, IsError: !result.Success);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Tool {Tool} threw exception", tool.Name);
            var errorJson = JsonSerializer.Serialize(new { success = false, error = ex.Message });
            return new ToolCallResult(toolCall.Id, toolCall.Name, errorJson, IsError: true);
        }
    }

    private static Dictionary<string, object?> ParseInput(string argumentsJson)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object?>>(argumentsJson)
                ?? new Dictionary<string, object?>();
        }
        catch
        {
            return new Dictionary<string, object?>();
        }
    }

    private void UpdateSessionContext(string toolName, Dictionary<string, object?> input, ToolExecuteResult result)
    {
        if (!result.Success)
            return;

        switch (toolName)
        {
            case "analyze_project":
                _session.RecordRepoMap(result.Output);
                _session.RecordSuggestedCommitMessage(SuggestCommitMessage());
                break;
            case "build_project":
            case "run_project":
            case "run_shell":
            case "git_status":
            case "git_diff":
            case "suggest_commit_message":
                _session.RecordShellResult(BuildShellContextLabel(toolName, input), result.Output);
                _session.RecordSuggestedCommitMessage(result.Output);
                if (toolName == "build_project")
                    _session.RecordBuildSummary(SummarizeOutput(result.Output));
                break;
            case "web_fetch":
            case "web_search":
            case "open_page":
            case "screenshot_page":
                _session.RecordWebResult(input.GetValueOrDefault("url")?.ToString() ?? toolName, result.Output);
                if (toolName == "screenshot_page")
                    _session.RecordScreenshotPath(ExtractPath(result.Output));
                break;
            case "query_db":
                _session.RecordDbResult(BuildDbContextLabel(input), result.Output);
                break;
            case "test_project":
                _session.RecordTestSummary(SummarizeOutput(result.Output));
                break;
        }
    }

    private static string SummarizeOutput(string output)
        => output.Replace("\r", " ").Replace("\n", " ").Trim()[..Math.Min(160, output.Replace("\r", " ").Replace("\n", " ").Trim().Length)];

    private static string ExtractPath(string output)
    {
        const string prefix = "Screenshot saved to:";
        var line = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(static value => value.Contains(prefix, StringComparison.OrdinalIgnoreCase));
        return line is null ? output : line[(line.IndexOf(prefix, StringComparison.OrdinalIgnoreCase) + prefix.Length)..].Trim();
    }

    private string SuggestCommitMessage()
    {
        var recentTools = _session.ToolExecutionHistory.TakeLast(3)
            .Select(static entry => entry.Split(']').FirstOrDefault()?.Split('[').LastOrDefault())
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (recentTools.Count == 0)
            return "update verified project behavior";

        return $"update project workflow for {string.Join(", ", recentTools)}";
    }

    private static string TrimForDisplay(string text, int maxLength)
    {
        var normalized = text.Replace("\r", " ").Replace("\n", " ").Trim();
        if (normalized.Length <= maxLength)
            return normalized;

        return normalized[..Math.Max(0, maxLength - 3)] + "...";
    }

    private static string BuildShellContextLabel(string toolName, IReadOnlyDictionary<string, object?> input)
    {
        return toolName switch
        {
            "build_project" => $"build_project project={input.GetValueOrDefault("project")?.ToString() ?? "(default)"} config={input.GetValueOrDefault("configuration")?.ToString() ?? "Debug"}",
            "run_project" => $"run_project project={input.GetValueOrDefault("project")?.ToString() ?? "(default)"} args={TrimForDisplay(input.GetValueOrDefault("args")?.ToString() ?? string.Empty, 80)}",
            "run_shell" => $"run_shell raw={TrimForDisplay(input.GetValueOrDefault("command")?.ToString() ?? string.Empty, 100)}",
            _ => toolName
        };
    }

    private static string BuildDbContextLabel(IReadOnlyDictionary<string, object?> input)
    {
        var rawMode = string.Equals(input.GetValueOrDefault("raw_mode")?.ToString(), "true", StringComparison.OrdinalIgnoreCase);
        if (rawMode)
            return $"raw:{TrimForDisplay(input.GetValueOrDefault("query")?.ToString() ?? string.Empty, 120)}";

        var table = input.GetValueOrDefault("table")?.ToString() ?? "(unknown)";
        var where = TrimForDisplay(input.GetValueOrDefault("where")?.ToString() ?? string.Empty, 80);
        var top = input.GetValueOrDefault("top_n")?.ToString() ?? "50";
        return string.IsNullOrWhiteSpace(where)
            ? $"structured:{table} top={top}"
            : $"structured:{table} where={where} top={top}";
    }

    private static ExecutionSummary BuildExecutionSummary(string finalMessage, List<ToolCallResult> toolResults, long elapsedMilliseconds)
    {
        var toolsUsed = toolResults
            .Select(result => result.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var warnings = toolResults
            .Where(result => result.IsError)
            .Select(result => new ExecutionWarning(result.Name, result.ResultJson))
            .ToList();

        return new ExecutionSummary(
            Success: warnings.Count == 0,
            FinalMessage: finalMessage,
            ToolsUsed: toolsUsed,
            OutputSummaries: [],
            Warnings: warnings,
            Artifacts: [],
            ApprovalActions: [],
            PolicyDecisions: [],
            ToolResultSummaries: toolResults.TakeLast(6).Select(result => $"{result.Name}: {TrimForDisplay(result.ResultJson, 140)}").ToList(),
            ElapsedMilliseconds: elapsedMilliseconds);
    }
}
