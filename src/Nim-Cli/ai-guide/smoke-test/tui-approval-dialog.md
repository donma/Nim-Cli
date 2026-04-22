# 功能名稱

`tui-approval-dialog`

## 測試目的

確認 TUI approval dialog 已顯示 tool、risk、dry-run、reason、input summary 與 allow/deny/details 選項。

## 前置條件

- `TuiApplication.FormatApprovalDialog(...)` 已實作

## 測試資料

- `Tui_Approval_Dialog_Shows_Risk_DryRun_And_Input_Summary`

## 測試步驟

1. 執行 `tests/NimTui.Tests`
2. 確認 approval dialog 測試通過

## 實際命令

```powershell
dotnet test "tests/NimTui.Tests/NimTui.Tests.csproj"
```

## 預期結果

- approval dialog 不再只是 `[y/N]`

## 實際結果

- 對應測試通過

## 成功 / 失敗

成功

## 是否 dry-run

否

## policy / risk 說明

- 驗證的是 approval UX 本身；實際高風險執行仍由 policy 決定

## 後續建議

- 後續可補 allow/deny/details 的鍵盤互動測試
