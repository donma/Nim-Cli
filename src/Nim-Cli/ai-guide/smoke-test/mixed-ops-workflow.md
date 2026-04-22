# 功能名稱

`mixed-ops-workflow`

## 測試目的

確認 build / run / screenshot / db / git / plan / doctor 類混合工作流已有穩定入口與證據鏈。

## 前置條件

- 已有對應 smoke reports 與 integration tests

## 測試步驟

1. 檢視 BigPhase6 長期穩定度報告
2. 對照既有 build / screenshot / db / git / plan / doctor 證據

## 實際命令

```text
build.md + browser-screenshot.md + db-query.md + git-status.md + plan.md + doctor.md
```

## 預期結果

- mixed ops workflow 可被合理追蹤與證明

## 實際結果

- 證據鏈已可對照

## 成功 / 失敗

成功

## 是否 dry-run

部分為 dry-run

## policy / risk 說明

- 高風險部分維持 dry-run policy

## 後續建議

- 後續可補單一 scenario 自動化腳本
