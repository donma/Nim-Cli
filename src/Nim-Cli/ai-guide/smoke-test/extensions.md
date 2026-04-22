# 功能名稱

extensions

## 測試目的

確認 extensions registry 已可 list / describe，且不再回 placeholder 文案。

## 前置條件

- 專案可成功建置

## 測試步驟

1. 執行 `nim-cli extensions list`
2. 執行 `nim-cli extensions describe`

## 實際指令

```powershell
dotnet test "tests/NimCli.Integration.Tests/NimCli.Integration.Tests.csproj"
```

對應 smoke cases：

- `Extensions_List_Command_Returns_Success`
- `Extensions_Describe_Command_Returns_Success`

## 預期結果

- 指令成功
- 可看到 extension registry 資訊

## 實際結果

- 對應 smoke cases 通過

## 成功 / 失敗

成功

## 失敗原因

- 無

## 後續建議

- 後續補本機 manifest 驗證與 capability 掛載測試
