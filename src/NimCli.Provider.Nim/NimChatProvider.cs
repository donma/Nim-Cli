using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using NimCli.Contracts;
using NimCli.Infrastructure.Config;
using NimCli.Provider.Abstractions;

namespace NimCli.Provider.Nim;

public class NimProviderOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://integrate.api.nvidia.com/v1";
    public string Model { get; set; } = "meta/llama-3.1-70b-instruct";
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 4096;
    public int TimeoutSeconds { get; set; } = 120;
}

public class NimChatProvider : IChatProvider, IModelCatalogProvider, IProviderHealthChecker
{
    private readonly HttpClient _http;
    private readonly NimProviderOptions _options;
    private static readonly JsonSerializerOptions _json = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    public string ProviderName => "NVIDIA NIM";

    public NimChatProvider(NimProviderOptions options)
    {
        _options = options;
        _http = new HttpClient
        {
            BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds)
        };
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", options.ApiKey);
        _http.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<ChatCompletionResponse> CompleteAsync(
        ChatCompletionRequest request, CancellationToken cancellationToken = default)
    {
        var body = BuildRequestBody(request, stream: false);
        var content = new StringContent(JsonSerializer.Serialize(body, _json), Encoding.UTF8, "application/json");
        var response = await _http.PostAsync("chat/completions", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var raw = JsonSerializer.Deserialize<NimChatCompletionRaw>(json, _json)
            ?? throw new InvalidOperationException("Null response from NIM");

        return MapResponse(raw);
    }

    public async IAsyncEnumerable<string> StreamAsync(
        ChatCompletionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var body = BuildRequestBody(request, stream: true);
        var content = new StringContent(JsonSerializer.Serialize(body, _json), Encoding.UTF8, "application/json");
        var httpReq = new HttpRequestMessage(HttpMethod.Post, "chat/completions") { Content = content };
        var response = await _http.SendAsync(httpReq, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line == null)
                break;

            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: ")) continue;
            var data = line["data: ".Length..];
            if (data == "[DONE]") break;

            NimStreamChunk? chunk = null;
            try { chunk = JsonSerializer.Deserialize<NimStreamChunk>(data, _json); } catch { }
            var delta = chunk?.Choices?.FirstOrDefault()?.Delta?.Content;
            if (!string.IsNullOrEmpty(delta)) yield return delta;
        }
    }

    public async Task<List<ModelInfo>> ListModelsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _http.GetAsync("models", cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var raw = JsonSerializer.Deserialize<NimModelsRaw>(json, _json);
        return raw?.Data?.Select(m => new ModelInfo(m.Id, m.OwnedBy ?? "nvidia")).ToList() ?? [];
    }

    public async Task<bool> ModelExistsAsync(string modelId, CancellationToken cancellationToken = default)
    {
        var models = await ListModelsAsync(cancellationToken);
        return models.Any(m => m.Id == modelId);
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var models = await ListModelsAsync(cancellationToken);
            return models.Count > 0;
        }
        catch { return false; }
    }

    public async Task<string> GetStatusMessageAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var models = await ListModelsAsync(cancellationToken);
            return $"Connected to NIM. {models.Count} model(s) available.";
        }
        catch (Exception ex)
        {
            return $"NIM connection failed: {ex.Message}";
        }
    }

    private object BuildRequestBody(ChatCompletionRequest request, bool stream)
    {
        var messages = request.Messages.Select(m => new { role = m.Role, content = m.Content }).ToList();
        var body = new Dictionary<string, object>
        {
            ["model"] = request.Model,
            ["messages"] = messages,
            ["temperature"] = request.Temperature,
            ["max_tokens"] = request.MaxTokens,
            ["stream"] = stream
        };

        if (request.Tools != null && request.Tools.Count > 0)
        {
            body["tools"] = request.Tools.Select(t => new
            {
                type = "function",
                function = new { name = t.Name, description = t.Description, parameters = t.InputSchema }
            }).ToList();
            body["tool_choice"] = "auto";
        }

        return body;
    }

    private static ChatCompletionResponse MapResponse(NimChatCompletionRaw raw)
    {
        var choice = raw.Choices?.FirstOrDefault();
        var message = choice?.Message;
        List<ToolCallRequest>? toolCalls = null;

        if (message?.ToolCalls != null)
        {
            toolCalls = message.ToolCalls.Select(tc => new ToolCallRequest(
                tc.Id ?? Guid.NewGuid().ToString(),
                tc.Function?.Name ?? "",
                tc.Function?.Arguments ?? "{}"
            )).ToList();
        }

        return new ChatCompletionResponse(
            Id: raw.Id ?? "",
            Model: raw.Model ?? "",
            Content: message?.Content,
            ToolCalls: toolCalls,
            FinishReason: choice?.FinishReason ?? "stop",
            PromptTokens: raw.Usage?.PromptTokens ?? 0,
            CompletionTokens: raw.Usage?.CompletionTokens ?? 0
        );
    }

    // Raw JSON models
    private class NimChatCompletionRaw
    {
        public string? Id { get; set; }
        public string? Model { get; set; }
        public List<NimChoice>? Choices { get; set; }
        public NimUsage? Usage { get; set; }
    }

    private class NimChoice
    {
        public NimMessage? Message { get; set; }
        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; }
    }

    private class NimMessage
    {
        public string? Content { get; set; }
        [JsonPropertyName("tool_calls")]
        public List<NimToolCall>? ToolCalls { get; set; }
    }

    private class NimToolCall
    {
        public string? Id { get; set; }
        public NimFunction? Function { get; set; }
    }

    private class NimFunction
    {
        public string? Name { get; set; }
        public string? Arguments { get; set; }
    }

    private class NimUsage
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }
        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }
    }

    private class NimModelsRaw
    {
        public List<NimModelEntry>? Data { get; set; }
    }

    private class NimModelEntry
    {
        public string Id { get; set; } = "";
        [JsonPropertyName("owned_by")]
        public string? OwnedBy { get; set; }
    }

    private class NimStreamChunk
    {
        public List<NimStreamChoice>? Choices { get; set; }
    }

    private class NimStreamChoice
    {
        public NimStreamDelta? Delta { get; set; }
    }

    private class NimStreamDelta
    {
        public string? Content { get; set; }
    }
}
