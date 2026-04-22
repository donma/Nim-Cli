# 功能名稱

`repo-map`

## 測試目的

確認 `nim-cli repo map` 已具備 aider-style repo map 輸出能力。

## 前置條件

- 工作目錄：`D:\AI_PROJECTS\Nim-Cli\Nim-Cli`
- 分析目錄：`src`

## 測試步驟

1. 執行 `nim-cli repo map --directory src`。
2. 確認輸出包含專案與主要類別摘要。

## 實際指令

```powershell
dotnet run --project "src/Nim-Cli/Nim-Cli.csproj" -- repo map --directory "src"
```

## 預期結果

- 輸出 repo map。
- 至少列出 `Nim-Cli.csproj` 與主要命令/服務類別。

## 實際結果

- 指令成功完成。
- 輸出 `# Repo Map: src`。
- 列出 `Nim-Cli\CliApplication.cs`、`InteractiveCommandService.cs`、`CompatibilityCommandService.cs` 等主要檔案與型別。

## 成功 / 失敗

成功

## 失敗原因

無

## 後續建議

- 後續可補 max files / filtering 行為測試。
