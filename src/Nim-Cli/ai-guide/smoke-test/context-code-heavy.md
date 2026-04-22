# context-code-heavy smoke test

## 目的

- 驗證 code-heavy / repo-heavy 情境下，不會因單一長 block 把整個 context 切壞。

## 觀察

- multiline block 會保留前後緣與省略提示。
- `Context Audit` 可看出 compressed block 類型。
