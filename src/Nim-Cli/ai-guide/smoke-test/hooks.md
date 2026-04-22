# 功能名稱

hooks

## 測試目的

確認 hooks registry 已可 list / describe，不再只是空殼。

## 前置條件

- 專案可成功建置

## 測試步驟

1. 執行 `nim-cli hooks list`
2. 執行 `nim-cli hooks describe`

## 實際指令

```powershell
dotnet test "tests/NimCli.Integration.Tests/NimCli.Integration.Tests.csproj"
```

對應 smoke cases：

- `Hooks_List_Command_Returns_Success`
- `Hooks_Describe_Command_Returns_Success`

## 預期結果

- 指令成功
- 可看到 hooks 狀態資訊

## 實際結果

- 對應 smoke cases 通過

## 成功 / 失敗

成功

## 失敗原因

- 無

## 後續建議

- 後續補 pre-command / post-command phase model 與持久化行為測試
