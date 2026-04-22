# 功能名稱

`run`

## 測試目的

確認 `nim-cli run "<prompt>"` 在未登入時會被正確擋下，並保有 headless 單次 prompt 執行入口規格。

## 前置條件

- 工作目錄：`D:\AI_PROJECTS\Nim-Cli\Nim-Cli`
- 未配置 API key

## 測試步驟

1. 執行 `nim-cli run "hello"`。
2. 確認先做 `EnsureAuthenticated()`。
3. 確認輸出登入引導。

## 實際指令

```powershell
dotnet run --project "src/Nim-Cli/Nim-Cli.csproj" -- run "hello"
```

## 預期結果

- 未登入時不直接呼叫 provider。
- 輸出先登入提示。

## 實際結果

- `CliApplication` 在 `run` 入口先執行 `EnsureAuthenticated()`。
- 未登入時會輸出：`No API key found in appsettings.secret.json. Run 'nim-cli auth login' first.`
- `MockNimProviderTests` 已額外驗證成功路徑：在本機 mock NIM server 下，`nim-cli -p "hello from mock nim"` 可成功回傳 provider response。

## 成功 / 失敗

成功

## 失敗原因

無

## 後續建議

- 後續可補真實 NIM 環境下的 end-to-end transcript。
