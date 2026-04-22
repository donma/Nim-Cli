namespace NimCli.App;

public sealed record CommandCatalogEntry(
    string Command,
    string Category,
    string Description,
    string Handler,
    string Status,
    bool AvailableInCli,
    bool AvailableInTui);

public sealed class CommandCatalogService
{
    private static readonly IReadOnlyList<CommandCatalogEntry> Entries =
    [
        new("auth", "核心入口", "登入與檢查 NIM 驗證狀態", "CliApplication.HandleAuthAsync -> AuthCommands", "已實作", true, true),
        new("models", "核心入口", "列出可用模型", "CliApplication.HandleModelsAsync -> ModelsCommands", "已實作", true, true),
        new("chat", "核心入口", "互動聊天模式", "ChatCommands.RunChatAsync", "已實作", true, true),
        new("run", "核心入口", "單次 prompt 執行", "CliApplication -> ChatCommands.RunChatAsync", "已實作", true, true),
        new("doctor", "核心入口", "環境與依賴健康檢查", "DoctorCommandService", "已實作", true, true),
        new("plan", "核心入口", "變更規劃與風險摘要", "PlanCommandService", "已實作", true, true),
        new("session", "核心入口", "顯示、清除、恢復 session", "SessionCommandService", "已實作", true, true),
        new("build", "高頻命令", "執行 dotnet build", "ToolCommands.BuildAsync", "已實作", true, true),
        new("run-project", "高頻命令", "執行 dotnet run", "ToolCommands.RunProjectAsync", "已實作", true, true),
        new("screenshot", "高頻命令", "開頁並截圖", "ToolCommands.ScreenshotAsync", "已實作", true, true),
        new("browser", "高頻命令", "瀏覽器開啟、導頁、等待、關閉", "CliApplication.HandleBrowserAsync", "已實作", true, true),
        new("analyze", "高頻命令", "專案分析與 repo map 輔助", "ToolCommands.AnalyzeAsync", "已實作", true, true),
        new("repo map", "高頻命令", "生成 repo map", "ToolCommands.RepoMapAsync", "已實作", true, true),
        new("db query", "高頻命令", "唯讀資料庫查詢", "ToolCommands.DbQueryAsync", "已實作", true, false),
        new("ftp upload", "高頻命令", "FTP 單檔上傳", "ToolCommands.FtpUploadAsync", "已實作", true, false),
        new("git status", "高頻命令", "檢視 git 狀態", "ToolCommands.GitStatusAsync", "已實作", true, false),
        new("git diff", "高頻命令", "檢視 git diff", "ToolCommands.GitDiffAsync", "已實作", true, false),
        new("git commit", "高頻命令", "建立 commit", "ToolCommands.GitCommitAsync", "已實作", true, false),
        new("git push", "高頻命令", "推送遠端", "ToolCommands.GitPushAsync", "已實作", true, false),
        new("lint", "高頻命令", "執行格式/靜態檢查", "ToolCommands.LintAsync", "已實作", true, false),
        new("test", "高頻命令", "執行測試", "ToolCommands.TestAsync", "已實作", true, false),
        new("hooks", "次要命令", "管理 hooks registry", "CliApplication.HandleHooksCommand -> RegistryCommandService", "已實作", true, true),
        new("agents", "次要命令", "切換 agent mode", "CompatibilityCommandService.HandleAgents", "已實作", true, true),
        new("skills", "次要命令", "管理 skills registry", "CliApplication.HandleSkillsCommand -> RegistryCommandService", "已實作", true, true),
        new("extensions", "次要命令", "管理 extension registry", "CliApplication.HandleExtensionsCommand -> RegistryCommandService", "已實作", true, true),
        new("commands", "次要命令", "列出命令矩陣與同步狀態", "CommandCatalogService", "已實作", true, true),
        new("workspace", "次要命令", "查看與管理目前 workspace", "WorkspaceCommandService", "已實作", true, true),
        new("compatibility", "次要命令", "查看兼容對位資訊", "CompatibilityCommandService", "已實作", true, true),
        new("vim", "次要命令", "編輯器整合與啟用狀態", "VimCommandService", "已實作", true, true),
        new("restore", "次要命令", "還原 checkpoint", "CompatibilityCommandService.HandleRestore", "已實作", true, true),
        new("rewind", "次要命令", "退回最近 checkpoint/上下文", "CompatibilityCommandService.HandleRestore", "已實作", true, true),
        new("setup-github", "次要命令", "檢查 GitHub CLI 與環境", "CompatibilityCommandService.HandleSetupGitHubAsync", "已實作", true, true),
        new("terminal-setup", "次要命令", "檢查終端與編輯器設定", "CompatibilityCommandService.HandleTerminalSetupDetailedAsync", "已實作", true, true),
        new("update", "次要命令", "查看版本與更新說明", "UpdateCommandService", "已實作", true, true),
        new("memory", "次要命令", "管理 Nim.md 記憶檔", "CliApplication.HandleMemoryCommand", "已實作", true, true),
        new("settings", "次要命令", "查看與修改設定", "CliApplication.HandleSettingsCommand", "已實作", true, true),
        new("compress", "次要命令", "壓縮目前 session 並保存 checkpoint", "CompatibilityCommandService.HandleCompress", "已實作", true, true),
        new("stats", "次要命令", "查看 session/tool/model 統計", "CliApplication.HandleStatsCommand", "已實作", true, true),
        new("mcp", "次要命令", "查看與管理 MCP server 與 tool", "CliApplication.HandleMcpAsync -> McpCommandService", "已實作", true, true)
    ];

    public IReadOnlyList<CommandCatalogEntry> GetAll()
        => Entries;

    public string FormatSummary()
        => string.Join(Environment.NewLine,
            Entries.Select(entry =>
                $"{entry.Command} | {entry.Category} | {entry.Status} | CLI={(entry.AvailableInCli ? "Y" : "N")} | TUI={(entry.AvailableInTui ? "Y" : "N")} | {entry.Description}"));

    public string FormatCompatibilitySummary()
        => string.Join(Environment.NewLine,
        [
            "目前兼容模式：Gemini CLI 風格命令面 + Nim-Cli 擴充工作流",
            "CLI/TUI 共用核心：AgentOrchestrator / ContextBuilder / ToolRegistry / ToolPolicyService / SessionManager / CodingPipeline",
            "對位重點：聊天、工具編排、session、plan、doctor、approval、coding workflow",
            "未追求 1:1 的部分：畫面細節、私有實作細節、非必要 gimmick"
        ]);
}
