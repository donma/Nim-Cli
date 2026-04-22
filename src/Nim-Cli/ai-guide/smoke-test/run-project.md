# 功能名稱

`run-project`

## 測試目的

確認 `nim-cli run-project` 已具備實際執行能力，且可把參數傳給目標專案。

## 前置條件

- 工作目錄：`D:\AI_PROJECTS\Nim-Cli\Nim-Cli`
- 使用 `src/Nim-Cli/Nim-Cli.csproj` 作為 smoke 目標

## 測試步驟

1. 執行 `nim-cli run-project --project src/Nim-Cli/Nim-Cli.csproj --args "--help"`。
2. 確認目標專案被執行並輸出 CLI help。

## 實際指令

```powershell
dotnet run --project "src/Nim-Cli/Nim-Cli.csproj" -- run-project --project "src/Nim-Cli/Nim-Cli.csproj" --args "--help"
```

## 預期結果

- 指令成功執行目標專案。
- 輸出 Nim-Cli help，而不是 shell 錯誤或 placeholder。

## 實際結果

- 指令成功完成。
- 輸出 `Nim-CLI - Terminal Agent powered by NVIDIA NIM` 與完整 help 清單。
- 顯示 `run-project` 已接上實際 `dotnet run` 路徑。

## 成功 / 失敗

成功

## 失敗原因

無

## 後續建議

- 後續可新增針對示範 console app 的 smoke case，驗證一般專案啟動而不只是 help 輸出。
