# 功能名稱

tui-plan

## 測試目的

確認 TUI 透過共享 `InteractiveCommandService` 執行 `/plan <task>` 時，可得到結構化規劃結果。

## 前置條件

- 專案可成功建置

## 測試步驟

1. 執行 TUI smoke test
2. 驗證共享 slash command service 對 `/plan add doctor summary` 回傳有 impact files 與 verify strategy

## 實際指令

```powershell
dotnet test "tests/NimTui.Tests/NimTui.Tests.csproj"
```

對應 smoke case：`Shared_Interactive_Command_Service_Handles_Plan_Task`

## 預期結果

- 測試成功
- TUI plan 路徑與 CLI 共用同一套規劃 service

## 實際結果

- 測試通過

## 成功 / 失敗

成功

## 失敗原因

- 無

## 後續建議

- 後續可補 TUI build / screenshot / doctor 的直接 smoke case
