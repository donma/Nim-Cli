using NimCli.Contracts;

namespace NimCli.Provider.Abstractions;

public interface IChatProvider
{
    string ProviderName { get; }
    Task<ChatCompletionResponse> CompleteAsync(ChatCompletionRequest request, CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> StreamAsync(ChatCompletionRequest request, CancellationToken cancellationToken = default);
}

public interface IModelCatalogProvider
{
    Task<List<ModelInfo>> ListModelsAsync(CancellationToken cancellationToken = default);
    Task<bool> ModelExistsAsync(string modelId, CancellationToken cancellationToken = default);
}

public interface IProviderHealthChecker
{
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
    Task<string> GetStatusMessageAsync(CancellationToken cancellationToken = default);
}
