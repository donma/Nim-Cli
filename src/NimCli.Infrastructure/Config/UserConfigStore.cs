using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace NimCli.Infrastructure.Config;

public static class UserConfigStore
{
    public static string AppHomeDirectory =>
        Environment.GetEnvironmentVariable("NIMCLI_HOME")
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NimCli");

    public static string ConfigDirectory => AppHomeDirectory;
    public static string WorkspaceDirectory => Directory.GetCurrentDirectory();

    public static string ConfigFilePath => Path.Combine(ConfigDirectory, "appsettings.secret.json");
    public static void EnsureDirectoryExists()
    {
        Directory.CreateDirectory(ConfigDirectory);
    }

    public static void SaveApiKey(string apiKey)
    {
        var options = LoadUserConfig();
        options.Provider.ApiKey = apiKey;
        SaveUserConfig(options);
    }

    public static string? LoadApiKey()
    {
        var options = LoadUserConfig();
        return string.IsNullOrWhiteSpace(options.Provider.ApiKey) ? null : options.Provider.ApiKey;
    }

    public static bool HasApiKey() => !string.IsNullOrWhiteSpace(LoadApiKey());

    public static string MaskKey(string key)
    {
        if (string.IsNullOrEmpty(key) || key.Length <= 8) return "****";
        return key[..4] + new string('*', key.Length - 8) + key[^4..];
    }

    public static NimCliOptions LoadUserConfig()
    {
        var options = LoadAppSettings();

        if (string.IsNullOrWhiteSpace(options.Provider.DefaultModel) && !string.IsNullOrWhiteSpace(options.Provider.Name))
            options.Provider.DefaultModel = "openai/gpt-oss-120b";

        return options;
    }

    public static void SaveUserConfig(NimCliOptions options)
    {
        EnsureDirectoryExists();
        File.WriteAllText(ConfigFilePath, JsonSerializer.Serialize(new { NimCli = options },
            new JsonSerializerOptions { WriteIndented = true }));
    }

    private static NimCliOptions LoadAppSettings()
    {
        try
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile(Path.Combine(ConfigDirectory, "appsettings.json"), optional: true)
                .AddJsonFile(Path.Combine(ConfigDirectory, "appsettings.secret.json"), optional: true)
                .AddJsonFile(Path.Combine(ConfigDirectory, "appsettings.Local.json"), optional: true)
                .AddJsonFile(Path.Combine(WorkspaceDirectory, "appsettings.json"), optional: true)
                .AddJsonFile(Path.Combine(WorkspaceDirectory, "appsettings.secret.json"), optional: true)
                .AddJsonFile(Path.Combine(WorkspaceDirectory, "appsettings.Local.json"), optional: true)
                .Build();

            return configuration.GetSection("NimCli").Get<NimCliOptions>() ?? new NimCliOptions();
        }
        catch
        {
            return new NimCliOptions();
        }
    }

    private static NimCliOptions MergeOptions(NimCliOptions defaults, NimCliOptions overrides)
    {
        defaults.Provider = overrides.Provider ?? defaults.Provider;
        defaults.Shell = overrides.Shell ?? defaults.Shell;
        defaults.Browser = overrides.Browser ?? defaults.Browser;
        defaults.Coding = overrides.Coding ?? defaults.Coding;
        defaults.Tools = overrides.Tools ?? defaults.Tools;
        defaults.Retry = overrides.Retry ?? defaults.Retry;
        defaults.Mcp = overrides.Mcp ?? defaults.Mcp;
        return defaults;
    }
}
