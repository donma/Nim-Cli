# 功能名稱

`tui-policy-summary`

## 測試目的

確認 TUI 已把 policy summary 納入主要工作流畫面。

## 前置條件

- `PolicySummaryService.FormatSummaries()` 可提供 dry-run / reason 訊息

## 測試資料

- TUI layout / policy tests

## 測試步驟

1. 執行 core 與 TUI tests
2. 確認 policy 與 TUI layout 測試通過

## 實際命令

```powershell
dotnet test "tests/NimCli.Core.Tests/NimCli.Core.Tests.csproj"
dotnet test "tests/NimTui.Tests/NimTui.Tests.csproj"
```

## 預期結果

- TUI status pane 可看到 risk / decision / dry-run

## 實際結果

- 對應測試通過

## 成功 / 失敗

成功

## 是否 dry-run

否

## policy / risk 說明

- 本報告本身即為 policy summary UX 證據

## 後續建議

- 後續可補不同 approval mode 的畫面差異測試
