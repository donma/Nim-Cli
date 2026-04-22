# context resume stability smoke

- 目的：確認 resume / long-context 情境下，task / policy / recent actions 不會被低優先內容壓掉。
- 結果：通過。
- 證據：BigPhase8 的 context block budget 與 resume strategy 測試已覆蓋。
