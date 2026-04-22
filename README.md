# Nim-Cli

`Nim-Cli` 是用 `C# / .NET 10` 開發的 terminal agent，provider 目前以 `NVIDIA NIM` 為主，並提供：

- `nim-cli`：CLI 入口
- `nim-tui`：TUI 入口
- shared core：CLI / TUI 共用 `AgentOrchestrator`、`ContextBuilder`、`ToolRegistry`、`ToolPolicyService`、`CodingPipeline`、`SessionState` / `SessionManager`

它的目標不是單純聊天，而是可處理專案分析、build / run、browser screenshot、DB 查詢、git / ftp、MCP 與 coding workflow 的 Windows terminal agent。

## 目前功能

- chat / run / doctor / plan
- build / run-project / analyze / repo map
- browser open / navigate / screenshot
- DB query（read-only boundary）
- git status / diff / commit / push
- ftp upload
- session / restore / rewind
- MCP status / inspect / ping / registry
- CLI / TUI 共用 summary / audit / policy flow

## 系統需求

目前 README 以 Windows 為主。

必要條件：

- Windows 10 或 Windows 11
- `PowerShell 7` (`pwsh`)
- `.NET 10 SDK`
- `Git`
- 網路可連到 NVIDIA NIM API

依功能可能需要：

- Playwright Chromium runtime：browser / screenshot 功能需要
- `GitHub CLI` (`gh`)：如果你要做 PR 或部分 GitHub workflow

## 快速安裝

repo 內已提供安裝腳本：

- `scripts/install-prerequisites.ps1`

這是新機器或第一次 clone repo 後的環境初始化腳本。

在 repo 根目錄執行：

```powershell
pwsh -ExecutionPolicy Bypass -File ".\scripts\install-prerequisites.ps1"
```

如果也要安裝 `gh`：

```powershell
pwsh -ExecutionPolicy Bypass -File ".\scripts\install-prerequisites.ps1" -InstallGitHubCli
```

如果暫時不想安裝 Playwright browser runtime：

```powershell
pwsh -ExecutionPolicy Bypass -File ".\scripts\install-prerequisites.ps1" -SkipPlaywright
```

這個腳本會做：

1. 檢查並安裝 `.NET 10 SDK`
2. 檢查並安裝 `PowerShell 7`
3. 檢查並安裝 `Git`
4. 可選安裝 `GitHub CLI`
5. 建立本地 `appsettings.secret.json`
6. `dotnet restore`
7. 安裝 Playwright Chromium runtime
8. 跑一次 `dotnet build`

## 手動安裝流程

如果你不想用腳本，可以照下面步驟手動安裝。

### 1. 安裝 .NET 10 SDK

確認是否已安裝：

```powershell
dotnet --version
```

如果沒有，使用 `winget`：

```powershell
winget install --id Microsoft.DotNet.SDK.10 --exact --accept-package-agreements --accept-source-agreements --disable-interactivity
```

### 2. 安裝 PowerShell 7

確認是否已安裝：

```powershell
pwsh -NoLogo -NoProfile -Command '$PSVersionTable.PSVersion.ToString()'
```

如果沒有：

```powershell
winget install --id Microsoft.PowerShell --exact --accept-package-agreements --accept-source-agreements --disable-interactivity
```

### 3. 安裝 Git

確認是否已安裝：

```powershell
git --version
```

如果沒有：

```powershell
winget install --id Git.Git --exact --accept-package-agreements --accept-source-agreements --disable-interactivity
```

### 4. 可選：安裝 GitHub CLI

如果你會用到 GitHub PR / issue / checks workflow：

```powershell
winget install --id GitHub.cli --exact --accept-package-agreements --accept-source-agreements --disable-interactivity
```

確認：

```powershell
gh --version
```

### 5. 還原 NuGet 套件

```powershell
dotnet restore ".\Nim-Cli.slnx"
```

### 6. 安裝 Playwright Chromium runtime

如果你要用 browser / screenshot 功能，先 build 一次 CLI：

```powershell
dotnet build ".\src\Nim-Cli\Nim-Cli.csproj" -c Debug
```

再安裝 Playwright Chromium：

```powershell
pwsh -NoLogo -NoProfile -ExecutionPolicy Bypass -File ".\src\Nim-Cli\bin\Debug\net10.0\playwright.ps1" install chromium
```

### 7. 最後確認 solution 可建置

```powershell
dotnet build ".\Nim-Cli.slnx"
```

## 設定 NVIDIA NIM

### 1. 建立本地 secret 設定檔

repo 內已有範例檔：

- `src/Nim-Cli/appsettings.secret.example.json`
- `src/NimTui.App/appsettings.secret.example.json`

第一次可直接複製：

```powershell
Copy-Item ".\src\Nim-Cli\appsettings.secret.example.json" ".\src\Nim-Cli\appsettings.secret.json"
Copy-Item ".\src\NimTui.App\appsettings.secret.example.json" ".\src\NimTui.App\appsettings.secret.json"
```

然後把 `ApiKey` 改成你的 NIM key。

### 2. 設定檔位置與 precedence

目前實作大致如下：

- user-level app home：`%LocalAppData%\NimCli\`
- 若有設定 `NIMCLI_HOME`，則改用該目錄
- user-level config 作為 base
- workspace config 作為 override

會讀的檔案類型包含：

- `appsettings.json`
- `appsettings.secret.json`
- `appsettings.Local.json`

### 3. 用 CLI 寫入 API key

也可以直接用命令：

```powershell
dotnet run --project ".\src\Nim-Cli\Nim-Cli.csproj" -- auth login
```

或：

```powershell
dotnet run --project ".\src\Nim-Cli\Nim-Cli.csproj" -- auth login --api-key "<your-key>"
```

### 4. 驗證 provider 狀態

```powershell
dotnet run --project ".\src\Nim-Cli\Nim-Cli.csproj" -- auth status
dotnet run --project ".\src\Nim-Cli\Nim-Cli.csproj" -- models list
dotnet run --project ".\src\Nim-Cli\Nim-Cli.csproj" -- doctor
```

## 執行方式

### CLI

```powershell
dotnet run --project ".\src\Nim-Cli\Nim-Cli.csproj" --
```

或直接指定命令：

```powershell
dotnet run --project ".\src\Nim-Cli\Nim-Cli.csproj" -- doctor
dotnet run --project ".\src\Nim-Cli\Nim-Cli.csproj" -- plan "summarize current architecture"
dotnet run --project ".\src\Nim-Cli\Nim-Cli.csproj" -- run "analyze this repository"
```

### TUI

```powershell
dotnet run --project ".\src\NimTui.App\NimTui.App.csproj"
```

## 首次安裝後最短驗證流程

如果你剛完成安裝，想先確認整套環境有沒有通，建議直接照下面順序跑。

### 1. 確認 SDK 與 shell

```powershell
dotnet --version
pwsh -NoLogo -NoProfile -Command '$PSVersionTable.PSVersion.ToString()'
git --version
```

### 2. 確認 solution 能建置

```powershell
dotnet build ".\Nim-Cli.slnx"
```

### 3. 確認 CLI 本體可啟動

```powershell
dotnet run --project ".\src\Nim-Cli\Nim-Cli.csproj" -- doctor
```

### 4. 如果你已填好 NIM key，確認 provider 狀態

```powershell
dotnet run --project ".\src\Nim-Cli\Nim-Cli.csproj" -- auth status
dotnet run --project ".\src\Nim-Cli\Nim-Cli.csproj" -- models list
```

### 5. 確認核心測試可跑

```powershell
dotnet test ".\tests\NimCli.Core.Tests\NimCli.Core.Tests.csproj"
```

### 6. 如果你有安裝 Playwright，再確認 browser 相關能力

```powershell
pwsh -NoLogo -NoProfile -ExecutionPolicy Bypass -File ".\src\Nim-Cli\bin\Debug\net10.0\playwright.ps1" install chromium
dotnet test ".\tests\NimCli.Integration.Tests\NimCli.Integration.Tests.csproj"
```

如果上面這幾步都通，通常代表：

- SDK 正常
- PowerShell 7 正常
- CLI host 正常
- DI / build / test 主路徑正常
- provider 設定大致正常
- Playwright browser runtime 已就緒

## 從原始碼發佈可執行檔

如果你不想每次都用 `dotnet run`，可以直接 publish 成執行檔。

### 發佈 nim-cli

```powershell
dotnet publish ".\src\Nim-Cli\Nim-Cli.csproj" -c Release -r win-x64 --self-contained false -o ".\publish\nim-cli"
```

輸出完成後，可執行檔通常會在：

```text
publish\nim-cli\Nim-Cli.exe
```

### 發佈 nim-tui

```powershell
dotnet publish ".\src\NimTui.App\NimTui.App.csproj" -c Release -r win-x64 --self-contained false -o ".\publish\nim-tui"
```

輸出完成後，可執行檔通常會在：

```text
publish\nim-tui\Nim-Tui.exe
```

### 如果你想做 self-contained

這樣目標機器不用另外安裝 .NET runtime，但輸出會比較大：

```powershell
dotnet publish ".\src\Nim-Cli\Nim-Cli.csproj" -c Release -r win-x64 --self-contained true -o ".\publish\nim-cli-self"
dotnet publish ".\src\NimTui.App\NimTui.App.csproj" -c Release -r win-x64 --self-contained true -o ".\publish\nim-tui-self"
```

### 發佈後建議一起帶的檔案

至少確認下面幾種檔案在輸出目錄可用：

- `appsettings.json`
- `appsettings.secret.json` 或對應 user-level config
- Playwright runtime（如果你要 browser / screenshot）

### 發佈後最短驗證

```powershell
.\publish\nim-cli\Nim-Cli.exe doctor
.\publish\nim-cli\Nim-Cli.exe auth status
.\publish\nim-tui\Nim-Tui.exe
```

## 常用命令

### 核心命令

```text
nim-cli
nim-cli chat
nim-cli run "<prompt>"
nim-cli doctor
nim-cli plan "<task>"
nim-cli session show
nim-cli session resume
nim-cli settings show
nim-cli settings set <key> <value>
nim-cli workspace
nim-cli mcp status
nim-tui
```

### tool 導向命令

```text
nim-cli build --project <path>
nim-cli run-project --project <path>
nim-cli screenshot --url http://localhost:5000 --out shot.png
nim-cli db query --conn default --table User --where "id='123'"
nim-cli git status
nim-cli git diff
nim-cli git push --dry-run
nim-cli ftp upload --dry-run
```

## 驗證與測試

常用測試：

```powershell
dotnet test ".\tests\NimCli.Core.Tests\NimCli.Core.Tests.csproj"
dotnet test ".\tests\NimCli.Integration.Tests\NimCli.Integration.Tests.csproj"
dotnet test ".\tests\NimTui.Tests\NimTui.Tests.csproj"
```

完整建置：

```powershell
dotnet build ".\Nim-Cli.slnx"
```

如果你剛裝完外部依賴，建議順序：

1. `dotnet build ".\Nim-Cli.slnx"`
2. `dotnet test ".\tests\NimCli.Core.Tests\NimCli.Core.Tests.csproj"`
3. `dotnet test ".\tests\NimCli.Integration.Tests\NimCli.Integration.Tests.csproj"`
4. `dotnet test ".\tests\NimTui.Tests\NimTui.Tests.csproj"`

## 執行期資料位置

目前 runtime state 會保存到 user-level app home 下的：

```text
.nim-cli-runtime/
```

內容包含：

- sessions
- checkpoints
- state.json

預設 base directory 是：

- `%LocalAppData%\NimCli\`

也可以用：

- `NIMCLI_HOME`

覆蓋。

## 常見問題

### 1. `pwsh` 找不到

請先安裝 PowerShell 7：

```powershell
winget install --id Microsoft.PowerShell --exact --accept-package-agreements --accept-source-agreements --disable-interactivity
```

### 2. `dotnet` 找不到

請先安裝 `.NET 10 SDK`：

```powershell
winget install --id Microsoft.DotNet.SDK.10 --exact --accept-package-agreements --accept-source-agreements --disable-interactivity
```

### 3. browser / screenshot 失敗

通常是 Playwright runtime 尚未安裝。請先：

```powershell
dotnet build ".\src\Nim-Cli\Nim-Cli.csproj" -c Debug
pwsh -NoLogo -NoProfile -ExecutionPolicy Bypass -File ".\src\Nim-Cli\bin\Debug\net10.0\playwright.ps1" install chromium
```

### 4. `models list` 或 `auth status` 顯示沒 API key

代表 `appsettings.secret.json` 還沒填好，或目前 workspace / user-level precedence 沒命中你預期的檔案。

先檢查：

- `src/Nim-Cli/appsettings.secret.json`
- `%LocalAppData%\NimCli\appsettings.secret.json`
- `NIMCLI_HOME`

### 5. `git push` / `ftp upload` 沒有直接真的執行

這是設計上的風險控制。高風險操作會經過 approval / dry-run / policy 路徑，不是預設無條件放行。

## 相關文件

- 架構總覽：`src/Nim-Cli/ai-guide/nim-cli-architecture.md`
- BigPhase 總整理：`src/Nim-Cli/ai-guide/BigPhase1-9-overview.md`
- 架構補充：`src/Nim-Cli/ai-guide/architecture-overview.md`
- 測試總覽：`src/Nim-Cli/ai-guide/testing-overview.md`
- 已知限制：`src/Nim-Cli/ai-guide/known-limitations.md`
- smoke reports：`src/Nim-Cli/ai-guide/smoke-test/`

## 注意

- `appsettings.secret.json` 不應提交到 git
- `appsettings.secret.example.json` 才是可提交範本
- 如果真實 API key 曾經出現在對話、截圖、日誌或其他外部可見位置，應直接視為已暴露並輪替
