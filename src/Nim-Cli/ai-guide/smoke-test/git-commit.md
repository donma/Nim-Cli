# 功能名稱

`git-commit`

## 測試目的

確認 `nim-cli git commit` 在高風險操作前會要求 approval，不會直接執行 commit。

## 前置條件

- 工作目錄：`D:\AI_PROJECTS\Nim-Cli\Nim-Cli`
- 受控測試 repo：`.tmp-bigphase3-git-smoke`

## 測試步驟

1. 執行 `nim-cli git commit --working-dir .tmp-bigphase3-git-smoke -m "smoke commit"`。
2. 不提供 approval。
3. 確認 CLI 停在 approval prompt，且未直接 commit。

## 實際指令

```powershell
dotnet run --project "src/Nim-Cli/Nim-Cli.csproj" -- git commit --working-dir ".tmp-bigphase3-git-smoke" -m "smoke commit"
```

## 預期結果

- 顯示 approval prompt。
- 未經同意不執行 commit。

## 實際結果

- CLI 輸出：`Approval required for git commit. Continue? [y/N]`
- 行為符合高風險操作需人工批准的規格。

## 成功 / 失敗

成功

## 失敗原因

無

## 後續建議

- 後續可用受控輸入流補真正 commit 成功的自動化 smoke case。
