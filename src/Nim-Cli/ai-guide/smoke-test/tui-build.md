# 功能名稱

`tui-build`

## 測試目的

確認 TUI 已支援共享 `/build` slash command，而不是只有 CLI 有。

## 前置條件

- 工作目錄：`D:\AI_PROJECTS\Nim-Cli\Nim-Cli`
- 使用共享 `InteractiveCommandService`

## 測試步驟

1. 執行 `tests/NimTui.Tests`。
2. 確認 `Shared_Interactive_Command_Service_Handles_Build_Command` 通過。

## 實際指令

```powershell
dotnet test "tests/NimTui.Tests/NimTui.Tests.csproj"
```

## 預期結果

- `/build` 在 TUI 共享指令層可被正確處理。

## 實際結果

- `dotnet test` 通過。
- `Shared_Interactive_Command_Service_Handles_Build_Command` 測試通過。

## 成功 / 失敗

成功

## 失敗原因

無

## 後續建議

- 後續可加入真正互動式 TUI transcript 報告。
