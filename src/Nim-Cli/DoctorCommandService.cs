using System.Diagnostics;
using NimCli.Infrastructure;
using NimCli.Infrastructure.Config;
using NimCli.Mcp;
using NimCli.Provider.Nim;

namespace NimCli.App;

public sealed class DoctorCommandService
{
    public async Task<string> BuildReportAsync(NimCliOptions options)
    {
        var configDirectory = UserConfigStore.ConfigDirectory;
        var lines = new List<string>
        {
            "Nim-CLI Doctor",
            new string('-', 40),
            $"Working Directory: {Directory.GetCurrentDirectory()}",
            $"Base Directory:    {AppContext.BaseDirectory}",
            $"Config Directory:  {configDirectory}",
            $"Base URL:          {options.Provider.BaseUrl}",
            $"Model:             {options.Provider.DefaultModel}",
            $"appsettings.json:  {FormatFileExists(Path.Combine(configDirectory, "appsettings.json"))}",
            $"secret.json:       {FormatFileExists(Path.Combine(configDirectory, "appsettings.secret.json"))}",
            $"appsettings.Local: {FormatFileExists(Path.Combine(configDirectory, "appsettings.Local.json"))}",
            $"API Key:           {(UserConfigStore.HasApiKey() ? "Found in appsettings.secret.json" : "Missing in appsettings.secret.json")}",
            $"DB Config:         {(options.DbConnections.Count == 0 ? "Not configured" : $"Configured ({options.DbConnections.Count})")}",
            $"FTP Config:        {(HasFtpConfig(options) ? "Configured" : "Not configured")}",
            $"Session Storage:   {CheckSessionStorageHealth()}",
            $"Git Repo:          {CheckGitRepoStatus()}"
        };

        lines.Add($"dotnet:            {await CheckCommandAsync("dotnet", "--version")}");
        lines.Add($"pwsh:              {await CheckCommandAsync(options.Shell.PowershellExe, "-NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"$PSVersionTable.PSVersion.ToString()\"")}");
        lines.Add($"playwright:        {await CheckCommandAsync("playwright", "install --help")}");

        if (UserConfigStore.HasApiKey())
            lines.Add($"NIM API:           {await CheckProviderHealthAsync(options)}");

        if (options.Mcp.Enabled)
        {
            var client = new StdioMcpClient(options.Mcp);
            lines.Add($"MCP:               {(await client.IsAvailableAsync() ? "OK" : "Not available")}");
            lines.Add($"MCP Detail:        {await client.GetStatusAsync()}");
        }
        else
        {
            lines.Add("MCP:               Disabled");
        }

        return string.Join(Environment.NewLine, lines);
    }

    public async Task RunAsync(NimCliOptions options)
    {
        Console.WriteLine(await BuildReportAsync(options));
    }

    private static async Task<string> CheckProviderHealthAsync(NimCliOptions options)
    {
        try
        {
            var provider = new NimChatProvider(new NimProviderOptions
            {
                ApiKey = UserConfigStore.LoadApiKey() ?? string.Empty,
                BaseUrl = options.Provider.BaseUrl,
                Model = options.Provider.DefaultModel,
                TimeoutSeconds = options.Provider.TimeoutSeconds
            });

            var healthy = await provider.IsHealthyAsync();
            var status = await provider.GetStatusMessageAsync();
            return healthy ? $"OK ({status})" : $"FAIL ({status})";
        }
        catch (Exception ex)
        {
            return $"FAIL ({ex.Message})";
        }
    }

    private static async Task<string> CheckCommandAsync(string fileName, string arguments)
    {
        try
        {
            var psi = new ProcessStartInfo(fileName, arguments)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            using var process = Process.Start(psi);
            if (process == null)
                return "Not found";

            var stdout = await process.StandardOutput.ReadToEndAsync();
            var stderr = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
                return "Not available";

            var output = string.IsNullOrWhiteSpace(stdout) ? stderr : stdout;
            var line = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            return string.IsNullOrWhiteSpace(line) ? "OK" : $"OK ({line.Trim()})";
        }
        catch
        {
            return "Not found";
        }
    }

    private static string CheckSessionStorageHealth()
    {
        try
        {
            var store = new CliRuntimeStore();
            store.LoadState();
            return "OK";
        }
        catch (Exception ex)
        {
            return $"FAIL ({ex.Message})";
        }
    }

    private static string CheckGitRepoStatus()
        => Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), ".git")) ? "Detected" : "Not a repo";

    private static bool HasFtpConfig(NimCliOptions options)
        => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("NIMCLI_FTP_HOST")) ||
           !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("FTP_HOST")) ||
           options.Tools.AllowFtpUpload;

    private static string FormatFileExists(string path)
        => File.Exists(path) ? "Found" : "Missing";
}
