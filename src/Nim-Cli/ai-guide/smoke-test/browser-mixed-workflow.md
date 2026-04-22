# browser mixed workflow smoke

- 目的：確認 browser serialization 修正不只停在 semaphore，而有 mixed workflow 證據支撐。
- 結果：通過。
- 證據：browser serialization core test 維持綠燈，TUI / mixed workflow 測試無回歸。
