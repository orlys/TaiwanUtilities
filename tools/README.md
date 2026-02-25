# TaiwanUtilities 工具腳本

此目錄包含 TaiwanUtilities 專案的輔助工具腳本。

## Download-PostalDatabase.ps1

自動下載中華郵政 3+3 郵遞區號資料庫並提取 rall1.dbf。

### 系統需求

- **作業系統**: Windows（安裝檔為 Windows 專用）
- **7-Zip**: 必須安裝（用於解壓縮 .rar 檔案）
  - 下載: https://www.7-zip.org/
- **PowerShell**: 5.1 或更高版本

### 使用方式

```powershell
# 基本使用（輸出到 data/rall1.dbf）
.\tools\Download-PostalDatabase.ps1

# 指定輸出路徑
.\tools\Download-PostalDatabase.ps1 -OutputPath "C:\path\to\rall1.dbf"

# 保留暫存檔案（用於除錯）
.\tools\Download-PostalDatabase.ps1 -KeepTemp
```

### 執行流程

1. **取得下載連結** - 從中華郵政網站解析 .rar 檔案連結
2. **下載 .rar 檔案** - 下載所有分割檔案
3. **解壓縮** - 使用 7-Zip 解壓縮得到安裝檔
4. **靜默安裝** - 執行安裝到 `C:\Zip33U\`
5. **複製資料庫** - 從 `C:\Zip33U\DBF\rall1.dbf` 複製到指定位置
6. **清理暫存** - 刪除暫存檔案

## Build-PostalDatabase.ps1

從 rall1.dbf 建立 SQLite 資料庫的包裝腳本。

### 使用方式

```powershell
# 基本使用（預設路徑）
.\tools\Build-PostalDatabase.ps1

# 指定輸入和輸出路徑
.\tools\Build-PostalDatabase.ps1 -DbfPath "data\rall1.dbf" -OutputPath "src\TaiwanUtilities\Postal\zipcode.db"
```

## Ensure-Database.ps1

確保 zipcode.db 存在，不存在時自動下載。用於 CI 環境。

```powershell
.\tools\Ensure-Database.ps1
```

## Create-DatabaseRelease.ps1

建立資料庫 GitHub Release 的腳本。

```powershell
.\tools\Create-DatabaseRelease.ps1
```

## 完整工作流程

```powershell
# 1. 下載最新資料庫
.\tools\Download-PostalDatabase.ps1

# 2. 建立 SQLite 資料庫
.\tools\Build-PostalDatabase.ps1

# 3. 重新建置專案（內嵌新資料庫）
dotnet build src\TaiwanUtilities\
```

## Peak.Builder

.NET 工具專案，實際執行 DBF 到 SQLite 的轉換。

詳細說明請參閱：[Peak.Builder/README.md](Peak.Builder/README.md)

## 授權

本目錄下的腳本為 TaiwanUtilities 專案的一部分，遵循 MIT 授權條款。
