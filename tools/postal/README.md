# TaiwanUtilities 工具腳本

此目錄包含 TaiwanUtilities 郵遞區號資料管線的工具。

## Download-PostalDatabase.ps1

自動下載中華郵政 3+3 郵遞區號資料庫並提取 rall1.dbf。

### 系統需求

- **作業系統**: Windows（安裝檔為 Windows 專用）
- **7-Zip**: 必須安裝（用於解壓縮 .rar 檔案；自動偵測標準路徑、註冊表與 PATH）
  - 下載: https://www.7-zip.org/
- **PowerShell**: 5.1 或更高版本

### 使用方式

```powershell
# 基本使用（輸出到 temp/rall1.dbf）
.\tools\postal\Download-PostalDatabase.ps1

# 指定輸出路徑
.\tools\postal\Download-PostalDatabase.ps1 -OutputPath "C:\path\to\rall1.dbf"

# 保留暫存檔案（用於除錯）
.\tools\postal\Download-PostalDatabase.ps1 -KeepTemp
```

### 執行流程

1. **取得下載連結** - 從中華郵政網站解析 .rar 檔案連結
2. **下載 .rar 檔案** - 下載所有分割檔案
3. **解壓縮** - 使用 7-Zip 解壓縮得到安裝檔
4. **靜默安裝** - 執行安裝到 `C:\Zip33U\`
5. **複製資料庫** - 從 `C:\Zip33U\DBF\rall1.dbf` 複製到指定位置
6. **清理暫存** - 刪除暫存檔案

## Postal.Builder

.NET 工具專案，將 DBF 展開為靜態 C# 資料（`PostalData.g.cs`），編譯進函式庫、執行期零讀檔。

詳細說明請參閱：[Postal.Builder/README.md](Postal.Builder/README.md)

## 完整工作流程

```powershell
# 1. 下載最新資料
.\tools\postal\Download-PostalDatabase.ps1

# 2. 生成靜態 C# 資料
dotnet run --project tools/postal/Postal.Builder --framework net10.0 -- codegen temp\rall1.dbf src\TaiwanUtilities\Postal\PostalData.g.cs

# 3. 重新建置專案
dotnet build src\TaiwanUtilities\
```

CI 端由 `.github/workflows/update-postal-database.yml` 每季自動執行同樣流程並 commit 生成物。

## 授權

本目錄下的腳本為 TaiwanUtilities 專案的一部分，遵循 MIT 授權條款。
