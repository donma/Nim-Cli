# startup init smoke

- 目的：確認主路徑 startup 不再含明顯 sync-over-async。
- 結果：通過。
- 證據：service bootstrap、MCP registration、patch verification、vim install plan 皆已改為 async await。
