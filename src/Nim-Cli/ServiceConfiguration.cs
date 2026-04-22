using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NimCli.Coding;
using NimCli.Core;
using NimCli.Infrastructure;
using NimCli.Infrastructure.Config;
using NimCli.Mcp;
using NimCli.Provider.Nim;
using NimCli.Tools.Browser;
using NimCli.Tools.Db;
using NimCli.Tools.Ftp;
using NimCli.Tools.Git;
using NimCli.Tools.Shell;
using NimCli.Tools.Web;

namespace NimCli.App;

public static class ServiceConfiguration
{
    public static async Task<IServiceProvider> BuildServicesAsync(NimCliOptions options)
    {
        var services = new ServiceCollection();

        // Logging
        services.AddLogging(logging =>
        {
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Warning); // quiet by default
        });

        // Options
        services.AddSingleton(options);
        services.AddSingleton<CliRuntimeStore>();
        services.AddSingleton<SessionManager>();
        services.AddSingleton<McpCommandService>();
        services.AddSingleton<RegistryCommandService>();
        services.AddSingleton<WorkspaceCommandService>();
        services.AddSingleton<CommandCatalogService>();
        services.AddSingleton<UpdateCommandService>();
        services.AddSingleton<VimCommandService>();
        services.AddSingleton<CompatibilityCommandService>();
        services.AddSingleton<SessionCommandService>();
        services.AddSingleton<DoctorCommandService>();
        services.AddSingleton<PlanCommandService>();
        services.AddSingleton<InteractiveCommandService>();
        services.AddSingleton<ExecutionSummaryFormatter>();
        services.AddSingleton<PolicySummaryService>();

        // Core
        services.AddSingleton<SessionState>();
        services.AddSingleton<ToolRegistry>();
        services.AddSingleton(sp => new ToolPolicyService(options));
        services.AddSingleton<PromptBuilder>();
        services.AddSingleton<ContextBuilder>();
        services.AddSingleton<CommandIntentResolver>();
        services.AddSingleton<ProviderRouter>();

        // Provider
        services.AddSingleton(sp =>
        {
            var apiKey = UserConfigStore.LoadApiKey() ?? string.Empty;
            return new NimProviderOptions
            {
                ApiKey = apiKey,
                BaseUrl = options.Provider.BaseUrl,
                Model = options.Provider.DefaultModel,
                Temperature = options.Provider.Temperature,
                MaxTokens = options.Provider.MaxTokens,
                TimeoutSeconds = options.Provider.TimeoutSeconds
            };
        });
        services.AddSingleton<NimChatProvider>();

        // Orchestrator
        services.AddSingleton<AgentOrchestrator>(sp =>
        {
            var router = sp.GetRequiredService<ProviderRouter>();
            var nimProvider = sp.GetRequiredService<NimChatProvider>();
            router.Register(nimProvider, nimProvider, nimProvider);

            return new AgentOrchestrator(
                router,
                sp.GetRequiredService<ToolRegistry>(),
                sp.GetRequiredService<ToolPolicyService>(),
                sp.GetRequiredService<PromptBuilder>(),
                sp.GetRequiredService<ContextBuilder>(),
                sp.GetRequiredService<CommandIntentResolver>(),
                sp.GetRequiredService<SessionState>(),
                options,
                sp.GetRequiredService<ILogger<AgentOrchestrator>>()
            );
        });

        // Shell provider (shared dependency)
        services.AddSingleton<IShellProvider>(sp => new PowerShellProvider(options));

        // Shell tools
        services.AddSingleton<BuildProjectTool>();
        services.AddSingleton<RunProjectTool>();
        services.AddSingleton<RunShellTool>();

        // Web tools
        services.AddSingleton<WebFetchTool>();
        services.AddSingleton<WebSearchTool>();

        // Browser tools
        services.AddSingleton<BrowserSessionManager>();
        services.AddSingleton<BrowserOpenTool>();
        services.AddSingleton<ScreenshotTool>();
        services.AddSingleton<BrowserNavigateTool>();
        services.AddSingleton<BrowserWaitTool>();
        services.AddSingleton<BrowserCloseTool>();

        // DB tool
        services.AddSingleton<QueryDbTool>();

        // Git tools
        services.AddSingleton<GitStatusTool>();
        services.AddSingleton<GitDiffTool>();
        services.AddSingleton<GitCommitTool>();
        services.AddSingleton<GitPushTool>();

        // FTP tool
        services.AddSingleton<FtpUploadTool>();

        // Coding tools
        services.AddSingleton<RepoMapBuilder>();
        services.AddSingleton<CodeEditPlanner>();
        services.AddSingleton<PatchApplier>();
        services.AddSingleton<CodingPipeline>();
        services.AddSingleton<RepoMapTool>();
        services.AddSingleton<AnalyzeProjectTool>();
        services.AddSingleton<PlanEditTool>();
        services.AddSingleton<ApplyPatchTool>();
        services.AddSingleton<TestProjectTool>();
        services.AddSingleton<LintProjectTool>();
        services.AddSingleton<EditFilesTool>();
        services.AddSingleton<SuggestCommitMessageTool>();

        // MCP registration
        services.AddSingleton<IMcpClient>(_ => options.Mcp.Enabled ? new StdioMcpClient(options.Mcp) : new NullMcpClient());

        var sp = services.BuildServiceProvider();

        // Register tools into ToolRegistry
        var registry = sp.GetRequiredService<ToolRegistry>();
        registry.Register(sp.GetRequiredService<BuildProjectTool>());
        registry.Register(sp.GetRequiredService<RunProjectTool>());
        registry.Register(sp.GetRequiredService<RunShellTool>());
        registry.Register(sp.GetRequiredService<WebFetchTool>());
        registry.Register(sp.GetRequiredService<WebSearchTool>());
        registry.Register(sp.GetRequiredService<BrowserOpenTool>());
        registry.Register(sp.GetRequiredService<ScreenshotTool>());
        registry.Register(sp.GetRequiredService<BrowserNavigateTool>());
        registry.Register(sp.GetRequiredService<BrowserWaitTool>());
        registry.Register(sp.GetRequiredService<BrowserCloseTool>());
        registry.Register(sp.GetRequiredService<QueryDbTool>());
        registry.Register(sp.GetRequiredService<GitStatusTool>());
        registry.Register(sp.GetRequiredService<GitDiffTool>());
        registry.Register(sp.GetRequiredService<GitCommitTool>());
        registry.Register(sp.GetRequiredService<GitPushTool>());
        registry.Register(sp.GetRequiredService<FtpUploadTool>());
        registry.Register(sp.GetRequiredService<RepoMapTool>());
        registry.Register(sp.GetRequiredService<AnalyzeProjectTool>());
        registry.Register(sp.GetRequiredService<PlanEditTool>());
        registry.Register(sp.GetRequiredService<ApplyPatchTool>());
        registry.Register(sp.GetRequiredService<TestProjectTool>());
        registry.Register(sp.GetRequiredService<LintProjectTool>());
        registry.Register(sp.GetRequiredService<EditFilesTool>());
        registry.Register(sp.GetRequiredService<SuggestCommitMessageTool>());
        await McpRegistration.RegisterToolsAsync(sp.GetRequiredService<IMcpClient>(), registry);

        return sp;
    }
}
