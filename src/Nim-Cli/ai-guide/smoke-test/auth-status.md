# 功能名稱

`auth-status`

## 測試目的

確認 `nim-cli auth status` 在沒有真實金鑰時，仍能安全輸出目前驗證狀態、Base URL 與 Model，且不洩漏 secrets。

## 前置條件

- 工作目錄：`D:\AI_PROJECTS\Nim-Cli\Nim-Cli`
- 執行目錄未配置有效 `appsettings.secret.json` API key

## 測試步驟

1. 執行 `nim-cli auth status`。
2. 確認輸出為未登入狀態。
3. 確認未出現任何真實 key。

## 實際指令

```powershell
dotnet run --project "src/Nim-Cli/Nim-Cli.csproj" -- auth status
```

## 預期結果

- 顯示未找到 API key。
- 顯示目前 Base URL 與 Model。
- 不洩漏任何 secrets。

## 實際結果

- CLI 輸出：`No API key found in appsettings.secret.json. Run 'nim-cli auth login' to write one.`
- 同時輸出：
  - `Base URL: https://integrate.api.nvidia.com/v1`
  - `Model: openai/gpt-oss-120b`
- 未出現任何真實 key。
- 另外已由 `MockNimProviderTests` 驗證成功路徑：登入後 `auth status` 可回傳 `Health: OK (...)` 與 masked key。

## 成功 / 失敗

成功

## 失敗原因

無

## 後續建議

- 後續可將 mock NIM 成功 transcript 另外整理成附錄。
