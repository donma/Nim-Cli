# 功能名稱

doctor

## 測試目的

確認 `nim-cli doctor` 可在目前執行目錄下輸出環境檢查摘要，且不依賴互動輸入。

## 前置條件

- 專案可成功建置
- 不要求真實 API key

## 測試步驟

1. 執行 `nim-cli doctor`
2. 檢查是否輸出工作目錄、config 狀態、工具檢查與 provider 狀態欄位

## 實際指令

```powershell
dotnet test "tests/NimCli.Integration.Tests/NimCli.Integration.Tests.csproj"
```

對應 smoke case：`Doctor_Command_Returns_Success`

## 預期結果

- 指令成功
- exit code 為 0
- 不洩漏 secret

## 實際結果

- 測試通過
- `doctor` 指令 smoke case 成功

## 成功 / 失敗

成功

## 失敗原因

- 無

## 後續建議

- 後續可再增加真實 provider health / model existence 的受控測試案例
