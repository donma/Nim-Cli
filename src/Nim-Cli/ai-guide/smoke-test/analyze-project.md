# 功能名稱

`analyze-project`

## 測試目的

確認 `nim-cli analyze` 會產出專案分析輸出，而不是單純顯示說明文字。

## 前置條件

- 工作目錄：`D:\AI_PROJECTS\Nim-Cli\Nim-Cli`
- 分析目錄：`src`

## 測試步驟

1. 執行 `nim-cli analyze --directory src`。
2. 確認輸出包含 repo map/分析內容。

## 實際指令

```powershell
dotnet run --project "src/Nim-Cli/Nim-Cli.csproj" -- analyze --directory "src"
```

## 預期結果

- 成功輸出專案分析內容。
- 不為 placeholder。

## 實際結果

- 指令成功完成。
- 輸出包含 `# Repo Map: src` 與多個 project/file/type 摘要。
- 目前 analyze 以 repo map 為核心輸出，已可日常用於快速盤點專案。

## 成功 / 失敗

成功

## 失敗原因

無

## 後續建議

- 後續可再補 `--build true` 路徑的 smoke case，讓 analyze 同時覆蓋 build verify。
