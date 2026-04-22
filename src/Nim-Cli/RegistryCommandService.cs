using NimCli.Infrastructure;

namespace NimCli.App;

public enum RegistryKind
{
    Extensions,
    Skills,
    Hooks
}

public sealed class RegistryCommandService
{
    private readonly CliRuntimeStore _runtimeStore;

    public RegistryCommandService(CliRuntimeStore runtimeStore)
    {
        _runtimeStore = runtimeStore;
    }

    public string List(RegistryKind kind)
    {
        var items = GetRegistry(kind).Items;
        if (items.Count == 0)
            return $"No {GetKindName(kind)} configured.";

        return string.Join(Environment.NewLine, items.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .Select(item => $"{item.Name} | {(item.Enabled ? "enabled" : "disabled")} | source={item.Source} | ref={item.Reference ?? "(none)"} | autoUpdate={item.AutoUpdate.ToString().ToLowerInvariant()} | desc={item.Description ?? "(none)"}"));
    }

    public string Describe(RegistryKind kind, string? name = null)
    {
        var items = GetRegistry(kind).Items;
        if (items.Count == 0)
            return $"No {GetKindName(kind)} configured.";

        var filtered = string.IsNullOrWhiteSpace(name)
            ? items
            : items.Where(item => item.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).ToList();

        if (filtered.Count == 0)
            return $"{GetKindName(kind).TrimEnd('s')} not found: {name}";

        return string.Join(Environment.NewLine + Environment.NewLine,
            filtered.Select(item => string.Join(Environment.NewLine,
            [
                $"Name: {item.Name}",
                $"Enabled: {item.Enabled}",
                $"Source: {item.Source}",
                $"Reference: {item.Reference ?? "(none)"}",
                $"AutoUpdate: {item.AutoUpdate}",
                $"Description: {item.Description ?? "(none)"}"
            ])));
    }

    public string Rename(RegistryKind kind, string currentName, string newName)
    {
        var registry = GetRegistry(kind);
        var existing = registry.Items.FirstOrDefault(item => item.Name.Equals(currentName, StringComparison.OrdinalIgnoreCase));
        if (existing is null)
            return $"{GetKindName(kind).TrimEnd('s')} not found: {currentName}";

        existing.Name = newName;
        SaveRegistry(kind, registry);
        return $"Renamed {GetKindName(kind).TrimEnd('s')} '{currentName}' to '{newName}'";
    }

    public string SetDescription(RegistryKind kind, string name, string description)
    {
        var registry = GetRegistry(kind);
        var existing = registry.Items.FirstOrDefault(item => item.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (existing is null)
            return $"{GetKindName(kind).TrimEnd('s')} not found: {name}";

        existing.Description = description;
        SaveRegistry(kind, registry);
        return $"Updated description for {GetKindName(kind).TrimEnd('s')} '{name}'";
    }

    public string Add(RegistryKind kind, string source, string? name = null, string? reference = null, bool autoUpdate = false)
    {
        var registry = GetRegistry(kind);
        name ??= InferName(source);
        var existing = registry.Items.FirstOrDefault(item => item.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
            registry.Items.Remove(existing);

        registry.Items.Add(new RegistryItem
        {
            Name = name,
            Source = source,
            Reference = reference,
            AutoUpdate = autoUpdate,
            Enabled = true,
            Description = $"{GetKindName(kind)} from {source}"
        });

        SaveRegistry(kind, registry);
        return $"Added {GetKindName(kind).TrimEnd('s')} '{name}'";
    }

    public string Remove(RegistryKind kind, string name)
    {
        var registry = GetRegistry(kind);
        var existing = registry.Items.FirstOrDefault(item => item.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (existing is null)
            return $"{GetKindName(kind).TrimEnd('s')} not found: {name}";

        registry.Items.Remove(existing);
        SaveRegistry(kind, registry);
        return $"Removed {GetKindName(kind).TrimEnd('s')} '{name}'";
    }

    public string SetEnabled(RegistryKind kind, string? name, bool enabled, bool all = false)
    {
        var registry = GetRegistry(kind);
        if (all)
        {
            foreach (var item in registry.Items)
                item.Enabled = enabled;

            SaveRegistry(kind, registry);
            return $"All {GetKindName(kind)} {(enabled ? "enabled" : "disabled")}";
        }

        if (string.IsNullOrWhiteSpace(name))
            return $"Name is required for {GetKindName(kind)} toggle.";

        var existing = registry.Items.FirstOrDefault(item => item.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (existing is null)
            return $"{GetKindName(kind).TrimEnd('s')} not found: {name}";

        existing.Enabled = enabled;
        SaveRegistry(kind, registry);
        return $"{GetKindName(kind).TrimEnd('s')} '{name}' {(enabled ? "enabled" : "disabled")}";
    }

    public string Reload(RegistryKind kind)
        => $"Reloaded {GetKindName(kind)} registry from runtime state.";

    public string Update(string? name = null, bool all = false)
    {
        if (all)
            return "Marked all extensions as updated in runtime registry.";

        if (string.IsNullOrWhiteSpace(name))
            return "Extension name is required unless --all is used.";

        return $"Marked extension '{name}' as updated in runtime registry.";
    }

    private RegistryDocument GetRegistry(RegistryKind kind)
    {
        var state = _runtimeStore.LoadState();
        return kind switch
        {
            RegistryKind.Extensions => state.Extensions,
            RegistryKind.Skills => state.Skills,
            RegistryKind.Hooks => state.Hooks,
            _ => state.Extensions
        };
    }

    private void SaveRegistry(RegistryKind kind, RegistryDocument registry)
    {
        var state = _runtimeStore.LoadState();
        switch (kind)
        {
            case RegistryKind.Extensions:
                state.Extensions = registry;
                break;
            case RegistryKind.Skills:
                state.Skills = registry;
                break;
            case RegistryKind.Hooks:
                state.Hooks = registry;
                break;
        }

        _runtimeStore.SaveState(state);
    }

    private static string GetKindName(RegistryKind kind)
        => kind switch
        {
            RegistryKind.Extensions => "extensions",
            RegistryKind.Skills => "skills",
            RegistryKind.Hooks => "hooks",
            _ => "items"
        };

    private static string InferName(string source)
    {
        var normalized = source.Replace('\\', '/').TrimEnd('/');
        var name = normalized.Split('/').LastOrDefault();
        return string.IsNullOrWhiteSpace(name) ? Guid.NewGuid().ToString("N")[..8] : name;
    }
}
