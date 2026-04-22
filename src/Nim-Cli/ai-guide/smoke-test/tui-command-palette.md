# 功能名稱

`tui-command-palette`

## 測試目的

確認 TUI command palette 已包含 BigPhase5 要求的高頻工作流入口。

## 前置條件

- `TuiApplication.FormatCommandPalette()` 已實作

## 測試資料

- `Tui_Command_Palette_Contains_High_Frequency_Actions`

## 測試步驟

1. 執行 `tests/NimTui.Tests`
2. 確認 palette 測試通過

## 實際命令

```powershell
dotnet test "tests/NimTui.Tests/NimTui.Tests.csproj"
```

## 預期結果

- 至少包含 Build / Run Project / Screenshot / Policy Summary / FTP Upload

## 實際結果

- 對應測試通過

## 成功 / 失敗

成功

## 是否 dry-run

否

## policy / risk 說明

- 本報告只驗證 palette 顯示，不實際執行高風險命令

## 後續建議

- 後續可補 palette 選擇互動自動化測試
