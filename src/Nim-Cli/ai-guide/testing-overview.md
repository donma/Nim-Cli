# 測試總覽

## 目前測試專案

- `tests/NimCli.Core.Tests`
- `tests/NimCli.Integration.Tests`
- `tests/NimTui.Tests`

## 目前已驗證內容

### Core unit tests

- `CommandIntentResolver`
- `ToolPolicyService`
- `CodingPipeline` 結構化 workflow result
- `MCP` registration / invocation / stdio success-failure paths / command service registry summary
- BigPhase5 context strategy / policy audit / TUI opening/palette/approval/layout

### CLI integration tests

- auth / models / settings / memory / session / commands / workspace / compatibility / update
- hooks / skills / extensions
- mcp list/tools/ping/inspect
- plan / doctor
- build / run-project / analyze / repo map
- browser open / screenshot / navigate
- git status / diff / push dry-run / commit approval
- provider success path（mock NIM）
- git push dry-run / ftp upload dry-run

### TUI smoke tests

- 共用 slash command planning flow
- 共用 session summary 欄位
- 共用 `/run` `/build` `/analyze` `/browser open` `/git status`
- opening / palette / approval dialog / layout state

## 目前已落 smoke reports

- auth-login
- auth-status
- models-list
- chat
- run
- doctor
- plan
- build
- run-project
- screenshot
- browser-open
- browser-screenshot
- analyze-project
- repo-map
- db-query
- ftp-upload
- git-status
- git-diff
- git-commit
- git-push
- session-show
- session-resume
- memory
- settings
- hooks
- agents
- skills
- extensions
- commands
- workspace
- compatibility
- restore
- rewind
- update
- mcp
- tui-chat
- tui-plan
- tui-doctor
- tui-build
- tui-screenshot
- tui-opening
- tui-run-project
- tui-analyze
- tui-command-palette
- tui-approval-dialog
- tui-session-resume
- tui-model-switch
- tui-tools-list
- tui-policy-summary

## 尚待補強

- 真實第三方 MCP compatibility integration
- 真實 DB read-only integration
- ContextBuilder / PromptBuilder / SessionManager / RepoMapBuilder 額外單元測試

## 測試原則

- 高風險命令預設採 dry-run 或安全測試模式
- 不輸出真實 API key 或敏感資訊
- 先以 smoke test 覆蓋命令面，再逐步加深 unit/integration 驗證
