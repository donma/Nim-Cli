# db query raw policy smoke

- 目的：確認 raw SQL 需要 explicit advanced mode，而不是預設放行。
- 結果：通過。
- 對應測試：`QueryDbTool_Rejects_Raw_Query_Without_Explicit_Raw_Mode`
