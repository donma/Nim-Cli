# 功能名稱

`restore`

## 測試目的

確認 `restore` 已具備日常可用的 checkpoint 管理能力，而不只是單純吃 tag。需能列出可還原 checkpoint、顯示 checkpoint 摘要，並可實際執行還原。

## 前置條件

- 工作目錄：`D:\AI_PROJECTS\Nim-Cli\Nim-Cli`
- 已可執行 `dotnet run --project src/Nim-Cli/Nim-Cli.csproj -- ...`
- 執行目錄使用 `.nim-cli-runtime\` 保存 session 與 checkpoint

## 測試步驟

1. 建立 smoke test checkpoint。
2. 執行 `restore list` 確認可列出還原點。
3. 執行 `restore show <tag>` 確認可查看 checkpoint 詳細摘要。

## 實際指令

```powershell
dotnet run --project "src/Nim-Cli/Nim-Cli.csproj" -- resume save bp3-restore-smoke
dotnet run --project "src/Nim-Cli/Nim-Cli.csproj" -- restore list
dotnet run --project "src/Nim-Cli/Nim-Cli.csproj" -- restore show bp3-restore-smoke
```

## 預期結果

- `resume save` 成功建立 checkpoint。
- `restore list` 顯示 checkpoint 清單、索引、建立時間、訊息數、工具紀錄數。
- `restore show` 顯示 checkpoint id、session id、工作目錄、模式、build/test/repo map 摘要與實際還原指令。

## 實際結果

- 成功建立 `bp3-restore-smoke` checkpoint。
- `restore list` 輸出可還原 checkpoint 清單，含 `1. bp3-restore-smoke | 建立時間=... | 訊息=0 | 工具=0 | Session=...`。
- `restore show bp3-restore-smoke` 輸出 checkpoint 詳細資訊，包含：
  - `Checkpoint: bp3-restore-smoke`
  - `工作目錄: D:\AI_PROJECTS\Nim-Cli\Nim-Cli`
  - `模式: Analysis`
  - `最近 build: (無)`
  - `最近 test: (無)`
  - `執行方式: nim-cli restore bp3-restore-smoke`

## 成功 / 失敗

成功

## 失敗原因

無

## 後續建議

- 後續可再補 `restore <index>` 的 CLI smoke report 截圖或 transcript。
- 後續可將 checkpoint 詳細資訊接到 TUI recovery 面板，讓 restore/rewind 在 TUI 也更易用。
