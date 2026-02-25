# 內嵌資源功能說明

## 概述

TaiwanUtilities 支援將郵遞區號資料庫嵌入 DLL 中，使用時無需額外的外部檔案。

## 優點

- **無需外部檔案**：資料庫內嵌在 DLL 中，部署更簡單
- **自動提取**：首次使用時自動提取到臨時目錄
- **快取重用**：多個實例共享同一臨時檔案
- **向後相容**：仍可使用外部資料庫檔案

## 使用方式

### 查詢郵遞區號

```csharp
using TaiwanUtilities;

// 自動使用內嵌的資料庫
ZipCodeResult result = ZipCode.Find("臺北市信義區市府路1號");
Console.WriteLine(result.ZipCode);  // 110204
```

### 解析地址組件

```csharp
using TaiwanUtilities;

PostalAddress address = PostalAddress.Parse("臺北市信義區市府路1號");
Console.WriteLine(address.City);      // 臺北市
Console.WriteLine(address.District);  // 信義區
```

## 技術細節

### 資料庫位置

內嵌資源提取到：`%TEMP%\TaiwanUtilities\zipcode.db`

資料庫由 Database 單例類別自動管理，所有查詢都透過內部連接池處理，無需手動管理資料庫路徑。

### 快取機制

- 首次初始化時，從內嵌資源提取資料庫到臨時目錄
- 如果臨時檔案已存在且大小相同，直接重用
- 系統重啟時自動清理臨時檔案

### 效能

- **初始化**：首次約 50-100ms（提取資源），後續 < 10ms（重用檔案）
- **查詢速度**：與使用外部檔案完全相同
- **記憶體**：資料庫不會載入到記憶體，僅在磁碟上

## 建置配置

### TaiwanUtilities.csproj

zipcode.db 在建置時由 `Database.targets` 自動下載（如果不存在），不需要手動管理。

### 建置步驟

1. 建置專案（資料庫會自動下載並嵌入）：
   ```bash
   dotnet build src/TaiwanUtilities/
   ```

2. 如需手動重建資料庫：
   ```powershell
   .\tools\Build-PostalDatabase.ps1
   ```

## 部署

### 單檔案部署

```bash
# 發布為單檔案（包含資料庫）
dotnet publish -c Release -p:PublishSingleFile=true
```

生成的單一執行檔包含所有內容，無需額外檔案。

### NuGet 套件

安裝 NuGet 套件後，資料庫已包含在套件中，無需額外設定。

## 注意事項

### 1. DLL 大小

嵌入資料庫後，`TaiwanUtilities.dll` 大小約 **50 MB**。

### 2. 更新資料

如果郵遞區號資料更新，需要：
1. 重新建立資料庫
2. 重新建置專案

### 3. 臨時檔案清理

臨時資料庫檔案會在以下情況清理：
- 系統重啟
- 手動刪除臨時目錄

### 4. 多版本共存

不同版本的 DLL 可以共存，因為：
- 臨時檔案大小不同時會重新提取
- 多個實例可以同時使用

## API 參考

### ZipCode 類別（靜態）

```csharp
// 查詢郵遞區號
public static ZipCodeResult Find(string address)

// 驗證地址
public static AddressValidationResult ValidateAddress(string address)

// 取得投遞規則
public static List<ZipCodeDeliveryRule> GetDeliveryRules(string address)

// 取得地址候選
public static List<PostalAddressSuggestion> GetSuggestions(string partialAddress, int maxResults = 10)
```

### PostalAddress 類別

```csharp
// 解析地址
public static PostalAddress Parse(string address)

// 驗證地址組件
public static PostalAddressValidation Validate(PostalAddress address)
```

### AddressUtils 類別（靜態）

```csharp
// 正規化地址
public static string Normalize(string address)
```

## 常見問題

### Q: 為什麼不直接從記憶體使用資料庫？

A: SQLite 需要檔案系統支援，從記憶體使用需要特殊配置且效能可能較差。使用臨時檔案是最佳實踐。

### Q: 臨時檔案會占用多少空間？

A: 約 50 MB，與原始資料庫相同。多個實例共享同一檔案。

### Q: 內嵌資源會影響啟動速度嗎？

A: 首次使用時需要提取資源（約 50-100ms），後續使用幾乎無影響。
