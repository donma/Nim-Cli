using NimCli.Contracts;
using NimCli.Tools.Abstractions;

namespace NimCli.Core;

public class ToolRegistry
{
    private readonly Dictionary<string, ITool> _tools = new(StringComparer.OrdinalIgnoreCase);

    public void Register(ITool tool)
        => _tools[tool.Name] = tool;

    public void RegisterAll(IEnumerable<ITool> tools)
    {
        foreach (var t in tools) Register(t);
    }

    public ITool? Get(string name)
        => _tools.TryGetValue(name, out var t) ? t : null;

    public bool Exists(string name) => _tools.ContainsKey(name);

    public IReadOnlyList<ITool> GetAll() => _tools.Values.ToList();

    public List<ToolDefinition> GetToolDefinitions()
        => _tools.Values.Select(t => new ToolDefinition(t.Name, t.Description, t.InputSchema)).ToList();
}
