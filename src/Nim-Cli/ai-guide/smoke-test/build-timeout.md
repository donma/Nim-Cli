# build timeout smoke

- 目的：確認 `build_project` timeout 會被視為失敗。
- 結果：通過。
- 行為：provider timeout 後，tool 層不再誤判為成功。
