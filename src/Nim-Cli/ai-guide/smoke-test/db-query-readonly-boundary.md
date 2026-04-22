# db-query-readonly-boundary smoke test

## 目的

- 驗證 raw mode 不是 read-only 邊界的豁免權。

## 觀察

- `raw_mode=true` 只是 explicit acknowledgement
- readonly-boundary bypass pattern 仍會被拒絕
- `raw_mode=true` 若沒有 explicit raw query 也會被拒絕
