# 功能名稱

`browser-screenshot`

## 測試目的

確認 `nim-cli screenshot` 可真正開頁並輸出截圖檔案。

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

- 成功建立截圖檔案。
- 輸出保存路徑。

## 實際結果

- 指令成功完成。
- CLI 輸出：`Screenshot saved to: D:\AI_PROJECTS\Nim-Cli\Nim-Cli\.tmp-bigphase3-shot.png`
- 實際產出截圖檔案。

## 成功 / 失敗

成功

## 失敗原因

無

## 後續建議

- 後續可補 full-page / wait-second 參數 smoke case。
