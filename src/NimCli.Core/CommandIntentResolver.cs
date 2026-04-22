namespace NimCli.Core;

public enum IntentType
{
    Chat,
    AnalyzeProject,
    PlanChange,
    BuildProject,
    RunProject,
    ScreenshotPage,
    QueryDb,
    GitPush,
    GitCommit,
    GitStatus,
    UploadFtp,
    WebSearch,
    WebFetch,
    EditFiles,
    SessionManagement,
    SettingsManagement,
    HooksManagement,
    SkillsManagement,
    ExtensionsManagement,
    Unknown
}

public record ResolvedIntent(IntentType Type, string OriginalInput, Dictionary<string, string>? Parameters = null);

public class CommandIntentResolver
{
    private static readonly Dictionary<string[], IntentType> _patterns = new()
    {
        { ["analyze", "分析", "建議", "suggest", "review", "弱點", "weakness"], IntentType.AnalyzeProject },
        { ["plan", "規劃", "impact", "風險", "步驟"], IntentType.PlanChange },
        { ["build", "編譯", "compile", "dotnet build", "msbuild"], IntentType.BuildProject },
        { ["run", "執行", "start", "dotnet run", "launch"], IntentType.RunProject },
        { ["screenshot", "截圖", "screen shot", "capture"], IntentType.ScreenshotPage },
        { ["db", "database", "資料庫", "query", "查詢"], IntentType.QueryDb },
        { ["git push", "push"], IntentType.GitPush },
        { ["git commit", "commit"], IntentType.GitCommit },
        { ["git status", "status"], IntentType.GitStatus },
        { ["ftp", "upload", "上傳"], IntentType.UploadFtp },
        { ["search", "搜尋", "find news", "找新聞", "google"], IntentType.WebSearch },
        { ["fetch", "read url", "open url", "http", "https"], IntentType.WebFetch },
        { ["edit", "modify", "change", "fix", "修改", "改"], IntentType.EditFiles },
        { ["session", "resume", "history", "會話"], IntentType.SessionManagement },
        { ["settings", "config", "設定"], IntentType.SettingsManagement },
        { ["hooks", "hook"], IntentType.HooksManagement },
        { ["skills", "skill"], IntentType.SkillsManagement },
        { ["extensions", "extension", "外掛"], IntentType.ExtensionsManagement },
    };

    public ResolvedIntent Resolve(string input)
    {
        var lower = input.ToLowerInvariant();

        foreach (var (keywords, intentType) in _patterns)
        {
            if (keywords.Any(k => lower.Contains(k)))
                return new ResolvedIntent(intentType, input);
        }

        return new ResolvedIntent(IntentType.Chat, input);
    }
}
