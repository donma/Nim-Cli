# 功能名稱

tui-doctor

## 測試目的

確認 TUI doctor 路徑仍建立在共享 service 與共享 session summary 模型上。

## 前置條件

- 專案可成功建置

## 測試步驟

1. 執行 TUI smoke tests
2. 確認 TUI shared summary 測試仍通過，未因第三輪 command / policy 變更回歸

## 實際指令

```powershell
dotnet test "tests/NimTui.Tests/NimTui.Tests.csproj"
```

## 預期結果

- TUI smoke tests 成功

## 實際結果

- `NimTui.Tests` 通過

## 成功 / 失敗

成功

## 失敗原因

- 無

## 後續建議

- 後續補 TUI 中 doctor 直接 command flow 的專屬測試
