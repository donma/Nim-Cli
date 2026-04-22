# shell-escaping smoke test

## 目的

- 驗證 `run-project` 類命令的 args 在 spaces / quoted text / semicolons 下仍走 builder。

## 觀察

- extra args 先 tokenize，再逐一 quote。
- `a;b` 會作為 literal arg 保留，不直接變成 command composition 缺口。
