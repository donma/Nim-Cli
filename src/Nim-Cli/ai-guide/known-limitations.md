# 已知限制

## 目前限制

- `git push` / `ftp upload` 的 smoke test 目前以 dry-run 路徑為主，尚未接假遠端/安全測試站整合驗證。
- TUI 已共享核心與 slash command service，現已補 `/build`、`/run-project`、`/screenshot`、`/analyze`，但互動體驗仍可再優化。
- `hooks` / `skills` / `extensions` 目前已具備 registry 管理能力，但尚未全面形成更高階工作流掛接行為。
- `MCP` 已可 inspect / ping / list tools，且已有受控第三方風格 stdio compatibility 測試；目前仍欠缺的是更大的真實第三方 product matrix。
- `CodingPipeline` 已有結構化 workflow result，但尚未全面由所有 coding 類入口統一走同一 edit workflow。
- browser / screenshot / provider 類真實整合測試仍受本機環境與外部依賴影響；目前未安裝 Playwright Chromium runtime 時會回傳受控錯誤與安裝指引。
- TUI 已補 opening / palette / approval / layout 與可測 state transition，但目前仍是以 console repaint 為主，尚未進入更完整的 terminal widget / key event simulator 架構。

## 非限制但需注意

- Windows 環境下偶爾會遇到 `VBCSCompiler` file lock，需要先 shutdown build server 再重跑。
- `auth status` 已做遮罩，但若未來新增新 log 路徑，仍需持續檢查 secret masking。
