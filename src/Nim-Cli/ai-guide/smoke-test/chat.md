# 功能名稱

`chat`

## 測試目的

確認 `nim-cli chat` 在未登入時會走受控驗證檢查，而不是直接進入失敗狀態或崩潰。

## 前置條件

- 工作目錄：`D:\AI_PROJECTS\Nim-Cli\Nim-Cli`
- 未配置 API key

## 測試步驟

1. 執行 `nim-cli chat`。
2. 確認命令先做 `EnsureAuthenticated()`。
3. 確認缺少 key 時直接回報引導訊息。

## 實際指令

```powershell
dotnet run --project "src/Nim-Cli/Nim-Cli.csproj" -- chat
```

## 預期結果

- 不進入半殘聊天流程。
- 明確提示先登入。

## 實際結果

- `CliApplication` 在 `chat` 入口先執行 `EnsureAuthenticated()`。
- 未登入時會輸出：`No API key found in appsettings.secret.json. Run 'nim-cli auth login' first.`
- 屬於受控阻擋行為。

## 成功 / 失敗

成功

## 失敗原因

無

## 後續建議

- 後續可在測試 key 環境補真實 chat transcript smoke case。
