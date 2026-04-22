# run-project timeout smoke

- 目的：確認 `run_project` timeout 不再被包裝成 success。
- 結果：通過。
- 行為：timeout 時回傳 failure，並標示 process 已 terminated。
- 對應測試：`RunProjectTool_Timeout_Returns_Failure`
