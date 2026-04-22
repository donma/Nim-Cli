# config workspace override smoke

- 目的：確認 user-level base 與 workspace-level override 的 precedence 可被直接驗證。
- 結果：通過。
- 對應測試：
  - `UserConfigStore_LoadUserConfig_Allows_Workspace_Override_Over_User_Level_Base`
  - `Config_MultiWorkspace_Workflow_Uses_User_Base_And_Workspace_Override`
