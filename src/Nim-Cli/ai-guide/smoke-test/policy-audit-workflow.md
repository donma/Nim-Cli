# 功能名稱

`policy-audit-workflow`

## 測試目的

確認高風險 workflow 下，policy decision 與 dry-run 可被審計。

## 前置條件

- `LongRunWorkflowTests.High_Risk_Approval_Workflow_Produces_Audit_Trail_With_DryRun`

## 測試步驟

1. 執行 integration tests
2. 確認 git push / ftp upload 對應 policy dry-run 測試通過

## 實際命令

```powershell
dotnet test "tests/NimCli.Integration.Tests/NimCli.Integration.Tests.csproj"
```

## 預期結果

- 高風險行為顯示 `Ask` 且 `DryRun=true`

## 實際結果

- 對應測試通過

## 成功 / 失敗

成功

## 是否 dry-run

是

## policy / risk 說明

- 本報告即為稽核證據的一部分

## 後續建議

- 後續補更多 tool override / global override scenario
