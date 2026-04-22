# 功能名稱

`long-coding-workflow`

## 測試目的

確認 long coding workflow 可穩定跑過 analyze/plan/edit/build/test/summarize 鏈路。

## 前置條件

- `CodingPipelineIntegrationTests`

## 測試步驟

1. 執行 integration tests
2. 確認 coding pipeline project-level workflow 通過

## 實際命令

```powershell
dotnet test "tests/NimCli.Integration.Tests/NimCli.Integration.Tests.csproj"
```

## 預期結果

- 長 coding workflow 成功完成

## 實際結果

- 對應測試通過

## 成功 / 失敗

成功

## 是否 dry-run

否

## policy / risk 說明

- 本測試聚焦 orchestration correctness

## 後續建議

- 後續可補真實 solution 級鏈路
