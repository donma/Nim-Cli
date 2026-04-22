# 功能名稱

compatibility

## 測試目的

確認 `compatibility` root command 已可用，能輸出兼容對位摘要。

## 前置條件

- 專案可成功建置

## 測試步驟

1. 執行 `nim-cli compatibility`
2. 檢查指令成功結束

## 實際指令

```powershell
dotnet test "tests/NimCli.Integration.Tests/NimCli.Integration.Tests.csproj"
```

對應 smoke case：`Compatibility_Command_Returns_Success`

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

- 後續可補更詳細的 Gemini / Copilot / aider 對位欄位
