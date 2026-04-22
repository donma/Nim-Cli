# 功能名稱

`tui-chat`

## 測試目的

確認 TUI 並非另一套平行產品，而是使用共享 `InteractiveCommandService` / `AgentOrchestrator` 作為聊天入口。

## 前置條件

- `TuiApplication` 使用 `ServiceConfiguration.BuildServices(options)`
- TUI 與 CLI 共用 `SessionState`、`SessionManager`、`InteractiveCommandService`

## 測試步驟

1. 檢視 `TuiApplication.RunAsync()`。
2. 確認 slash command 走共享 `InteractiveCommandService`。
3. 確認一般輸入走共享 `AgentOrchestrator.RunAsync()`。

## 實際指令

```text
TuiApplication.RunAsync -> InteractiveCommandService.ExecuteAsync / AgentOrchestrator.RunAsync
```

## 預期結果

- TUI chat 與 CLI chat 共用核心流程。
- 不存在另一套獨立聊天 orchestrator。

## 實際結果

- `TuiApplication` 以 `ServiceConfiguration.BuildServices(options)` 建立共享服務。
- slash commands 呼叫 `InteractiveCommandService.ExecuteAsync(...)`。
- 一般訊息呼叫 `AgentOrchestrator.RunAsync(input)`。
- 符合 BigPhase3「一套核心，多個入口」要求。

## 成功 / 失敗

成功

## 失敗原因

無

## 後續建議

- 後續可補真實 TUI 聊天 transcript 與 approval prompt transcript。
