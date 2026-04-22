# Nim-Cli 專案施工總覽

## 1. 專案目標

`Nim-Cli` 是一套用 `C# + .NET 10` 開發的 terminal agent 系統，現況是 `CLI + TUI + shared core`。  
主藍本採用 `g* cli` 類型產品的整體骨架思路：一個總 agent loop，外掛 shell、web、browser、MCP、tool routing，再搭配 `aider` 式 coding workflow。

`nim-cli` 的 provider 不用 Gemini，而是改成 `NVIDIA NIM`。NIM 提供 OpenAI-compatible inference API，因此適合在 C# 內做成一個 `OpenAI-compatible provider adapter`。

瀏覽器操作與截圖採用 `Playwright for .NET`。Playwright 官方 .NET 文件直接提供 `Page.ScreenshotAsync(...)` 以及 Chromium 導頁與截圖做法，因此這條路在 C# 上是正規做法，不需要繞奇怪 shell 腳本。

寫程式那一段的 workflow 參考 `aider`，但只吸收它的強項，不整套照搬。aider 的強項是 repository map、git integration、以及自動 lint/test 循環修正。

---

## 2. 最終技術決策

### 2.1 主骨架
選 `g* cli` 類型產品風格當主藍本。

原因不是因為要抄 Gemini provider，而是因為它的產品形狀最接近 `nim-cli` 的需求：

- shell command
- web fetch
- web search
- MCP / extension
- tool routing
- 通用 terminal agent，不只寫 code

### 2.2 coding 子系統
補進 `aider` 的幾個核心觀念：

- repo map
- plan before edit
- auto lint / test
- git-aware edit flow

### 2.3 shell
Windows 預設 shell 採用 `PowerShell`。

這沒有本質問題，因為 `nim-cli` 目前目標使用情境就是：

- `dotnet build`
- `dotnet run`
- `msbuild`
- `git`
- `Edge`
- `FTP`
- Windows 路徑與工具鏈

這些都很適合 PowerShell。  
但 shell 只能是執行層，不能變成整套系統的唯一 abstraction。

### 2.4 browser
瀏覽器操作與截圖採用 `Playwright for .NET`。

不要把瀏覽器控制塞進 shell。  
這塊要做成正式 tool：`BrowserTool` / `ScreenshotTool`。

---

## 3. 成功率最高的施工原則

這份文件的目標不是做一個學術上最漂亮的 agent framework，而是讓 AI 工程代理能高機率一次施工到可驗收狀態。

所以這裡的原則是：

1. 先做出可跑版本，再補高級功能
2. 主體先做一套 agent loop，不要一開始就做多套狀態機
3. provider 先只做 NIM，但 provider abstraction 一開始就留好
4. shell 預設 PowerShell，但不要綁死未來不能接 bash
5. browser 一律走 Playwright，不要混 shell + browser script
6. DB、FTP、Git 優先先做成 tool，不要全部靠模型自由拼 shell
7. aider 的精華只做成 coding pipeline，不直接整套照抄它的互動模式

---

## 4. 目標功能範圍

`Nim-Cli` 第一版要支援以下高頻任務：

1. 幫我給專案建議
2. 分析專案弱點
3. 幫我網路找新聞
4. 幫我編譯並執行專案
5. 專案執行後打開頁面並截圖
6. 查詢資料庫指定資料
7. 編譯到 Debug / Release
8. 上傳到 FTP
9. git push

這代表 `Nim-Cli` 不是純 coding assistant，而是 terminal agent。  
因此它的核心不是單純 chat，而是：

- intent → plan
- plan → tool selection
- tool execution
- post-check / retry
- summarization

---

## 5. 專案骨架總覽

目前 repo 採用單 solution、多 project。

### 5.1 Solution 結構

```text
Nim-Cli.slnx

src/
  Nim-Cli/                        // CLI 入口、ServiceConfiguration、command services
  NimTui.App/                     // TUI 入口
  NimCli.Core/                    // 核心 domain 與 orchestration
  NimCli.Provider.Abstractions/   // provider 介面
  NimCli.Provider.Nim/            // NIM provider 實作
  NimCli.Tools.Abstractions/      // tool 介面
  NimCli.Tools.Shell/             // shell tools
  NimCli.Tools.Web/               // web fetch / web search
  NimCli.Tools.Browser/           // Playwright tools
  NimCli.Tools.Db/                // DB tools
  NimCli.Tools.Git/               // Git tools
  NimCli.Tools.Ftp/               // FTP tools
  NimCli.Coding/                  // repo map / code edit / lint test
  NimCli.Mcp/                     // MCP client / MCP adapter
  NimCli.Infrastructure/          // config / logging / storage / secrets
  NimCli.Contracts/               // DTO / request / response contracts

tests/
  NimCli.Core.Tests/
  NimCli.Integration.Tests/
  NimTui.Tests/
```

---

## 6. 最小可行架構

### 6.1 App 層
`Nim-Cli` 專案主要負責 CLI host、DI 與 command services；`NimTui.App` 則負責 TUI host，但兩者都共用同一套 core / tools / provider / policy / session 主幹。

CLI / TUI host 只負責：

- command line parsing
- 啟動 DI container
- 載入 config
- 啟動 agent session
- 顯示輸出

不要把商業邏輯寫在 `Program.cs`。

### 6.2 Core 層
`NimCli.Core` 是大腦，負責：

- session state
- context assembly
- prompt building
- tool planning
- approval policy
- post-tool evaluation
- retry policy
- final response composition

### 6.3 Provider 層
`NimCli.Provider.Nim` 負責：

- 讀取 NIM API key
- 讀取 base URL
- 呼叫 `/v1/models`
- 呼叫 `/v1/chat/completions`
- 將 tool-call / function-call 類型結果轉成 core 可理解的格式

NIM 官方文件說明它提供 OpenAI-compatible inference API，所以這層應該做成盡量貼近 OpenAI-compatible 介面的 adapter。

### 6.4 Tools 層
每個 tool 都是獨立模組，統一註冊到 ToolRegistry。

### 6.5 Coding 層
這層只做 coding 專屬流程：

- repo scan
- repo map
- plan-before-edit
- patch apply
- lint/test/build verify
- git-aware diff / commit message suggestion

---

## 7. 核心流程設計

## 7.1 單一 agent loop

整個系統只有一套總控主幹；以目前 repo 的描述，更接近：

```text
User Input
  -> Intent Resolver
  -> Context Builder
  -> Prompt Builder
  -> Agent Orchestrator
  -> Tool Policy / Tool Registry
  -> Provider Router / Provider Call
  -> Tool Execution
  -> Session / Summary Update
  -> Optional Retry
  -> Final Answer
```

這一套要由 `AgentOrchestrator` 全權掌控。  
不要再另外做一套隱藏的 aider 式 loop，不然會衝突。

## 7.2 Pre-LLM 與 Post-LLM

### Pre-LLM
要做的事：

- 解析使用者意圖
- 判斷是否是 coding 類任務
- 判斷要不要先跑 repo map
- 判斷要不要先讀專案檔案
- 判斷要不要先執行 web search / db query / shell state gather
- 組 prompt 與 tools list

### Post-LLM
要做的事：

- 判斷模型是直接回答還是要調 tool
- 執行 tool
- 驗證 tool 結果
- 針對 coding 任務執行 build / lint / test
- 決定是否 retry
- 整理輸出

---

## 8. 必備核心類別

以下是 AI 施工時一定要有的骨架。

### 8.1 AgentOrchestrator
責任：

- 管整場 session
- 控制 pre-LLM / post-LLM
- 呼叫 provider
- 呼叫 tools
- 控制 retry

### 8.2 SessionState
責任：

- 保存目前工作目錄
- 保存最近對話
- 保存工具執行歷史
- 保存使用者偏好
- 保存目前模式，例如 analysis / coding / ops
- 保存 `current task`
- 保存 `recent actions`
- 保存 `policy audit`
- 保存 `context strategy`

### 8.3 ContextBuilder
責任：

- 收集 prompt 所需上下文
- 檔案內容
- repo map
- shell output
- tool result
- web fetch result
- db result

這裡只能有一套 context ownership。

### 8.4 PromptBuilder
責任：

- 組 system prompt
- 組 developer prompt
- 組 user message
- 注入工具說明
- 分析任務類型

token / budget 控制的主責目前比較偏向 `ContextBuilder`。

### 8.5 ProviderRouter
責任：

- 根據 config 選 provider
- 目前先固定 NIM
- 未來保留 OpenAI-compatible provider 擴充點

### 8.6 ToolRegistry
責任：

- 註冊所有 tool
- 查詢 tool metadata
- 驗證 tool name 是否存在
- 回傳 tool 執行器

### 8.7 ToolPolicyService
責任：

- 控制 ask / allow / deny
- 管理高風險操作
- 例如刪檔、git push、ftp upload、資料庫寫入

### 8.8 CommandIntentResolver
責任：

- 將自然語言轉成高階意圖
- 例如：
  - analyze_project
  - build_project
  - run_project
  - screenshot_page
  - query_db
  - git_push
  - upload_ftp

### 8.9 CodingPipeline
責任：

- repo map
- file selection
- edit planning
- patch apply
- build/lint/test
- git-aware summary

---

## 9. Provider 設計

## 9.1 介面

建議定義：

```text
IChatProvider
IModelCatalogProvider
IProviderHealthChecker
```

### IChatProvider
負責：

- chat completion
- tool call support
- streaming support
- cancellation support

### IModelCatalogProvider
負責：

- 取回 `/v1/models`
- 驗證 model 是否存在

### IProviderHealthChecker
負責：

- ping
- ready check
- 失敗時友善訊息

## 9.2 NIM 實作

`NimChatProvider` 至少要支援：

- `ApiKey`
- `BaseUrl`
- `Model`
- `Temperature`
- `Timeout`
- `MaxTokens`
- `UseStreaming`

NIM 官方文件已列出 `/v1/models` 與 `/v1/chat/completions`，因此 provider 主路徑至少要把這兩個做穩。

## 9.3 Key 輸入方式

一定要支援這三種：

1. 環境變數
2. 設定檔
3. 首次啟動互動式輸入並安全保存

建議名稱：

- `NIM_API_KEY`
- `NIM_BASE_URL`
- `NIM_MODEL`

### 建議互動命令

```text
nim-cli auth login
nim-cli auth status
nim-cli models list
nim-cli settings set provider.defaultModel <model-name>
```

---

## 10. 設定檔設計

目前實作的設定來源與優先序比較接近：

- user-level app home：`%LocalAppData%/NimCli/`
- 可用 `NIMCLI_HOME` 覆蓋 user-level app home 位置
- user-level `appsettings.json`
- user-level `appsettings.secret.json`
- user-level `appsettings.Local.json`
- workspace `appsettings.json`
- workspace `appsettings.secret.json`
- workspace `appsettings.Local.json`

其中 user-level config 是穩定基底，workspace config 是 override。

### 建議格式

```json
{
  "provider": {
    "name": "nim",
    "baseUrl": "https://integrate.api.nvidia.com/v1",
    "apiKeyEnv": "NIM_API_KEY",
    "model": "your-default-model",
    "timeoutSeconds": 120,
    "streaming": true
  },
  "shell": {
    "default": "powershell",
    "powershellExe": "pwsh",
    "workingDirectory": ""
  },
  "browser": {
    "engine": "chromium",
    "headless": true,
    "defaultViewportWidth": 1440,
    "defaultViewportHeight": 900
  },
  "coding": {
    "enableRepoMap": true,
    "autoBuildAfterEdit": true,
    "autoLint": true,
    "autoTest": false,
    "autoCommit": false
  },
  "tools": {
    "allowShell": true,
    "allowWebFetch": true,
    "allowWebSearch": true,
    "allowBrowser": true,
    "allowDbRead": true,
    "allowFtpUpload": false,
    "allowGitPush": false
  }
}
```

---

## 11. Shell 設計

## 11.1 預設用 PowerShell 沒問題

但只能是 provider，不是核心。

### 目前介面

```text
IShellProvider
  - ExecuteAsync(...)
```

### 目前實作

```text
PowerShellProvider
```

## 11.2 PowerShell 啟動參數

目前預設固定用：

```text
pwsh -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand ...
```

原因：

- 避免被使用者 profile 污染
- 避免互動式卡住
- 降低 AI 執行時環境漂移

## 11.3 重要注意事項

不要讓模型直接自由生成超長 PowerShell 指令。  
應該先把任務轉成結構化 action，再由 C# 組命令。現況已使用 `PowerShellCommandBuilder`、external command builder 與 argument tokenization 來處理 critical path。

錯誤示範：

```text
模型直接輸出一大串 powershell script 然後直接執行
```

正確做法：

```text
模型輸出:
{
  action: "build_project",
  configuration: "Release"
}

C# Tool:
組出 dotnet build /p:Configuration=Release
```

---

## 12. Browser 與 Playwright 設計

## 12.1 為什麼用 Playwright

Playwright for .NET 官方已提供導頁與 screenshot API，這在 C# 上是穩定正規路線。

## 12.2 建議工具

```text
BrowserOpenTool
BrowserNavigateTool
BrowserScreenshotTool
BrowserWaitTool
BrowserCloseTool
```

## 12.3 截圖需求支援

要支援：

- 指定 URL
- 指定等待秒數
- 指定 full page
- 指定輸出檔案
- 指定 viewport
- 支援本地站台，例如 `http://localhost:5000`

## 12.4 預設流程

```text
1. build
2. run
3. wait for server ready
4. open browser page
5. wait for network idle or delay
6. screenshot
7. save file path
8. summarize result
```

---

## 13. Tool 設計

每個 tool 都要統一介面。

### 介面

```text
ITool
  - Name
  - Description
  - RiskLevel
  - InputSchema
  - ExecuteAsync(...)
```

### RiskLevel
定義：

- Low
- Medium
- High
- Critical

### 目前主幹核心工具

#### Shell 類
- `build_project`
- `run_project`
- `run_shell`
- `git_status`
- `git_diff`
- `git_commit`
- `git_push`

#### Web 類
- `web_fetch`
- `web_search`

#### Browser 類
- `browser_open`
- `open_page`
- `browser_wait`
- `screenshot_page`
- `browser_close`

#### DB 類
- `query_db`

#### FTP 類
- `upload_ftp`

#### Coding 類
- `analyze_project`
- `edit_files`
- `lint_project`
- `test_project`

---

## 14. 你的 9 個常用動作如何落地

### 1. 幫我給專案建議
走：

- `analyze_project`
- 讀取 repo map
- 掃描 solution / project / csproj
- 提供建議

### 2. 分析專案弱點
走：

- `analyze_project`
- `repo_map`
- 可選 `build_project`
- 可選 `lint_project`

### 3. 幫我網路找新聞
走：

- `web_search`
- `web_fetch`

### 4. 幫我編譯並執行專案
走：

- `build_project`
- `run_project`

### 5. 專案執行起來幫我用 Edge 截圖
走：

- `build_project`
- `run_project`
- `open_page`
- `screenshot_page`

實際底層可用 Playwright Chromium，不需要真的外掛控制使用者正在手動操作的 Edge 視窗。

### 6. 幫我查一下那 DB 的中 User 表裡面的 id='123'
走：

- `query_db`

注意 DB 主路徑應維持 read-only 邊界。  
不要讓 agent 任意 update/delete。

### 7. 幫我編譯到 Release/Debug
走：

- `build_project` with `configuration`

### 8. 幫我上傳到 FTP
走：

- `upload_ftp`

### 9. 幫我 git push
走：

- `git_push`

注意這個預設應該是 `High` 風險，必須 ask approval。

---

## 15. Coding Pipeline 設計

這塊是 `aider` 精華落地區。aider 官方文件列出 repo map、git integration、以及自動 lint/test。

### 15.1 RepoMapBuilder
責任：

- 掃描 solution
- 建立 project tree
- 擷取檔案重要 symbol
- 只保留高價值結構資訊

早期版本不必做到 tree-sitter 很深。  
先做出可用版本：

- 檔名
- namespace
- class 名稱
- public method signature
- interface 實作關係
- project reference

C# 專案可優先考慮 Roslyn。

### 15.2 CodeEditPlanner
責任：

- 接收任務
- 決定哪些檔案要讀
- 決定哪些檔案要改
- 產出 edit plan

### 15.3 PatchApplier
責任：

- 套 patch
- 保存 backup
- 產出 diff summary

### 15.4 CodeVerifier
責任：

- `dotnet build`
- `dotnet test`
- 可加 lint / format

### 15.5 GitAwareChangeService
責任：

- 收集 diff
- 生成 commit summary
- 可選 auto-commit
- 不預設 auto-push

---

## 16. DB 工具設計

DB 主路徑只做 read-only，而且先支援最需要的資料來源。

### 介面

```text
IDbQueryTool
```

### 輸入

- connection name
- table
- where clause 或 key query
- top N

### 安全限制

- structured mode 與 raw mode 必須分流
- raw SQL 必須顯式 `raw_mode=true`
- 禁止 multi-statement
- 禁止 SQL comments
- 禁止 readonly-boundary bypass pattern
- `top_n` 應限制範圍
- structured `where` 應限制複雜度與過度自由條件

### 目前建議

優先支援：

- SQL Server
- SQLite

---

## 17. FTP 工具設計

FTP 不要先讓模型自由組 ftp script。  
直接做成正式 tool。

### 介面

```text
IFtpUploadTool
```

### 輸入

- host
- username
- password source
- local path
- remote path
- overwrite

### 安全限制

- 預設 ask approval
- 預設禁止 wildcard 大量上傳
- 預設禁止刪遠端

---

## 18. Git 工具設計

### 目前主幹工具

- `git_status`
- `git_diff`
- `git_commit`
- `git_push`

### 安全分級

- `git_status` = Low
- `git_diff` = Low
- `git_commit` = Medium
- `git_push` = High

### 原則

- 模型可以建議 commit message
- commit 可 ask approval
- push 一律 ask approval

---

## 19. MCP 與 Extension 設計

`g* cli` 類型產品的一大價值就是 built-in tools + MCP / extension 生態，MCP 也確實是本系統需要保留的擴充軸。

`Nim-Cli` 不需要自己做一整套複雜 extension marketplace。  
但一定要預留接口。

### 目前至少應有

- `IMcpClient`
- 可連 stdio 型 MCP server
- 可連本地 process MCP server
- 將 MCP tools 註冊進 ToolRegistry
- runtime registry / inspect / ping / status

### 這樣的好處

未來可以外掛：

- database MCP
- browser MCP
- internal company tools
- deployment tools

---

## 20. CLI 指令規劃

### 20.1 核心命令

```text
nim-cli
nim-tui
nim-cli chat
nim-cli run "<prompt>"
nim-cli auth login
nim-cli auth status
nim-cli models list
nim-cli doctor
nim-cli plan "<task>"
nim-cli session show
nim-cli session resume
nim-cli settings show
nim-cli settings set <key> <value>
nim-cli workspace
nim-cli mcp status
```

### 20.2 tool 導向命令

```text
nim-cli build --project xxx.csproj --config Release
nim-cli run-project --project xxx.csproj
nim-cli screenshot --url http://localhost:5000 --out shot.png
nim-cli db query --conn default --table User --where "id='123'"
nim-cli ftp upload --local ./publish.zip --remote /site/publish.zip
nim-cli git push
```

### 20.3 開發命令

```text
nim-cli repo map
nim-cli analyze
nim-cli test
nim-cli lint
```

---

## 21. 互動模式規劃

建議支援三種模式，但都是同一套 orchestrator 控制。

### 21.1 AnalysisMode
用來：

- 提建議
- 分析弱點
- 讀 repo
- 不直接改檔

### 21.2 CodingMode
用來：

- 改檔
- build
- test
- repo map
- git-aware flow

### 21.3 OpsMode
用來：

- shell
- browser
- db
- ftp
- deploy 類操作

不要直接照搬 aider 的 `/ask / architect / code` 命令表面。  
把它變成內部 mode concept 就好。

---

## 22. Approval Policy

一定要做，不然系統很容易變危險。

### 22.1 預設可直接做
- read files
- repo map
- build
- test
- web fetch
- web search
- local browser screenshot
- select db query

### 22.2 預設 ask approval
- shell arbitrary command
- write files
- git commit
- git push
- ftp upload
- db write
- delete files

補充：高風險工具除了 approval，現在也強調 dry-run semantics 要和 audit / summary 一致。

### 22.3 預設禁止
- recursive delete
- credential dump
- unrestricted raw SQL write
- uploading whole workspace blindly

---

## 23. Logging 與診斷

目前至少應能在 summary / audit 中看見這些資料：

- provider request summary
- provider response summary
- tool chosen
- tool input
- tool result
- elapsed time
- retry count
- shell exit code
- screenshot path
- build/test result
- approval action
- policy decision
- artifact / tool result summaries

### 錯誤輸出必須做到
- 對使用者友善
- 對開發者可追 log
- 不要把完整 API key 印出來

---

## 24. Secrets 管理

NIM API key 是必要輸入，所以要做乾淨。

### 必做
- 支援環境變數
- 支援安全儲存
- log 中 masking
- `auth status` 只能顯示是否存在，不印完整 key

### 不要做
- 把 key 明文寫進 git tracked config
- 把 key 印在錯誤訊息裡

---

## 25. 歷史施工順序

這段是早期規劃稿，描述的是初版施工順序，不代表目前 repo 的實際完成狀態。  
目前實際演進與驗證結果應以 `BigPhase1-9-overview.md` 為準。

### Phase 1
先完成可跑骨架：

1. solution 結構
2. CLI host
3. config loading
4. DI
5. provider interface
6. NIM provider
7. basic chat command

### Phase 2
加入基本工具：

1. ToolRegistry
2. PowerShellProvider
3. build_project
4. run_project
5. web_fetch
6. web_search

### Phase 3
加入 browser：

1. Playwright setup
2. screenshot tool
3. local page open
4. wait and screenshot flow

### Phase 4
加入 coding：

1. repo scan
2. repo map
3. analyze_project
4. build verify
5. test verify

### Phase 5
加入 ops：

1. query_db
2. ftp_upload
3. git_push
4. approval policy

### Phase 6
最後才補：

1. streaming
2. retry policy
3. MCP client
4. richer context builder
5. commit suggestion
6. integration tests

---

## 26. AI 施工時禁止事項

這段要明確寫給施工 AI。

1. 不要一開始就做過度抽象
2. 不要先做插件市場
3. 不要先做 GUI
4. 不要先做多 provider
5. 不要把所有功能都變 shell script
6. 不要讓 provider / shell / browser / db 耦合在一起
7. 不要把商業邏輯塞進 Program.cs
8. 不要直接把 API key 寫死在程式碼
9. 不要先優化 TUI 外觀
10. 不要先追求 1:1 還原所有 `g* cli` 細節

---

## 27. 目前較合理的驗收標準

以目前 repo 狀態，較合理的驗收條件是：

1. 能輸入 NIM API key 並成功呼叫 `/v1/chat/completions`
2. 能列出 model 清單
3. 能在 CLI / TUI 輸入 prompt 並得到回答
4. 能執行 `dotnet build`
5. 能執行 `dotnet run`
6. 能打開本地頁面並截圖
7. 能分析目前 solution 基本結構
8. 能進行 read-only DB 查詢
9. 能執行 FTP 上傳
10. 能執行 `git push`，但需 approval
11. coding 任務能在修改後自動做 build 驗證
12. log 中不洩漏 key
13. config / runtime state 具 user-level base 與 workspace override 行為
14. execution summary / policy audit / tool result summaries 可被 CLI / TUI 共同使用
15. shell、DB、browser 等高風險或易出錯路徑具對應 safety boundary 與測試證據

---

## 28. 給 AI 的歷史施工指令

以下這段是早期建置期的施工說明，主要作為歷史背景，不應再直接視為目前 repo 的最新架構規格。

```markdown
請建立一個名為 Nim-Cli 的 .NET 10 C# solution。

目標：
這是一個 terminal agent CLI，不是單純聊天程式。
主骨架參考 g* cli 類型產品：一個總 agent loop，支援 shell、web、browser、db、ftp、git、MCP 擴充。請直接參考 GitHub 專案 https://github.com/google-gemini/gemini-cli 的 README、docs、packages、integration-tests 與核心工具實作方式。
provider 固定先做 NVIDIA NIM，並保留 provider abstraction。
寫程式相關流程吸收 aider 的精華：repo map、plan before edit、auto build/test、git-aware edit flow。請直接參考 GitHub 專案 https://github.com/Aider-AI/aider 的 README、docs、aider 目錄、tests 與 coding workflow 做法。
Windows 預設 shell 使用 PowerShell。
瀏覽器控制與截圖使用 Playwright for .NET。

必要要求：
1. 先完成可跑版本，不要過度設計。
2. Program.cs 只保留啟動與 DI，不放商業邏輯。
3. provider 要支援從環境變數或設定檔讀取 NIM API key、base URL、model。
4. 要有 nim-cli auth login / auth status / models list / chat / run 等基本命令。
5. 要建立以下專案分層：
   - Nim-Cli
   - NimTui.App
   - NimCli.Core
   - NimCli.Provider.Abstractions
   - NimCli.Provider.Nim
   - NimCli.Tools.Abstractions
   - NimCli.Tools.Shell
   - NimCli.Tools.Web
   - NimCli.Tools.Browser
   - NimCli.Tools.Db
   - NimCli.Tools.Git
   - NimCli.Tools.Ftp
   - NimCli.Coding
   - NimCli.Infrastructure
   - NimCli.Contracts
6. 要建立以下核心類別：
   - AgentOrchestrator
   - SessionState
   - ContextBuilder
   - PromptBuilder
   - ProviderRouter
   - ToolRegistry
   - ToolPolicyService
   - CommandIntentResolver
   - CodingPipeline
7. Tool 必須統一介面，包含 Name、Description、RiskLevel、InputSchema、ExecuteAsync。
8. 初版先完成：
   - NIM provider chat completion
   - models list
   - build_project
   - run_project
   - web_fetch
   - web_search
   - screenshot_page
   - analyze_project
   - query_db(read-only)
   - upload_ftp
   - git_push(approval required)
9. PowerShell 只能當 shell provider，不可變成整個系統唯一能力層。
10. Browser 一律透過 Playwright for .NET，不要用 shell 拼瀏覽器自動化腳本。
11. CodingPipeline 內要預留 repo map 與 auto build/test 驗證。
12. 先做最小可行版本，再逐步補 streaming、MCP、retry policy。

請直接開始產出完整 solution 與專案檔、核心介面、主要類別、設定檔、範例命令、以及最小可執行版本。施工時請優先按 GitHub 上的模組切分與流程方法實作，再轉成適合 C# / .NET 10 的結構。
```

---

## 29. 最後結論

這條路目前確認下來的核心決策是：

- 主體採用 `g* cli` 類型產品的共享 orchestrator 形狀
- provider 改成 `NIM`
- shell 預設 `PowerShell`
- browser 用 `Playwright for .NET`
- coding 流程吸收 `aider` 精華
- 只保留一套總 orchestrator
- CLI / TUI 共用同一套 core / tools / provider / policy / session
- 先求能跑，再逐步把 summary、audit、context、shell、DB safety 做成熟

這樣的 `Nim-Cli` 才會真的像你要的：  
不是別人的 CLI 換皮，而是用 C# 寫出自己的 agent CLI，而且初版就有高機率能施工成功。
