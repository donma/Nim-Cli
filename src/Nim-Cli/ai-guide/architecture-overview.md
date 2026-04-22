# 架構總覽

## 產品形態

目前產品由兩個入口組成：

- `Nim-Cli`
- `NimTui.App`

兩者共用同一套核心能力，而不是平行實作。

## 共享核心

### 主要核心元件

- `AgentOrchestrator`
- `ContextBuilder`
- `PromptBuilder`
- `CommandIntentResolver`
- `ToolRegistry`
- `ToolPolicyService`
- `ProviderRouter`
- `CodingPipeline`
- `SessionState`
- `SessionManager`

### 共享命令服務

- `DoctorCommandService`
- `PlanCommandService`
- `InteractiveCommandService`
- `CompatibilityCommandService`
- `RegistryCommandService`
- `WorkspaceCommandService`
- `McpCommandService`

## 大方向資料流

### CLI / TUI

1. 讀取輸入
2. 建立/恢復 session
3. 路由到 command service 或 orchestrator
4. 由共享 policy / registry / provider / coding pipeline 執行
5. 以共享 summary/result model 回傳

### 自然語言任務

1. `CommandIntentResolver` 做 intent 判斷
2. `ContextBuilder` 收集 repo/tool/session context
3. `PromptBuilder` 組 prompt
4. `AgentOrchestrator` 呼叫 provider
5. 若需要 tool，經 `ToolRegistry` + `ToolPolicyService` 執行
6. 產出共享 `ExecutionSummary`

## BigPhase5 補強重點

- `ContextBuilder` 已補上 analysis / coding / ops strategy 與 token budget 截斷
- `ToolPolicyService` 已補上 dry-run、masked input summary、policy audit entry、override
- `SessionState` 已補上 current task、recent actions、policy audit trail
- `NimTui.App` 已補上 opening、layout、palette、approval dialog 與 recent summary

## 狀態與儲存

- config：execution directory 下 `appsettings*.json`
- runtime/session/checkpoint：execution directory 下 `.nim-cli-runtime/`
- memory：workspace 中的 `Nim.md`

## 第三輪新增重點

- root `workspace` / `compatibility` / `update`
- 共享 command catalog
- policy detail model
- coding workflow structured result
- 更多 smoke reports 與 unit tests
