# BigPhase1-9 總整理

## 說明

- 本文件整理 `ai-guide` 中 BigPhase1~9 的演進脈絡、已完成項、驗證結果與審查結論。
- 本文件已吸收各 phase 的主文件、`progress`、`final summary`、`command matrix`、`CLI/TUI sync matrix`、`workflow comparison`、`g* cli comparison`、`superiority claim`、`gap list`、`audit`、`report` 等 `*phase*.md` 內容，因此可作為單一保留版本。
- `BigPhase1` 原始文件目前不在 `ai-guide` 內，因此本整理不會捏造其內容，只會明確標記缺口。
- 本文件中的 `g* cli` 用語，統一取代先前文件中的 `Gemini CLI` 提法。

## 整體結論

- `Nim-Cli` 的演進主線很清楚：先把 CLI / TUI / shared core 建起來，再補 compatibility、MCP、policy、context、summary、shell / DB safety，最後收尾到可審計、可長期運作、可高信心驗證的主幹。
- BigPhase2~6 比較偏產品能力、工作流覆蓋、UI / UX、比較矩陣與證據鏈擴張。
- BigPhase7~9 比較偏主幹觀點修正，把最後真正會扣分的工程成熟度問題收掉。
- 到 BigPhase9 後，主幹已來到更接近「可高信心主張整體超過 g* cli」的狀態。

## BigPhase1

- `ai-guide` 內目前找不到 `BigPhase1` 原始文件。
- 目前無法只靠現有檔案精準回填它的目標、完成項與驗證數字。
- 若要補齊 BigPhase1，下一步應從 git 歷史或外部備份回溯，而不是在總結文件中硬猜。

## BigPhase2

- 產品定位從單一 CLI 工具，推進成 `CLI + TUI + shared core` 的雙前端架構。
- 目標明確設定為先追到接近 `g* cli` 的 80% 可用度。
- 這一輪建立了 `Nim-Cli` 與 `Nim-Tui` 的基本產品形狀，並強調 core / tools / provider / policy / session / coding pipeline 必須共用。
- `BigPhase2.md` 也是後面所有 phase 的共同起點：之後的補強基本都建立在這個 shared-core 架構上。
- 驗證結果：solution build 通過，CLI integration `43 passed, 0 failed`，TUI smoke `2 passed, 0 failed`。
- 測試重點：`doctor`、`plan <task>`、DB table-query failure path、shared slash-command planning flow、shared session summary fields used by TUI。

## BigPhase3

- 補齊 root command 與 compatibility 命令矩陣。
- 補上 `workspace`、`compatibility`、`update`、更完整的 `restore` / `rewind` checkpoint recovery。
- TUI 開始共享更多 slash command，例如 `/build`、`/run-project`、`/screenshot`、`/analyze`。
- 補 auth / models / run -p 的成功路徑與 mock NIM 驗證。
- 這一輪也把命令面盤清楚攤開：`auth`、`models`、`chat`、`run`、`doctor`、`plan`、`session`、`build`、`run-project`、`screenshot`、`browser`、`analyze`、`repo map`、`db query`、`ftp upload`、`git`、`workspace`、`compatibility`、`vim`、`restore`、`rewind`、`update`、`memory`、`settings`、`compress`、`stats`、`mcp` 等都已進入可驗收矩陣。
- CLI / TUI 共用核心在這時已明確成形：`AgentOrchestrator`、`ContextBuilder`、`ToolRegistry`、`ToolPolicyService`、`ProviderRouter`、`CodingPipeline`、`SessionState`、`SessionManager`、`InteractiveCommandService`、`DoctorCommandService`、`PlanCommandService`、`ExecutionSummaryFormatter`。
- 當時仍明確列出的缺口，是 `ContextBuilder` token budget、per-tool policy override、更完整 MCP smoke / verification，以及 TUI 高頻快捷入口與 approval 體驗。
- 最新驗證：Integration 66 passed，TUI 4 passed。

## BigPhase4

- 把 browser / screenshot 成功路徑補成真正可跑。
- `auth`、`auth login --api-key`、`auth status`、`models list` 的非互動與 mock 成功路徑補齊。
- TUI slash routing 更完整，覆蓋 `/run`、`/browser`、`/db`、`/git`、`/ftp`、`/settings`、`/permissions`、`/memory`。
- MCP 核心證據開始補齊，包含 stdio success/failure、registry/inspect/ping 等驗證。
- 最新驗證：Core 25 passed，Integration 66 passed，TUI 7 passed。
- 測試重點：mock NIM provider success path、shell build / run-project、browser open / navigate / screenshot、git status / diff / push dry-run / commit approval、recovery / session / resume、TUI shared orchestration、MCP command service registry / inspect / ping。
- 命令矩陣與 CLI/TUI sync matrix 的結論，已從 BigPhase3 的「部分同步」推進到「高頻能力大多已同步」，重點開始從能力補洞轉到 TUI 體驗優化。
- MCP 在這一輪已從 placeholder 提升成「可用且有證據」：已有 registration、invocation、status、inspect、ping、runtime registry，以及 controlled failure path。
- source tree / placeholder cleanup 也被納入本輪成果：移除 `src/NimCli.Mcp/Class1.cs`、修正 Playwright 路徑、修正 TUI/shared slash routing 重複 prepend，並把清理標準明確化。
- 比較矩陣與 superiority claim 的主結論是：高風險治理、recovery / session、aider 式 coding workflow、CLI/TUI 雙入口共享核心、證據鏈完整度，已足以開始合理主張部分面向超過 `g* cli`。

## BigPhase5

- `AgentOrchestrator`、`ContextBuilder`、`ToolPolicyService`、`ExecutionSummaryFormatter` 開始形成統一主幹。
- 新增 context strategy、policy audit、approval request、recent actions、dry-run summary 顯示等能力。
- TUI 從殼升級成真正 layout：opening、transcript pane、status pane、input pane、approval dialog、command palette、recent sessions 等狀態面板。
- 新增第三方 MCP compatibility integration、event-driven TUI 測試、coding pipeline integration 測試。
- 最新驗證：Core 28 passed，Integration 68 passed，TUI 13 passed。
- 測試重點：context strategy / current task / recent actions、policy audit / masked input summary / dry-run evidence、TUI opening / palette / approval dialog / layout state、mainline shared session summary、third-party MCP compatibility integration、coding pipeline project-level integration。
- 主幹穩定度報告把 mainline 明確收斂成：`CommandIntentResolver -> ContextBuilder -> PromptBuilder -> AgentOrchestrator -> ToolRegistry/ToolPolicyService -> ProviderRouter`，不再只是多套平行流程。
- `SessionState` / `SessionManager` 在這一輪開始承接 `current task`、`recent actions`、`policy audit`、`workspace` 與 session persistence，`ToolPolicyService` 也開始輸出 dry-run 與 masked input summary。
- TUI 不只變得更好看，而是成為可測的產品級入口：opening、palette、approval dialog、focus / mode / busy、recent sessions 都可透過 API 驗證 event-driven state transition。
- 第三方風格 stdio MCP 相容性已具受控 integration 證據；coding pipeline 也已具 project-level integration 證據，可驗證 `analyze -> plan -> edit -> build -> test -> summarize` 鏈路。
- 比較矩陣與超車主張在這一輪開始把焦點從「功能數量」轉成主幹穩定度、risk governance、aider workflow 收斂與 TUI UX 優勢。

## BigPhase6

- `SessionState` / `SessionManager` 持久化補齊 `current task`、`context strategy`、`recent actions`、`policy audit`。
- long-run workflow、long chat、mixed ops、session resume、CLI/TUI mixed、MCP long-run 證據鏈成形。
- 主幹穩定度、aider pipeline、TUI parity、MCP depth、comparison、superiority claim 文件全面補齊。
- 這一輪代表系統已不只是功能存在，而是開始證明它能長時間穩定地工作。
- 最新驗證：Core 30 passed，Integration 71 passed，TUI 15 passed。
- 驗證細節：`dotnet build "Nim-Cli.slnx"` 通過，且為 `0 warnings, 0 errors`。
- 測試重點：session persistence 擴充、long chat session、long coding workflow、session resume workflow、CLI / TUI mixed workflow、MCP compatibility / failure / recovery、TUI long session render 穩定度。
- 主幹最終稽核的結論是：`AgentOrchestrator`、`ContextBuilder`、`ToolPolicyService`、`SessionState` / `SessionManager` 已可被合理視為單一共享主流程，而不是鬆散功能集合。
- 長期穩定度報告把驗證擴到 8 類 scenario：長 chat、長 coding、mixed ops、session resume、CLI/TUI mixed、MCP long-run、高風險 approval、TUI long session。
- MCP 深度報告把子系統成熟度往前推一步：不只可用，還有 compatibility、failure / recovery、integration 證據，可視為成熟子系統的早期版本。
- TUI / CLI parity report 與 TUI UX report 的共同結論是：高頻工作流已以同步為主，且 TUI 進一步提供 palette、approval dialog、recent state 等純 CLI 沒有的效率優勢。
- 本輪比較矩陣與超車主張開始明確採用「105% g* cli」的說法，但主張基礎仍是可驗證的主幹、長跑穩定度、MCP 深度、aider pipeline 與證據完整度。

## BigPhase7

- 高風險工具的 `dry_run` 從 policy 標記變成真實執行語意。
- shell timeout / success semantics 修正，避免 timeout 被誤包裝成 success。
- PowerShell command pipeline 改成 `-EncodedCommand` 加 `PowerShellCommandBuilder` quoting。
- DB read-only 邊界補強，增加 multi-statement、comments、blocked keyword、structured `where` 限制。
- browser shared session 加上 serialize 保護。
- config / runtime store 從 current directory 漂移改成 user-level 基底。
- sync-over-async 主路徑移除，`BuildServicesAsync(...)` 與多個工具改為 async。
- 最新驗證：Core 38 passed，Integration 71 passed，TUI 15 passed。
- summary / audit 稽核結論：BigPhase5-6 已有骨架，BigPhase7 主要是把風險從「缺失」重新定性為「completeness」，CLI / TUI 都已接上 `ExecutionSummaryFormatter`，summary 已不再屬於硬傷。
- config 行為在這一輪正式定成：user-level app home 為穩定基底、workspace config 為 override，並支援 `NIMCLI_HOME` 作為隔離與測試控制點。
- startup 稽核的重點是把主路徑明顯的 sync-over-async 移掉，讓 CLI、TUI、MCP registration、patch verification、vim install plan 等都改走 async bootstrap / async verification。
- Browser stability report 的重點是 `BrowserSessionManager` 新增 `SemaphoreSlim` 與 serialize path，把 shared page race condition 從裸露狀態降為受控狀態。
- timeout report、shell safety report、dry-run audit 三者一起構成 BigPhase7 的可信度修正核心：timeout 一律失敗、`git push --dry-run` / FTP 模擬為真 dry-run、PowerShell transport 改走 `-EncodedCommand` 與 safer quoting。
- Context / DB 稽核的定位也更清楚：這一輪已從「幾乎沒有策略」進步到可信的安全子集與 truncation 策略，但仍未達完整 budget engine / 參數化模型，因此還保留後續 BigPhase8~9 的收尾空間。

## BigPhase8

- `ContextBuilder` 升級為 priority-based block budget。
- shell command composition 再結構化，`BuildProjectTool` 與 `RunProjectTool` 不再只靠 raw string route。
- DB structured / raw mode 邊界更清楚，`raw_mode=true` 變成 explicit advanced path。
- execution summary / audit 完整度再提升，新增 `ToolResultSummaries`、artifact / approval / policy 可見度。
- browser mixed workflow、config precedence 的證據再補強。
- 最新驗證：Core 47 passed，Integration 74 passed，TUI 20 passed。
- 驗證細節：`dotnet build "Nim-Cli.slnx"` 通過（`0 warnings, 0 errors`），`tests/NimCli.Core.Tests` `47 passed, 0 failed`，`tests/NimCli.Integration.Tests` `74 passed, 0 failed`，`tests/NimTui.Tests` `20 passed, 0 failed`。
- summary / audit 稽核重點：`ExecutionSummary` 新增 `ToolResultSummaries`，formatter 補進 repo map / build / test / commit suggestion artifacts、task / context output summary、approval action 與更完整 policy decision，summary 更接近可直接被 UI 與審查文件引用的資料模型。
- context 稽核的具體提升是 block model、priority ordering、strategy-aware budget、preserve-edges compaction、omitted block accounting，已從 BigPhase7 的「仍偏粗」收斂到真正 mode-aware 的 block-based context system。
- shell safety report 的具體提升是 command composition 本身也結構化：`BuildProjectTool` 改走 external command builder，`RunProjectTool` 改走 `ShellCommandComposer`，`run_shell` 則更誠實地被定義為 high-risk advanced path。
- DB safety report 把設計觀點講得更直白：structured mode 是受控安全子集，raw mode 是 explicit advanced mode，必須清楚 opt-in，不能再假裝兩者安全等價。
- config 行為報告與 browser stability report 的主軸較偏證據補強：multi-workspace precedence 與 mixed workflow / long session stability 現在都有更直接的 core / integration / TUI 測試支撐。
- 最終審查的總結是：BigPhase8 已把 BigPhase7 尚未完全收尾的主要工程觀點問題補到可交付狀態，剩下更多是產品 polish 或更大型 matrix 的問題。

## BigPhase9

- 只鎖三條主軸：`ContextBuilder`、shell command composition / safety、DB query boundary。
- `ContextBuilder` 再升級：新增 `tui-interactive` strategy、`Recent Conversation`、multiline compaction、`Context Audit`。
- shell 再升級：`RunProjectTool` 走 tokenization + quoted builder，新增 `PowerShellCommandBuilder.TokenizeArguments(...)`。
- DB 再硬化：`raw_mode=true` 只允許 explicit raw query、`top_n` 加範圍、readonly-boundary bypass pattern 額外封鎖。
- 直接相關的小修：TUI opening 增加停留時間；`CliSmokeTests` hang 修正。
- 修掉的 smoke hang 根因包括：`CliApplication.RunAsync(...)` 未釋放 service provider，以及 `unknown-command` 被誤判成單字 prompt。
- 最新驗證：Core 54 passed，Integration 76 passed，TUI 23 passed。
- context 稽核顯示這一輪的進步不只是再多幾個 block，而是讓 block 的保留、壓縮、捨棄規則更可審計、更可解釋；prompt 中已能看出 budget、included kinds、compressed blocks、omitted kinds。
- shell safety report 的重點是把 `run-project` extra args 從 raw string append 收斂成 tokenized / quoted pipeline，讓 spaces、quoted args、semicolon literal 都更可靠地落到 critical path。
- DB safety report 把 structured / raw / readonly boundary 再往前推一層，並把 mode evidence 寫進 session context / summary，讓 audit 能看出 `structured:<table>` 或 `raw:<query>` 的差異。
- 最終審查的定性是：BigPhase9 收的不是新功能，而是最後幾個仍可能讓審查者保留分數的工程成熟度點位，並把 shell / DB summary 與 audit 一致性一併補齊。

## 測試演進

- BigPhase2：Build passed，Integration 43，TUI 2。
- BigPhase3：Integration 66，TUI 4。
- BigPhase4：Core 25，Integration 66，TUI 7。
- BigPhase5：Core 28，Integration 68，TUI 13。
- BigPhase6：Core 30，Integration 71，TUI 15。
- BigPhase7：Core 38，Integration 71，TUI 15。
- BigPhase8：Core 47，Integration 74，TUI 20。
- BigPhase9：Core 54，Integration 76，TUI 23。

## 主幹演進摘要

- CLI / TUI：從 shared-core 雙前端起步，逐步補成可操作、可追蹤、可審批、可共享 slash routing 的終端工作流。
- Context：從單純拼接，進化到 strategy-aware、priority-aware、block-based、最後帶 audit 的 context composition。
- Policy / Approval：從基本風險分類，進化到 per-tool override、global override、dry-run semantics、policy audit trail。
- Shell：從脆弱字串拼接，進化到 encoded command、literal quoting、structured builder、tokenization 與更好的 audit consistency。
- DB：從基本 read-only 限制，進化到 structured/raw mode 分流、explicit raw mode、readonly-boundary hardening。
- Browser：從單一路徑可用，進化到 shared session serialization、screenshot / navigate 穩定化與資源釋放修正。
- MCP：從存在能力，進化到 command service、registry、inspect、ping、compatibility 與長跑證據。
- Summary / Audit：從基本輸出，進化到 artifact / approval / policy / tool result / context visibility 都可審查。
- Comparison / Evidence：從零散 smoke 結果，進化到 command matrix、sync matrix、workflow comparison、comparison matrix、audit、stability report、superiority claim 都能互相對應。

## 現在的狀態

- 如果只看 `ai-guide` 中 BigPhase2~9 的實際施工結果，`Nim-Cli` 已不只是接近 `g* cli` 的形狀，而是形成一個有自己主幹觀點、可審計、可長期運作、並在多條工程面向上嘗試超過 `g* cli` 的產品。
- BigPhase7~9 收掉的，是最後真正會讓審查者保留分數的工程成熟度問題。
- 目前剩下的改善空間，更多偏產品 polish、視覺體驗、或更進一步的策略深化，而不是主幹硬傷。

## 相關檔案

- `src/Nim-Cli/ai-guide/BigPhase1-9-overview.md`
