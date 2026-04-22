using System.Reflection;

namespace NimCli.App;

public sealed class UpdateCommandService
{
    public string GetUpdateInfo()
        => string.Join(Environment.NewLine,
        [
            $"目前版本：{Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown"}",
            "更新機制：本階段不內建自動升級器。",
            "建議流程：重新建置或重新發布最新版本，並以 doctor / build / smoke test 驗證更新後環境。",
            "若使用本機原始碼：請先 git pull，再執行 dotnet build Nim-Cli.slnx。"
        ]);
}
