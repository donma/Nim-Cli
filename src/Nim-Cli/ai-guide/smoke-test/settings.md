# 功能名稱

settings

## 測試目的

確認 `settings show` 可正確輸出執行期設定內容。

## 前置條件

- 專案可成功建置

## 測試步驟

1. 執行 `nim-cli settings show`

## 實際指令

```powershell
dotnet test "tests/NimCli.Integration.Tests/NimCli.Integration.Tests.csproj"
```

對應 smoke case：`Settings_Show_Command_Returns_Success`

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

- 後續補 `settings set` 的 smoke report
