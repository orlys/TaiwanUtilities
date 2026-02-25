# 內嵌資源說明

## 概述

TaiwanUtilities 將郵遞區號資料庫（`zipcode.db`）嵌入 DLL 中，部署時無需額外檔案。

## 運作方式

1. **建置時**：`PostalDatabase.targets` 自動從 GitHub Release 下載 `zipcode.db`（若不存在）
2. **執行時**：首次存取時自動從嵌入資源提取到臨時目錄
3. **快取**：後續啟動直接重用臨時檔案（比對大小）

### 臨時檔案位置

```
%TEMP%/TaiwanUtilities/zipcode.db
```

## 效能

| 階段 | 耗時 |
|------|------|
| 首次初始化（提取資源） | ~50-100ms |
| 後續啟動（重用檔案） | < 10ms |
| 查詢速度 | 與外部檔案相同 |

資料庫不載入記憶體，僅在磁碟上透過 SQLite 存取。

## 建置

```bash
# 建置專案（資料庫會自動下載並嵌入）
dotnet build src/TaiwanUtilities/

# 手動重建資料庫
.\tools\Build-PostalDatabase.ps1
```

## 部署

### NuGet 套件

安裝套件後即可使用，無需額外設定。

### 單檔案發布

```bash
dotnet publish -c Release -p:PublishSingleFile=true
```

生成的執行檔包含所有內容。

## 資料庫更新

內嵌資料庫隨 NuGet 版本更新。如需執行時更新：

```csharp
// 檢查更新
var info = await PostalDatabase.CheckForUpdatesAsync();
if (info?.HasUpdate == true)
{
    await PostalDatabase.UpdateAsync();
    PostalDatabase.Reload();
}
```

## 注意事項

- DLL 大小約 **50 MB**（含嵌入資料庫）
- 臨時檔案在系統重啟時自動清理
- 不同版本 DLL 可共存（大小不同時自動重新提取）
