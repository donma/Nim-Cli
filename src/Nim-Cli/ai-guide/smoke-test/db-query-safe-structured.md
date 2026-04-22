# db-query-safe-structured smoke test

## 目的

- 驗證 structured mode 仍是 DB tool 的預設安全子集。

## 觀察

- `table / columns / where / top_n` 受控
- summary 會記錄 `structured:<table> ...`
