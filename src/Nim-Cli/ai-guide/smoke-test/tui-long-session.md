# 功能名稱

`tui-long-session`

## 測試目的

確認 TUI 在長 session render 下仍能穩定顯示 transcript / status / recent actions。

## 前置條件

- `TuiLongRunTests.Tui_Long_Session_Render_Remains_Stable_With_Many_Actions`

## 測試步驟

1. 執行 TUI tests
2. 確認 long session render 測試通過

## 實際命令

```powershell
dotnet test "tests/NimTui.Tests/NimTui.Tests.csproj"
```

## 預期結果

- layout 與 recent actions 在長互動下仍可正常輸出

## 實際結果

- 對應測試通過

## 成功 / 失敗

成功

## 是否 dry-run

否

## policy / risk 說明

- 無高風險操作

## 後續建議

- 後續補更長時間 UI soak test
