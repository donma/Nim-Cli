# context-long-task smoke test

## 目的

- 驗證長任務下 `ContextBuilder` 會優先保留 task / recent / policy / conversation，而不是最後整包硬切。

## 觀察

- `Current Task`
- `Recent Conversation`
- `Policy Audit`
- `Context Audit`

都可在受限 budget 下保留或以摘要形式呈現。
