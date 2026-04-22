# 功能名稱

`session-resume-workflow`

## 測試目的

確認中斷後 resume，current task/context/policy audit 仍可恢復。

## 前置條件

- `LongRunWorkflowTests.Session_Resume_Workflow_Persists_Current_Task_Context_And_Policy_Audit`

## 測試步驟

1. 執行 integration tests
2. 確認 session resume workflow 測試通過

## 實際命令

```powershell
dotnet test "tests/NimCli.Integration.Tests/NimCli.Integration.Tests.csproj"
```

## 預期結果

- `CurrentTask` / `LastContextStrategy` / `RecentActions` / `PolicyAuditTrail` 可 roundtrip

## 實際結果

- 對應測試通過

## 成功 / 失敗

成功

## 是否 dry-run

否

## policy / risk 說明

- audit trail 會被一併保留

## 後續建議

- 後續可補更多 checkpoint + resume 壓力測試
