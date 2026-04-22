# 功能名稱

`tui-screenshot`

## 測試目的

確認 TUI 已有共享 `/screenshot` slash command 入口，符合 BigPhase3 CLI/TUI 同步要求。

## 前置條件

- TUI 使用共享 `InteractiveCommandService`
- `TuiApplication` 快捷列已顯示 `/screenshot`

## 測試步驟

1. 檢查 TUI 渲染提示列。
2. 確認已顯示 `/screenshot`。
3. 以既有 TUI slash command 測試基礎確認共享 routing 已打通。

## 實際指令

```text
TuiApplication Render -> Slash Commands: /help /tools /models /plan /doctor /build /run-project /screenshot /analyze /session /clear /exit
```

## 預期結果

- `/screenshot` 於 TUI 有明確快捷入口。
- 由共享 slash command service 處理。

## 實際結果

- `TuiApplication` 已顯示 `/screenshot`。
- `InteractiveCommandService` 已新增 `case "screenshot"` 轉接共享 CLI tool path。

## 成功 / 失敗

成功

## 失敗原因

無

## 後續建議

- 後續可在安裝 Playwright 後補真實 TUI `/screenshot --url ...` transcript。
