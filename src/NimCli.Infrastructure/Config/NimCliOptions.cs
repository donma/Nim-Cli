namespace NimCli.Infrastructure.Config;

public class ProviderOptions
{
    public string Name { get; set; } = "nim";
    public string BaseUrl { get; set; } = "https://integrate.api.nvidia.com/v1";
    public string ApiKey { get; set; } = "";
    public string DefaultModel { get; set; } = "openai/gpt-oss-120b";
    public int TimeoutSeconds { get; set; } = 120;
    public bool Streaming { get; set; } = true;
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 4096;
}

public class ShellOptions
{
    public string Default { get; set; } = "powershell";
    public string PowershellExe { get; set; } = "pwsh";
    public string WorkingDirectory { get; set; } = "";
}

public class BrowserOptions
{
    public string Engine { get; set; } = "chromium";
    public bool Headless { get; set; } = true;
    public int DefaultViewportWidth { get; set; } = 1440;
    public int DefaultViewportHeight { get; set; } = 900;
}

public class CodingOptions
{
    public bool EnableRepoMap { get; set; } = true;
    public bool AutoBuildAfterEdit { get; set; } = true;
    public bool AutoLint { get; set; } = true;
    public bool AutoTest { get; set; } = false;
    public bool AutoCommit { get; set; } = false;
}

public class ToolsOptions
{
    public bool AllowShell { get; set; } = true;
    public bool AllowWebFetch { get; set; } = true;
    public bool AllowWebSearch { get; set; } = true;
    public bool AllowBrowser { get; set; } = true;
    public bool AllowDbRead { get; set; } = true;
    public bool AllowFtpUpload { get; set; } = false;
    public bool AllowGitPush { get; set; } = false;
}

public class DbConnectionOptions
{
    public string Type { get; set; } = "sqlserver";
    public string ConnectionString { get; set; } = "";
}

public class RetryOptions
{
    public int MaxAttempts { get; set; } = 2;
    public int DelayMilliseconds { get; set; } = 1000;
}

public class McpOptions
{
    public bool Enabled { get; set; } = false;
    public string Command { get; set; } = "";
    public string Arguments { get; set; } = "";
    public string WorkingDirectory { get; set; } = "";
}

public class NimCliOptions
{
    public ProviderOptions Provider { get; set; } = new();
    public ShellOptions Shell { get; set; } = new();
    public BrowserOptions Browser { get; set; } = new();
    public CodingOptions Coding { get; set; } = new();
    public ToolsOptions Tools { get; set; } = new();
    public Dictionary<string, DbConnectionOptions> DbConnections { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public RetryOptions Retry { get; set; } = new();
    public McpOptions Mcp { get; set; } = new();
}
