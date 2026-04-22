# git push dry-run 一致性 smoke

- 目的：確認 `git_push` 的 dry-run 不只停在 policy，而會落到真實命令層。
- 結果：通過。
- 證據：`GitPushTool` 已改為在 dry-run 時帶 `--dry-run`，且 core test 已驗證命令內容。
- 對應測試：`GitPushTool_DryRun_Uses_Git_DryRun_Flag`
