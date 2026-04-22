# 功能名稱

`mcp-long-run`

## 測試目的

確認 MCP registration / invocation / compatibility / failure path 已形成長期穩定度證據。

## 前置條件

- `ThirdPartyMcpCompatibilityTests`
- `StdioMcpClientTests`
- `McpCommandServiceTests`

## 測試步驟

1. 執行 core 與 integration tests
2. 對照 MCP depth report

## 實際命令

```powershell
dotnet test "tests/NimCli.Core.Tests/NimCli.Core.Tests.csproj"
dotnet test "tests/NimCli.Integration.Tests/NimCli.Integration.Tests.csproj"
```

## 預期結果

- MCP 不只可用，還有 compatibility / failure / recovery 證據

## 實際結果

- 對應測試通過

## 成功 / 失敗

成功

## 是否 dry-run

否

## policy / risk 說明

- 使用受控第三方風格腳本，不使用真實敏感憑證

## 後續建議

- 後續補第三方 product matrix
