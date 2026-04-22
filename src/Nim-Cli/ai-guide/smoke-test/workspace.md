# 功能名稱

workspace

## 測試目的

確認 `workspace` root command 已可用，能顯示目前 workspace 摘要，而非被誤判成 prompt。

## 前置條件

- 專案可成功建置

## 測試步驟

1. 執行 `nim-cli workspace show`
2. 檢查指令成功結束

## 實際指令

```powershell
dotnet test "tests/NimCli.Integration.Tests/NimCli.Integration.Tests.csproj"
```

對應 smoke case：`Workspace_Show_Command_Returns_Success`

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

- 後續補 `workspace switch` 的 smoke test 與 TUI 快速入口報告
