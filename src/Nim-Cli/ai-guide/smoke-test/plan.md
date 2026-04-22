# 功能名稱

plan

## 測試目的

確認 `nim-cli plan "<task>"` 會輸出結構化規劃，而不是只切模式或回 placeholder。

## 前置條件

- 專案可成功建置

## 測試步驟

1. 執行 `nim-cli plan improve doctor output`
2. 檢查輸出中是否包含 impact files 與 verify strategy

## 實際指令

```powershell
dotnet test "tests/NimCli.Integration.Tests/NimCli.Integration.Tests.csproj"
```

對應 smoke case：`Plan_Command_With_Task_Returns_Success`

## 預期結果

- 指令成功
- exit code 為 0
- 輸出結構化規劃內容

## 實際結果

- 測試通過
- `plan` 指令 smoke case 成功

## 成功 / 失敗

成功

## 失敗原因

- 無

## 後續建議

- 後續可增加對計畫內容欄位的更細整合測試
