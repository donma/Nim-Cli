# context-priority smoke test

## 目的

- 驗證高優先 block 會先進 prompt，低優先大內容會先被壓縮或略過。

## 觀察

- `Current Task`、`Recent Actions` 比大段 `Repo Map` 更穩定地保留。
- 低優先長內容優先被 multiline compaction。
