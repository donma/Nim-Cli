# 功能名稱

`models-list`

## 測試目的

確認 `nim-cli models list` 在未登入情境下能明確回報缺少 API key，而不是無回應或崩潰。

## 前置條件

- 工作目錄：`D:\AI_PROJECTS\Nim-Cli\Nim-Cli`
- 未配置有效 NIM API key

## 測試步驟

1. 執行 `nim-cli models list`。
2. 確認指令結束且輸出缺少金鑰的引導訊息。

## 實際指令

```powershell
dotnet run --project "src/Nim-Cli/Nim-Cli.csproj" -- models list
```

## 預期結果

- 明確提示需要先執行 `auth login`。
- 不拋出未處理例外。

## 實際結果

- CLI 輸出：`No API key found in appsettings.secret.json. Run 'nim-cli auth login' first.`
- 指令行為符合預期，屬於受控未登入回應。
- `MockNimProviderTests` 已額外驗證成功路徑：本機 mock NIM server 可回傳 model 清單，`models list` 退出碼為 0。

## 成功 / 失敗

成功

## 失敗原因

無

## 後續建議

- 後續可把 mock model list transcript 寫成專用報告附錄。
