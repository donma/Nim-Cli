# ftp upload dry-run 一致性 smoke

- 目的：確認 `upload_ftp` dry-run 不會做真實 upload。
- 結果：通過。
- 證據：tool 在 dry-run 直接回傳受控結果，不連線、不上傳。
- 對應測試：`FtpUploadTool_DryRun_Does_Not_Require_Network_Call`
