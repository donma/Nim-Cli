# 功能名稱

`db-query`

## 測試目的

確認 `nim-cli db query` 在缺少 connection 情境下，能用受控錯誤方式回報 usage，而不是錯誤執行寫入或崩潰。

## 前置條件

- 工作目錄：`D:\AI_PROJECTS\Nim-Cli\Nim-Cli`
- 未提供 DB connection

## 測試步驟

1. 執行 `nim-cli db query --table Users`。
2. 確認輸出 usage 並結束。

## 實際指令

```powershell
dotnet run --project "src/Nim-Cli/Nim-Cli.csproj" -- db query --table Users
```

## 預期結果

- 指令拒絕執行。
- 輸出正確 usage，引導使用者提供 read-only 連線。

## 實際結果

- CLI 輸出：`Usage: nim-cli db query --conn <alias|connection-string> [--query <sql> | --table <name> --where <clause>] [--type sqlserver|sqlite] [--top <n>]`
- 屬於受控失敗，不是崩潰。

## 成功 / 失敗

成功

## 失敗原因

無

## 後續建議

- 後續可補 sqlite 測試 DB 的真正 read-only query smoke case。
