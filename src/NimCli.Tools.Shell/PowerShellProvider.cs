using System.Diagnostics;
using System.Text;
using NimCli.Infrastructure.Config;

namespace NimCli.Tools.Shell;

public interface IShellProvider
{
    Task<ShellResult> ExecuteAsync(string command, string? workingDir = null, int timeoutSeconds = 60, CancellationToken cancellationToken = default);
}

public record ShellResult(int ExitCode, string StandardOutput, string StandardError, bool TimedOut = false)
{
    public bool Success => ExitCode == 0 && !TimedOut;
}

public class PowerShellProvider : IShellProvider
{
    private readonly NimCliOptions _options;

    public PowerShellProvider(NimCliOptions options)
    {
        _options = options;
    }

    public async Task<ShellResult> ExecuteAsync(
        string command,
        string? workingDir = null,
        int timeoutSeconds = 60,
        CancellationToken cancellationToken = default)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _options.Shell.PowershellExe,
            Arguments = $"-NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand {EncodeCommand(command)}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = ResolveWorkingDirectory(workingDir)
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            var stdoutTask = process.StandardOutput.ReadToEndAsync(cts.Token);
            var stderrTask = process.StandardError.ReadToEndAsync(cts.Token);

            await Task.WhenAll(
                process.WaitForExitAsync(cts.Token),
                stdoutTask,
                stderrTask
            );

            return new ShellResult(
                process.ExitCode,
                await stdoutTask,
                await stderrTask
            );
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            try { process.Kill(true); } catch { }
            return new ShellResult(-1, "", "Command timed out.", TimedOut: true);
        }
    }

    private static string EncodeCommand(string command)
        => Convert.ToBase64String(Encoding.Unicode.GetBytes(command));

    private string ResolveWorkingDirectory(string? workingDir)
    {
        if (!string.IsNullOrWhiteSpace(workingDir))
            return workingDir;

        if (!string.IsNullOrWhiteSpace(_options.Shell.WorkingDirectory))
            return _options.Shell.WorkingDirectory;

        return Directory.GetCurrentDirectory();
    }
}

public static class PowerShellCommandBuilder
{
    public static string QuoteLiteral(string value)
        => $"'{(value ?? string.Empty).Replace("'", "''")}'";

    public static string BuildExternalCommand(string executable, params IEnumerable<string?> arguments)
    {
        var parts = new List<string> { "&", executable };
        parts.AddRange(arguments
            .Where(argument => !string.IsNullOrWhiteSpace(argument))
            .Select(argument => QuoteLiteral(argument!)));
        return string.Join(" ", parts);
    }

    public static IReadOnlyList<string> TokenizeArguments(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return [];

        var tokens = new List<string>();
        var current = new StringBuilder();
        char? quote = null;
        var escaping = false;

        foreach (var ch in value)
        {
            if (escaping)
            {
                current.Append(ch);
                escaping = false;
                continue;
            }

            if (ch == '`')
            {
                escaping = true;
                continue;
            }

            if (quote is not null)
            {
                if (ch == quote)
                {
                    quote = null;
                    continue;
                }

                current.Append(ch);
                continue;
            }

            if (ch is '\'' or '"')
            {
                quote = ch;
                continue;
            }

            if (char.IsWhiteSpace(ch))
            {
                if (current.Length > 0)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                }

                continue;
            }

            current.Append(ch);
        }

        if (current.Length > 0)
            tokens.Add(current.ToString());

        return tokens;
    }
}
