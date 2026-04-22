# 功能名稱

`auth-login`

## 測試目的

確認 `nim-cli auth login` 已具備真實互動入口，並會把 key 寫入執行目錄 `appsettings.secret.json`，而不是 placeholder 說明文字。

## 前置條件

- 工作目錄：`D:\AI_PROJECTS\Nim-Cli\Nim-Cli`
- 本次 smoke 不輸入真實 API key

## 測試步驟

1. 執行 `nim-cli auth login`。
2. 確認 CLI 顯示互動提示與保存位置說明。
3. 因本次不注入真 key，不進入完整驗證提交流程。

## 實際指令

```powershell
dotnet run --project "src/Nim-Cli/Nim-Cli.csproj" -- auth login
```

## 預期結果

- 顯示互動輸入提示。
- 說明會寫入 `appsettings.secret.json`。
- 不在螢幕明文回顯 key。

## 實際結果

- `AuthCommands.LoginAsync()` 已提供真實互動流程：
  - 顯示 `This will write Provider.ApiKey to appsettings.secret.json in the current execution directory.`
  - 顯示 `Enter your NVIDIA NIM API Key:`
  - 使用 `ReadPassword()` 攔截輸入，不直接明文回顯
- 另已補 `auth login --api-key <key>` 非互動路徑。
- `MockNimProviderTests` 已使用本機 mock NIM server 驗證 `--api-key` 成功登入與模型驗證流程。

## 成功 / 失敗

成功

## 失敗原因

無

## 後續建議

- 若後續有正式安全測試 key，可再補對真實 NIM 的 login success transcript。
