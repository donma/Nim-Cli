# 功能名稱

`tui-event-driven`

## 測試目的

確認 TUI 的 focus / mode / palette / sessions 已具備可自動化驗證的 event-driven state API。

## 前置條件

- `TuiApplication.HandleUiCommandForTest(...)` 可用

## 測試資料

- `/palette`
- `/focus status`
- `/mode coding`
- `/sessions`

## 測試步驟

1. 執行 `tests/NimTui.Tests`
2. 確認 `TuiEventDrivenTests` 通過

## 實際命令

```powershell
dotnet test "tests/NimTui.Tests/NimTui.Tests.csproj"
```

## 預期結果

- TUI 狀態轉換可被測試直接驗證

## 實際結果

- `TuiEventDrivenTests` 通過

## 成功 / 失敗

成功

## 是否 dry-run

否

## policy / risk 說明

- 無高風險操作

## 後續建議

- 後續補鍵盤事件模擬器
