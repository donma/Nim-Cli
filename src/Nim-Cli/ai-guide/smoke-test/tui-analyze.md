# 功能名稱

`tui-analyze`

## 測試目的

確認 TUI 已同步共享 `analyze` 能力。

## 前置條件

- `InteractiveCommandService` 支援 `/analyze`

## 測試資料

- `Shared_Interactive_Command_Service_Handles_Analyze_Command`

## 測試步驟

1. 執行 `tests/NimTui.Tests`
2. 確認 analyze 共享測試通過

## 實際命令

```powershell
dotnet test "tests/NimTui.Tests/NimTui.Tests.csproj"
```

## 預期結果

- `/analyze` 在 TUI 可用

## 實際結果

- 對應測試通過

## 成功 / 失敗

成功

## 是否 dry-run

否

## policy / risk 說明

- 低風險分析操作

## 後續建議

- 後續可補 analyze result 在 status pane 的更細緻驗證
