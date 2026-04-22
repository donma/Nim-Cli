# 功能名稱

`rewind`

## 測試目的

確認 `rewind` 不再只是 `restore` 的別名殼，而是具備可理解的退回操作說明，且可使用既有 checkpoint 將目前工作狀態退回指定還原點。

## 前置條件

- 工作目錄：`D:\AI_PROJECTS\Nim-Cli\Nim-Cli`
- 已存在 checkpoint：`bp3-restore-smoke`

## 測試步驟

1. 執行 `rewind bp3-restore-smoke`。
2. 確認 CLI 明確說明已退回哪個 checkpoint。
3. 確認輸出同時帶出恢復的訊息數與工具紀錄數。

## 實際指令

```powershell
dotnet run --project "src/Nim-Cli/Nim-Cli.csproj" -- rewind bp3-restore-smoke
```

## 預期結果

- 成功退回指定 checkpoint。
- 輸出應使用「已退回」語意，不是模糊的 placeholder 文字。
- 輸出應包含 checkpoint tag 與恢復內容摘要。

## 實際結果

- 指令成功完成。
- CLI 輸出：`已退回 checkpoint 'bp3-restore-smoke'。訊息 0 筆、工具紀錄 0 筆。可用 'rewind list' 查看其他還原點。`
- 表示 rewind 已具備基本可日常使用的 recovery 語意與 discoverability。

## 成功 / 失敗

成功

## 失敗原因

無

## 後續建議

- 後續可補 `rewind list` / `rewind show latest` 的專用 smoke transcript。
- 若後面補入更完整 transcript step recovery，可讓 `rewind` 額外支援上一個工具步驟或上一輪對話摘要退回。
