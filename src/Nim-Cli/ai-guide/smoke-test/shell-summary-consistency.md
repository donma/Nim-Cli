# shell-summary-consistency smoke test

## 目的

- 驗證 session / summary 會記錄更清楚的 shell context label，而不是只剩模糊 tool name。

## 觀察

- `run_shell raw=...`
- `run_project project=... args=...`

能在 summary / context 中直接看出類型與主要輸入。
