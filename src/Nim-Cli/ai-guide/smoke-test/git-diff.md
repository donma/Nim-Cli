# 功能名稱

`git-diff`

## 測試目的

確認 `nim-cli git diff` 可在受控測試 repo 中輸出真實 diff。

## 前置條件

- 工作目錄：`D:\AI_PROJECTS\Nim-Cli\Nim-Cli`
- 已建立暫時 repo `.tmp-bigphase3-git-smoke`
- `readme.txt` 有未提交變更

## 測試步驟

1. 執行 `nim-cli git diff --working-dir .tmp-bigphase3-git-smoke`。
2. 確認輸出 unified diff。

## 實際指令

```powershell
dotnet run --project "src/Nim-Cli/Nim-Cli.csproj" -- git diff --working-dir ".tmp-bigphase3-git-smoke"
```

## 預期結果

- 輸出 unified diff。
- 內容包含新增行。

## 實際結果

- 指令成功完成。
- 輸出包含：
  - `diff --git a/readme.txt b/readme.txt`
  - `@@ -1 +1,2 @@`
  - `+changed`

## 成功 / 失敗

成功

## 失敗原因

無

## 後續建議

- 後續可補 staged diff 與多檔 diff smoke case。
