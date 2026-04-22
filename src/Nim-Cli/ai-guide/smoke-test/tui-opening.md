# 功能名稱

`tui-opening`

## 測試目的

確認 TUI 已有 opening frame，並顯示產品名稱、provider、model、workspace 與 hint。

## 前置條件

- `TuiApplication.RenderOpeningFrame(...)` 可直接輸出 frame

## 測試資料

- 預設 `NimCliOptions`
- 臨時 `SessionState`

## 測試步驟

1. 執行 `tests/NimTui.Tests`
2. 確認 `Tui_Opening_Frame_Shows_Product_Provider_Model_And_Workspace` 通過

## 實際命令

```powershell
dotnet test "tests/NimTui.Tests/NimTui.Tests.csproj"
```

## 預期結果

- opening 內容包含 `Nim-Tui`、provider、model、workspace

## 實際結果

- 對應測試通過

## 成功 / 失敗

成功

## 是否 dry-run

否

## policy / risk 說明

- 無高風險操作

## 後續建議

- 後續可加入 reduced motion / skip opening 的自動化驗證
