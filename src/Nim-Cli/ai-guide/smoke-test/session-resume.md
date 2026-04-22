# 功能名稱

session-resume

## 測試目的

確認 `session resume` 在沒有 session 時會給出可控錯誤，不會 crash。

## 前置條件

- 專案可成功建置

## 測試步驟

1. 執行 `nim-cli session resume`

## 實際指令

```powershell
dotnet test "tests/NimCli.Integration.Tests/NimCli.Integration.Tests.csproj"
```

對應 smoke case：`Session_Resume_Command_Returns_Error_When_NoSessionExists`

## 預期結果

- 指令結束
- 以可預期錯誤碼回傳

## 實際結果

- 對應 smoke case 通過

## 成功 / 失敗

成功

## 失敗原因

- 無

## 後續建議

- 後續補有真實 session 時的 resume smoke report
