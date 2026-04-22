# 功能名稱

`git-status`

## 測試目的

確認 `nim-cli git status` 可在受控測試 repo 中輸出真實 git 狀態。

## 前置條件

- 工作目錄：`D:\AI_PROJECTS\Nim-Cli\Nim-Cli`
- 建立暫時測試 repo：`.tmp-bigphase3-git-smoke`

## 測試步驟

1. 初始化暫時 git repo。
2. 建立並提交初始檔案。
3. 修改檔案但不 stage。
4. 執行 `nim-cli git status --working-dir .tmp-bigphase3-git-smoke`。

## 實際指令

```powershell
git init ".tmp-bigphase3-git-smoke"
# 之後建立 readme.txt、commit initial、再修改 readme.txt
dotnet run --project "src/Nim-Cli/Nim-Cli.csproj" -- git status --working-dir ".tmp-bigphase3-git-smoke"
```

## 預期結果

- 顯示未 staged 修改。
- 不依賴主工作區是否為 git repo。

## 實際結果

- 指令成功完成。
- 輸出：
  - `On branch master`
  - `Changes not staged for commit:`
  - `modified:   readme.txt`

## 成功 / 失敗

成功

## 失敗原因

無

## 後續建議

- 後續可補 staged/untracked 狀態 smoke case。
