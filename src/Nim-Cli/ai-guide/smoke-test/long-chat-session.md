# 功能名稱

`long-chat-session`

## 測試目的

確認多輪 chat session 下 transcript 與 recent actions 仍穩定。

## 前置條件

- `LongRunWorkflowTests.Long_Chat_Session_Maintains_Transcript_And_Recent_Actions`

## 測試步驟

1. 執行 integration tests
2. 確認長 chat session 測試通過

## 實際命令

```powershell
dotnet test "tests/NimCli.Integration.Tests/NimCli.Integration.Tests.csproj"
```

## 預期結果

- transcript 與 recent actions 維持穩定

## 實際結果

- 對應測試通過

## 成功 / 失敗

成功

## 是否 dry-run

否

## policy / risk 說明

- 無高風險操作

## 後續建議

- 後續可補真實 provider + tools 的更長對話鏈
