# 功能名稱

`screenshot`

## 測試目的

確認 `nim-cli screenshot` 已具備真實截圖能力。

## 前置條件

- 工作目錄：`D:\AI_PROJECTS\Nim-Cli\Nim-Cli`
- 已安裝 Playwright Chromium runtime

## 測試步驟

1. 執行 `nim-cli screenshot --url https://example.com --out .tmp-bigphase3-shot.png`。
2. 確認輸出截圖檔案路徑。

## 實際指令

```powershell
dotnet run --project "src/Nim-Cli/Nim-Cli.csproj" -- screenshot --url "https://example.com" --out ".tmp-bigphase3-shot.png"
```

## 預期結果

- 成功輸出截圖檔案。
- 不崩潰。

## 實際結果

- CLI 輸出 `Screenshot saved to: D:\AI_PROJECTS\Nim-Cli\Nim-Cli\.tmp-bigphase3-shot.png`。
- 成功產生截圖檔。

## 成功 / 失敗

成功

## 失敗原因

無

## 後續建議

- 後續可補不同 viewport 與 full page 模式。
