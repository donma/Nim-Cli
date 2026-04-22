# 功能名稱

ftp-upload

## 測試目的

確認 `ftp upload` 具備受控 smoke test 模式，不會直接打正式站。

## 前置條件

- 專案可成功建置
- 使用 dry-run 測試模式

## 測試步驟

1. 執行 `nim-cli ftp upload --dry-run`
2. 確認不需要互動 approval 即可完成 smoke test
3. 確認不會對真實 FTP 主機送檔

## 實際指令

```powershell
dotnet test "tests/NimCli.Integration.Tests/NimCli.Integration.Tests.csproj"
```

對應 smoke case：`Ftp_Upload_Dry_Run_Command_Returns_Success`

## 預期結果

- 指令成功
- exit code 為 0
- 明確為 dry-run 模式

## 實際結果

- 測試通過
- dry-run 路徑成功，不會發送真實 FTP 上傳

## 成功 / 失敗

成功

## 失敗原因

- 無

## 後續建議

- 後續可補安全測試 FTP 站的整合測試案例
