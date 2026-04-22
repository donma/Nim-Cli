# 功能名稱

`tui-model-switch`

## 測試目的

確認 TUI header / opening / shared model summary 已能反映目前 model。

## 前置條件

- `TuiApplication.RenderOpeningFrame(...)`

## 測試資料

- 預設 `NimCliOptions.Provider.DefaultModel`

## 測試步驟

1. 執行 TUI tests
2. 確認 opening frame 測試通過

## 實際命令

```powershell
dotnet test "tests/NimTui.Tests/NimTui.Tests.csproj"
```

## 預期結果

- frame 顯示目前 model

## 實際結果

- 對應測試通過

## 成功 / 失敗

成功

## 是否 dry-run

否

## policy / risk 說明

- 無高風險操作

## 後續建議

- 後續可補真實 `/model set` 後 header 更新的互動測試
