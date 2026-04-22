using NimCli.Provider.Abstractions;

namespace NimCli.Core;

public class ProviderRouter
{
    private IChatProvider? _chatProvider;
    private IModelCatalogProvider? _catalogProvider;
    private IProviderHealthChecker? _healthChecker;

    public void Register(IChatProvider chat, IModelCatalogProvider catalog, IProviderHealthChecker health)
    {
        _chatProvider = chat;
        _catalogProvider = catalog;
        _healthChecker = health;
    }

    public IChatProvider ChatProvider
        => _chatProvider ?? throw new InvalidOperationException("No chat provider registered.");

    public IModelCatalogProvider CatalogProvider
        => _catalogProvider ?? throw new InvalidOperationException("No catalog provider registered.");

    public IProviderHealthChecker HealthChecker
        => _healthChecker ?? throw new InvalidOperationException("No health checker registered.");
}
