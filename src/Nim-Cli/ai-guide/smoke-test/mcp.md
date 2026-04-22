# 功能名稱

mcp

## 測試目的

確認 `mcp` 命令可列工具、執行 ping、輸出 inspect 資訊，且核心層已補上受控 stdio registration / invocation / failure path 證據。

## 前置條件

- 專案可成功建置
- 不要求真實第三方 MCP server

## 測試步驟

1. 執行 `nim-cli mcp`
2. 執行 `nim-cli mcp tools`
3. 執行 `nim-cli mcp ping`
4. 執行 `nim-cli mcp inspect`

## 實際指令

```powershell
dotnet test "tests/NimCli.Integration.Tests/NimCli.Integration.Tests.csproj"
```

對應 smoke cases：

- `Mcp_Command_Without_Subcommand_Returns_Success`
- `Mcp_Tools_Command_Returns_Success`
- `Mcp_Ping_Command_Returns_Success`
- `Mcp_Inspect_Command_Returns_Success`

對應 core cases：

- `NullMcpClient_Reports_Disabled_State`
- `McpRegistration_Registers_Proxy_Tools`
- `Reports_Not_Configured_When_Command_Is_Missing`
- `Returns_Initialize_Response_For_Controlled_Stdio_Process`
- `Returns_Failure_Status_For_Invalid_Command`
- `Add_Inspect_And_Ping_Expose_Mcp_State_And_Client_Status`
- `Inspect_Without_Name_Returns_Client_Summary_With_Tool_Count`

## 預期結果

- 所有指令成功
- exit code 為 0

## 實際結果

- 對應 integration / core cases 通過

## 成功 / 失敗

成功

## 失敗原因

- 無

## 後續建議

- 後續可再補獨立 `NimCli.Mcp.Tests` 與第三方 server compatibility matrix
