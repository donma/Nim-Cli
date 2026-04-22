# 功能名稱

`tui-session-resume`

## 測試目的

確認 TUI 已把 session / recent sessions 納入共享工作流。

## 前置條件

- `TuiApplication.BuildRecentSessionsPanel(...)` 已實作

## 測試資料

- TUI layout 與 recent sessions 面板

## 測試步驟

1. 執行 `tests/NimTui.Tests`
2. 確認 TUI layout 測試通過

## 實際命令

```powershell
dotnet test "tests/NimTui.Tests/NimTui.Tests.csproj"
```

## 預期結果

- TUI 可顯示 recent sessions / session 狀態摘要

## 實際結果

- layout 與 recent sessions summary 已存在

## 成功 / 失敗

成功

## 是否 dry-run

否

## policy / risk 說明

- 無高風險操作

## 後續建議

- 後續可補 session resume 的互動式 selection test
