# GitHub Actions Workflows

此目錄包含 TaiwanUtilities 專案的自動化工作流程。

## ci.yml

持續整合與持續部署流程。

### 觸發方式
- **Pull Request** → 建置和測試
- **推送到 master** → 建置、測試、發布預覽版本
- **推送標籤 (v*)** → 建置、測試、發布正式版本

### 建置矩陣
- **平台**: Ubuntu、Windows、macOS
- **步驟**: 還原 → 下載郵遞區號 DB → 建置 → 測試 → 打包 → 發布

---

## update-holidays.yml

每日同步辦公日曆表資料。

### 觸發方式
- **定期執行**: 每天 UTC 02:00（台灣時間 10:00）
- **手動觸發**: workflow_dispatch

### 執行步驟
1. 從行政院人事行政總處 API 取得最新辦公日曆表
2. 解析 CSV 資料，分類假日類型（一般、勞動節、軍人節）
3. 寫入 `data/holidays/<年份>.csv`
4. 如有變更，合併所有年份並建立 GitHub Release

---

## update-postal-database.yml

每季更新郵遞區號資料庫。

### 觸發方式
- **定期執行**: 每季首日（1/1, 4/1, 7/1, 10/1）
- **手動觸發**: workflow_dispatch

### 執行步驟
1. 下載中華郵政最新 rall1.dbf 資料
2. 使用 Postal.Builder 建立 SQLite 資料庫
3. 執行測試驗證資料庫完整性
4. 建立 Pull Request 包含更新

### 系統需求
- **Runner**: `windows-latest`（安裝檔僅支援 Windows）
- **.NET SDK**: 10.0
- **7-Zip**: Windows runner 已預裝

---

## 故障排除

### 下載失敗
- 中華郵政或政府資料平台暫時無法訪問
- 檢查 workflow 日誌確認錯誤

### 測試失敗
- 資料變更導致測試案例失效
- 檢查測試日誌，更新測試案例

### 權限需求
- `contents: write` - 建立分支和提交
- `pull-requests: write` - 建立 Pull Request

這些權限由 `GITHUB_TOKEN` 自動提供。

---

## 相關資源

- [工具腳本文件](../../tools/README.md)
- [Postal.Builder 文件](../../tools/Postal.Builder/README.md)
- [專案 README](../../README.md)
