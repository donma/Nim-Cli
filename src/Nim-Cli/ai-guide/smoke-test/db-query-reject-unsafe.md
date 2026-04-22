# db-query-reject-unsafe smoke test

## 目的

- 驗證危險 pattern 與 readonly-boundary bypass pattern 會被拒絕。

## 觀察

- blocked keyword
- comments / multiple statements
- `OPENROWSET` / `OPENDATASOURCE` / `APPLY` 類模式

都會在進 DB execution 前被擋下。
