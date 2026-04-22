# 功能名稱

`mcp-third-party-compatibility`

## 測試目的

確認 `Nim-Cli` 對第三方風格 stdio MCP server 具備基本 compatibility integration 證據。

## 前置條件

- 可執行 `pwsh`
- 允許建立暫時 PowerShell script

## 測試資料

- 受控 script：回傳 MCP initialize response

## 測試步驟

1. 建立模擬第三方 MCP script
2. 用 `StdioMcpClient` 啟動
3. 驗證 `IsAvailableAsync()` 與 `GetStatusAsync()`

## 實際命令

```powershell
dotnet test "tests/NimCli.Integration.Tests/NimCli.Integration.Tests.csproj"
```

## 預期結果

- client 可讀到 initialize response
- response 含受控 server identity

## 實際結果

- `ThirdPartyMcpCompatibilityTests` 通過

## 成功 / 失敗

成功

## 是否 dry-run

否

## policy / risk 說明

- 不使用真實第三方帳號或 secret
- 僅驗證受控相容性

## 後續建議

- 後續補真實第三方 MCP matrix
