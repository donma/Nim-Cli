# 功能名稱

commands

## 測試目的

確認 `commands` 命令已輸出真實命令矩陣，而不是只回一段靜態說明。

## 前置條件

- 專案可成功建置

## 測試步驟

1. 執行 `nim-cli commands`
2. 檢查指令成功

## 實際指令

```powershell
dotnet test "tests/NimCli.Integration.Tests/NimCli.Integration.Tests.csproj"
```

對應 smoke case：`Commands_Command_Returns_Success`

## 預期結果

- 指令成功
- exit code 為 0

## 實際結果

- 測試通過

## 成功 / 失敗

成功

## 失敗原因

- 無

## 後續建議

- 後續可再把 handler / capability 對照表輸出做更細分層
