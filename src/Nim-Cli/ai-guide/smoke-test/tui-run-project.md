# 功能名稱

`tui-run-project`

## 測試目的

確認 TUI 已同步共享 `run-project` 能力。

## 前置條件

- TUI 使用共享 `InteractiveCommandService`

## 測試資料

- `tests/NimTui.Tests`

## 測試步驟

1. 執行 TUI tests
2. 驗證 TUI 共享 slash command 與 command palette 已納入 `Run Project`

## 實際命令

```powershell
dotnet test "tests/NimTui.Tests/NimTui.Tests.csproj"
```

## 預期結果

- `run-project` 被視為 TUI 高頻同步能力

## 實際結果

- TUI palette 與 layout 已包含 `Run Project`

## 成功 / 失敗

成功

## 是否 dry-run

否

## policy / risk 說明

- 無高風險操作

## 後續建議

- 後續可補 `/run-project` 的專屬 TUI shared command test
