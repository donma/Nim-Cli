# 功能名稱

`cli-tui-mixed-workflow`

## 測試目的

確認 CLI 與 TUI 混合使用時，共享 session state 仍一致。

## 前置條件

- `TuiLongRunTests.Cli_Tui_Mixed_Workflow_Keeps_Shared_Session_State`

## 測試步驟

1. 執行 TUI tests
2. 確認 mixed workflow 測試通過

## 實際命令

```powershell
dotnet test "tests/NimTui.Tests/NimTui.Tests.csproj"
```

## 預期結果

- TUI 可看到 CLI 已留下的 recent actions / task state

## 實際結果

- 對應測試通過

## 成功 / 失敗

成功

## 是否 dry-run

否

## policy / risk 說明

- 無高風險操作

## 後續建議

- 後續可補 CLI/TUI 交替多次切換 scenario
