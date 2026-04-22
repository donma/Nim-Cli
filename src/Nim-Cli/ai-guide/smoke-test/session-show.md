# 功能名稱

session-show

## 測試目的

確認 `session show` 可輸出目前 session 狀態。

## 前置條件

- 專案可成功建置

## 測試步驟

1. 執行 `nim-cli session show`

## 實際指令

```powershell
dotnet test "tests/NimCli.Integration.Tests/NimCli.Integration.Tests.csproj"
```

對應 smoke case：`Session_Show_Command_Returns_Success`

## 預期結果

- 指令成功

## 實際結果

- 測試通過

## 成功 / 失敗

成功

## 失敗原因

- 無

## 後續建議

- 後續補 session artifact/history 細節檢查
