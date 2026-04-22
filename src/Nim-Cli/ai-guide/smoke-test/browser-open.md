# 功能名稱

`browser-open`

## 測試目的

確認 `nim-cli browser open` 可成功建立 headless browser session。

## 前置條件

- 工作目錄：`D:\AI_PROJECTS\Nim-Cli\Nim-Cli`
- 已成功執行 `nim-cli playwright install chromium`

## 測試步驟

1. 執行 `nim-cli browser open`。
2. 確認成功建立 browser session。

## 實際指令

```powershell
dotnet run --project "src/Nim-Cli/Nim-Cli.csproj" -- browser open
```

## 預期結果

- 成功建立 browser session。
- 不出現未處理例外。

## 實際結果

- 指令成功完成。
- CLI 輸出：`Browser session opened (1440x900).`
- 表示 `BrowserOpenTool` 與 Playwright Chromium runtime 已正常打通。

## 成功 / 失敗

成功

## 失敗原因

無

## 後續建議

- 後續可補 viewport 自訂參數 smoke case。
