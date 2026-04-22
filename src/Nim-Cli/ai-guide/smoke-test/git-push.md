# 功能名稱

git-push

## 測試目的

確認 `git push` 具備受控 smoke test 模式，不會直接推送真遠端。

## 前置條件

- 專案可成功建置
- 使用 dry-run 測試模式

## 測試步驟

1. 執行 `nim-cli git push --dry-run`
2. 確認不需要互動 approval 即可完成 smoke test
3. 確認不會推送真遠端

## 實際指令

```powershell
dotnet test "tests/NimCli.Integration.Tests/NimCli.Integration.Tests.csproj"
```

對應 smoke case：`Git_Push_Dry_Run_Command_Returns_Success`

## 預期結果

- 指令成功
- exit code 為 0
- 明確為 dry-run 模式

## 實際結果

- 測試通過
- dry-run 路徑成功，不會推送真遠端

## 成功 / 失敗

成功

## 失敗原因

- 無

## 後續建議

- 後續可加一個假 remote repo 的整合測試案例
