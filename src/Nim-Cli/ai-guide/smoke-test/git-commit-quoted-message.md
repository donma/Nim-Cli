# git-commit-quoted-message smoke test

## 目的

- 驗證 quoted message / user apostrophe 類內容在 shell builder 路徑下仍可安全 quoting。

## 觀察

- `PowerShellCommandBuilder.QuoteLiteral(...)` 持續保留單引號 escape 行為。
- shell tokenization 與 git command builder 現在觀點一致。
