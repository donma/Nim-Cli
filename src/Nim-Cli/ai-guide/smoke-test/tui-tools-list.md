# 功能名稱

`tui-tools-list`

## 測試目的

確認 TUI 可顯示共享工具總覽與 policy 對照資訊。

## 前置條件

- `PolicySummaryService` 與 `ToolRegistry` 已註冊

## 測試資料

- TUI layout / policy panel

## 測試步驟

1. 執行 TUI tests
2. 確認 layout test 通過

## 實際命令

```powershell
dotnet test "tests/NimTui.Tests/NimTui.Tests.csproj"
```

## 預期結果

- status pane 可顯示 policy/tool summary

## 實際結果

- 對應 layout 測試通過

## 成功 / 失敗

成功

## 是否 dry-run

否

## policy / risk 說明

- 只驗證摘要顯示，不執行工具

## 後續建議

- 後續可補 `/tools` 與 panel 互動整合測試
