# 功能名稱

`build`

## 測試目的

確認 `nim-cli build` 能透過共享 tool path 執行實際 `dotnet build`，而不是 placeholder。

## 前置條件

- 工作目錄：`D:\AI_PROJECTS\Nim-Cli\Nim-Cli`
- 專案檔存在：`src/Nim-Cli/Nim-Cli.csproj`

## 測試步驟

1. 執行 `nim-cli build --project src/Nim-Cli/Nim-Cli.csproj`。
2. 確認 build 成功完成。

## 實際指令

```powershell
dotnet run --project "src/Nim-Cli/Nim-Cli.csproj" -- build --project "src/Nim-Cli/Nim-Cli.csproj"
```

## 預期結果

- 成功呼叫 `dotnet build`。
- 輸出 `建置成功` 或等價成功訊息。

## 實際結果

- 指令成功完成。
- 輸出包含：`建置成功。`
- 同時顯示 `0 個警告`、`0 個錯誤`。

## 成功 / 失敗

成功

## 失敗原因

無

## 後續建議

- 後續可補 Release 組態 smoke case。
