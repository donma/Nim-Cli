# browser concurrency smoke

- 目的：確認 shared browser session 已加入 serialize 保護。
- 結果：通過。
- 對應測試：`BrowserSessionManager_Serializes_Concurrent_Access`
