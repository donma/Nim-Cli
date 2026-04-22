using System.Runtime.InteropServices;
using NimCli.Infrastructure;
using NimCli.Tools.Shell;

namespace NimCli.App;

public sealed class VimCommandService
{
    private readonly CliRuntimeStore _runtimeStore;
    private readonly IShellProvider _shellProvider;

    public VimCommandService(CliRuntimeStore runtimeStore, IShellProvider shellProvider)
    {
        _runtimeStore = runtimeStore;
        _shellProvider = shellProvider;
    }

    public async Task<(int ExitCode, string Output)> HandleAsync(string[] args)
    {
        var subcommand = args.Length == 0 ? string.Empty : args[0].ToLowerInvariant();

        return subcommand switch
        {
            "" => await HandleDefaultAsync(),
            "status" => await GetStatusAsync(),
            "install" => await InstallAsync(enableAfterInstall: true),
            "enable" => await EnableAsync(allowInstall: true),
            "disable" => Disable(),
            _ => (1, "Usage: nim-cli vim [status|install|enable|disable]")
        };
    }

    private async Task<(int ExitCode, string Output)> HandleDefaultAsync()
    {
        var editor = await DetectEditorAsync();
        if (editor is null)
            return await InstallAsync(enableAfterInstall: true);

        return Enable(editor.Value.Command, editor.Value.Path, wasInstalledDuringCommand: false);
    }

    private async Task<(int ExitCode, string Output)> GetStatusAsync()
    {
        var state = _runtimeStore.LoadState();
        var editor = await DetectEditorAsync();
        var lines = new List<string>
        {
            $"Vim mode: {(state.Settings.VimMode ? "enabled" : "disabled")}",
            $"Preferred editor: {state.Settings.PreferredEditor ?? "(not set)"}",
            editor is null
                ? "Detected Vim editor: not installed"
                : $"Detected Vim editor: {editor.Value.Command} ({editor.Value.Path})"
        };

        if (editor is null)
            lines.Add($"Install suggestion: {GetInstallSummary()}.");

        return (0, string.Join(Environment.NewLine, lines));
    }

    private async Task<(int ExitCode, string Output)> EnableAsync(bool allowInstall)
    {
        var editor = await DetectEditorAsync();
        if (editor is not null)
            return Enable(editor.Value.Command, editor.Value.Path, wasInstalledDuringCommand: false);

        if (!allowInstall)
            return (1, "No Vim-compatible editor detected. Run 'nim-cli vim install' first.");

        return await InstallAsync(enableAfterInstall: true);
    }

    private (int ExitCode, string Output) Disable()
    {
        var state = _runtimeStore.LoadState();
        state.Settings.VimMode = false;
        _runtimeStore.SaveState(state);
        return (0, "Vim mode disabled.");
    }

    private async Task<(int ExitCode, string Output)> InstallAsync(bool enableAfterInstall)
    {
        var plan = await GetInstallPlanAsync();
        if (plan is null)
            return (1, $"No supported package manager found for automatic Neovim installation. {GetInstallSummary()}.");

        if (!ConfirmInstall(plan.Value.Description))
            return (1, "Vim installation cancelled.");

        var result = await _shellProvider.ExecuteAsync(plan.Value.Command, Directory.GetCurrentDirectory(), timeoutSeconds: plan.Value.TimeoutSeconds);
        if (!result.Success)
            return (1, BuildInstallFailureMessage(plan.Value.Description, result));

        var editor = await DetectEditorAsync();
        if (editor is null)
            return (1, $"Installation command completed, but no Vim-compatible editor was detected afterward. {GetInstallSummary()}.");

        if (!enableAfterInstall)
            return (0, $"Installed {editor.Value.Command} at {editor.Value.Path}.");

        var enabled = Enable(editor.Value.Command, editor.Value.Path, wasInstalledDuringCommand: true);
        return (enabled.ExitCode, $"Installed and enabled {editor.Value.Command}.{Environment.NewLine}{enabled.Output}");
    }

    private (int ExitCode, string Output) Enable(string command, string path, bool wasInstalledDuringCommand)
    {
        var state = _runtimeStore.LoadState();
        state.Settings.VimMode = true;
        state.Settings.PreferredEditor = command;
        _runtimeStore.SaveState(state);

        var prefix = wasInstalledDuringCommand ? "" : "Vim mode enabled." + Environment.NewLine;
        return (0, $"{prefix}Preferred editor set to {command}.{Environment.NewLine}Detected path: {path}");
    }

    private async Task<DetectedEditor?> DetectEditorAsync()
    {
        foreach (var command in new[] { "nvim", "vim" })
        {
            var result = await _shellProvider.ExecuteAsync(
                $"$cmd = Get-Command {command} -ErrorAction SilentlyContinue; if ($null -ne $cmd) {{ $cmd.Source }}",
                Directory.GetCurrentDirectory(),
                timeoutSeconds: 15);

            if (!result.Success)
                continue;

            var path = result.StandardOutput
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(path))
                return new DetectedEditor(command, path);
        }

        return null;
    }

    private static bool ConfirmInstall(string description)
    {
        Console.Write($"Approval required to install Neovim via {description}. Continue? [y/N] ");
        var answer = Console.ReadLine()?.Trim().ToLowerInvariant();
        return answer is "y" or "yes";
    }

    private static string BuildInstallFailureMessage(string description, ShellResult result)
    {
        var details = new[] { result.StandardOutput, result.StandardError }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();

        return details.Length == 0
            ? $"Failed to install Neovim via {description}."
            : $"Failed to install Neovim via {description}.{Environment.NewLine}{string.Join(Environment.NewLine, details)}";
    }

    private static string GetInstallSummary()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "Install Neovim manually with 'winget install --id Neovim.Neovim -e'";

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "Install Neovim manually with 'brew install neovim'";

        return "Install Neovim manually with your package manager, for example 'sudo apt-get install -y neovim'";
    }

    private async Task<InstallPlan?> GetLinuxInstallPlanAsync()
    {
        foreach (var candidate in new[]
                 {
                     new InstallPlan("apt-get", "apt-get", "sudo apt-get update && sudo apt-get install -y neovim", 600),
                     new InstallPlan("dnf", "dnf", "sudo dnf install -y neovim", 600),
                     new InstallPlan("pacman", "pacman", "sudo pacman -Sy --noconfirm neovim", 600)
                 })
        {
            var probe = await _shellProvider.ExecuteAsync(
                $"$cmd = Get-Command {candidate.ProbeCommand} -ErrorAction SilentlyContinue; if ($null -ne $cmd) {{ $cmd.Source }}",
                Directory.GetCurrentDirectory(),
                timeoutSeconds: 15);

            if (probe.Success && !string.IsNullOrWhiteSpace(probe.StandardOutput))
                return candidate;
        }

        return null;
    }

    private async Task<InstallPlan?> GetInstallPlanAsync()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new InstallPlan("winget", "winget", "winget install --id Neovim.Neovim -e --accept-source-agreements --accept-package-agreements", 600);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return new InstallPlan("Homebrew", "brew", "brew install neovim", 600);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return await GetLinuxInstallPlanAsync();

        return null;
    }

    private readonly record struct DetectedEditor(string Command, string Path);

    private readonly record struct InstallPlan(string Description, string ProbeCommand, string Command, int TimeoutSeconds);
}
