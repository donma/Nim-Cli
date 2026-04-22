# 功能名稱

`coding-pipeline-integration`

## 測試目的

確認 coding pipeline 已具備 project-level integration 證據，而不只是單元級 patch 測試。

## 前置條件

- `CodingPipeline.ExecuteEditWorkflowAsync(...)`

## 測試資料

- 臨時專案目錄
- `Worker.cs`
- fake shell provider

## 測試步驟

1. 建立臨時專案資料夾與目標檔案
2. 執行 coding pipeline workflow
3. 驗證 build / test 驗證鏈與 summary

## 實際命令

```powershell
dotnet test "tests/NimCli.Integration.Tests/NimCli.Integration.Tests.csproj"
```

## 預期結果

- `Plan -> edit -> build -> test -> summarize` 成立

## 實際結果

- `CodingPipelineIntegrationTests` 通過

## 成功 / 失敗

成功

## 是否 dry-run

否

## policy / risk 說明

- 本測試不接真實外部資源
- 著重 orchestration correctness

## 後續建議

- 後續補真實 solution 級 integration 測試
