# 功能名稱

skills

## 測試目的

確認 skills registry 已可 list / describe，具備基本日常管理能力。

## 前置條件

- 專案可成功建置

## 測試步驟

1. 執行 `nim-cli skills list`
2. 執行 `nim-cli skills describe`

## 實際指令

```powershell
dotnet test "tests/NimCli.Integration.Tests/NimCli.Integration.Tests.csproj"
```

對應 smoke cases：

- `Skills_List_Command_Returns_Success`
- `Skills_Describe_Command_Returns_Success`

## 預期結果

- 指令成功
- 可看到 skill registry 資訊

## 實際結果

- 對應 smoke cases 通過

## 成功 / 失敗

成功

## 失敗原因

- 無

## 後續建議

- 後續補 skill 套用到 prompt/tool preset 的實測報告
