using System.Text;
using System.Text.Json;
using NimCli.Core;
using NimCli.Infrastructure;
using NimCli.Tools.Shell;

namespace NimCli.App;

public sealed class WorkspaceCommandService
{
    private readonly CliRuntimeStore _runtimeStore;
    private readonly IShellProvider _shellProvider;

    public WorkspaceCommandService(CliRuntimeStore runtimeStore, IShellProvider shellProvider)
    {
        _runtimeStore = runtimeStore;
        _shellProvider = shellProvider;
    }

    public IReadOnlyList<string> FindMemoryFiles(string rootDirectory)
        => Directory.GetFiles(rootDirectory, "Nim.md", SearchOption.AllDirectories)
            .Where(path => !path.Contains("\\bin\\", StringComparison.OrdinalIgnoreCase) &&
                           !path.Contains("\\obj\\", StringComparison.OrdinalIgnoreCase))
            .ToList();

    public string InitializeMemoryFile(string workingDirectory)
    {
        var path = Path.Combine(workingDirectory, "Nim.md");
        if (File.Exists(path))
            return $"Nim.md already exists: {path}";

        var content = """
            # Nim Project Context

            ## Purpose
            Describe this project's purpose, runtime environment, and deployment model.

            ## Architecture
            Summarize the main modules, entry points, and key integrations.

            ## Development Rules
            Add coding conventions, test expectations, review constraints, and operational guardrails.

            ## Important Commands
            List the commands the agent should prefer for build, test, lint, and run workflows.
            """;

        File.WriteAllText(path, content + Environment.NewLine, Encoding.UTF8);
        return $"Created Nim.md at {path}";
    }

    public string ShowSettings()
        => JsonSerializer.Serialize(_runtimeStore.LoadState().Settings, new JsonSerializerOptions { WriteIndented = true });

    public string SetSetting(string key, string value)
    {
        var state = _runtimeStore.LoadState();
        switch (key.ToLowerInvariant())
        {
            case "theme":
                state.Settings.Theme = value;
                break;
            case "approval_mode":
                state.Settings.ApprovalMode = value;
                break;
            case "editor":
                state.Settings.PreferredEditor = value;
                break;
            case "vim":
                state.Settings.VimMode = value.Equals("true", StringComparison.OrdinalIgnoreCase) || value.Equals("on", StringComparison.OrdinalIgnoreCase);
                break;
            case "telemetry":
                state.Settings.TelemetryConsent = value.Equals("true", StringComparison.OrdinalIgnoreCase) || value.Equals("on", StringComparison.OrdinalIgnoreCase);
                break;
            default:
                return $"Unknown setting: {key}";
        }

        _runtimeStore.SaveState(state);
        return $"Set {key} = {value}";
    }

    public string TrustFolder(string? folder)
    {
        var path = Path.GetFullPath(string.IsNullOrWhiteSpace(folder) ? Directory.GetCurrentDirectory() : folder);
        var state = _runtimeStore.LoadState();
        if (!state.Settings.TrustedFolders.Contains(path, StringComparer.OrdinalIgnoreCase))
            state.Settings.TrustedFolders.Add(path);

        _runtimeStore.SaveState(state);
        return $"Trusted folder added: {path}";
    }

    public string ShowTrustedFolders()
    {
        var trusted = _runtimeStore.LoadState().Settings.TrustedFolders;
        return trusted.Count == 0 ? "No trusted folders configured." : string.Join(Environment.NewLine, trusted);
    }

    public async Task<string> RunShellPassthroughAsync(string command, string? workingDirectory = null)
    {
        var result = await _shellProvider.ExecuteAsync(command, workingDirectory, timeoutSeconds: 120);
        return string.Join(Environment.NewLine,
            new[] { result.StandardOutput, string.IsNullOrWhiteSpace(result.StandardError) ? string.Empty : result.StandardError }
                .Where(output => !string.IsNullOrWhiteSpace(output)));
    }

    public async Task<string> CopyToClipboardAsync(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return "Nothing to copy.";

        var result = await _shellProvider.ExecuteAsync($"Set-Clipboard -Value {PowerShellCommandBuilder.QuoteLiteral(content)}", Directory.GetCurrentDirectory(), timeoutSeconds: 30);
        return result.Success ? "Copied last assistant message to clipboard." : "Clipboard copy failed.";
    }

    public async Task<string> OpenUrlAsync(string url)
    {
        var result = await _shellProvider.ExecuteAsync($"Start-Process {PowerShellCommandBuilder.QuoteLiteral(url)}", Directory.GetCurrentDirectory(), timeoutSeconds: 30);
        return result.Success ? $"Opened {url}" : $"Failed to open {url}";
    }

    public string ReadPathContext(string workingDirectory, string pathExpression)
    {
        var resolved = Path.GetFullPath(Path.IsPathRooted(pathExpression) ? pathExpression : Path.Combine(workingDirectory, pathExpression));
        if (File.Exists(resolved))
            return $"[Included File: {resolved}]\n{File.ReadAllText(resolved)}";

        if (Directory.Exists(resolved))
        {
            var builder = new StringBuilder();
            builder.AppendLine($"[Included Directory: {resolved}]");
            foreach (var file in Directory.GetFiles(resolved, "*", SearchOption.AllDirectories)
                         .Where(path => !path.Contains("\\bin\\", StringComparison.OrdinalIgnoreCase) &&
                                        !path.Contains("\\obj\\", StringComparison.OrdinalIgnoreCase))
                         .Take(20))
            {
                builder.AppendLine();
                builder.AppendLine($"## {Path.GetRelativePath(resolved, file)}");
                builder.AppendLine(File.ReadAllText(file));
            }

            return builder.ToString();
        }

        return $"Path not found: {pathExpression}";
    }

    public string ShowWorkspaceSummary(string workingDirectory)
    {
        var root = Path.GetFullPath(workingDirectory);
        var solutionFiles = Directory.GetFiles(root, "*.sln*", SearchOption.TopDirectoryOnly);
        var projectFiles = Directory.GetFiles(root, "*.csproj", SearchOption.AllDirectories)
            .Where(path => !path.Contains("\\bin\\", StringComparison.OrdinalIgnoreCase) &&
                           !path.Contains("\\obj\\", StringComparison.OrdinalIgnoreCase))
            .ToList();
        var isGitRepo = Directory.Exists(Path.Combine(root, ".git"));

        return string.Join(Environment.NewLine,
        [
            $"Workspace: {root}",
            $"Git Repo: {(isGitRepo ? "Detected" : "Not detected")}",
            $"Solutions: {(solutionFiles.Length == 0 ? "(none)" : string.Join(", ", solutionFiles.Select(Path.GetFileName)))}",
            $"Projects: {projectFiles.Count}",
            $"Memory Files: {FindMemoryFiles(root).Count}",
            $"Trusted: {ShowTrustedFolders()}"
        ]);
    }

    public string SwitchWorkspace(SessionState session, SessionManager sessionManager, string newDirectory)
    {
        var fullPath = Path.GetFullPath(newDirectory);
        if (!Directory.Exists(fullPath))
            return $"Workspace not found: {fullPath}";

        sessionManager.InitializeNewSession(session, fullPath, []);
        sessionManager.SaveSession(session);
        return $"Workspace switched to {fullPath}";
    }
}
