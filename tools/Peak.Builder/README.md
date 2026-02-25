# Peak.Builder - 郵遞區號資料庫建立與管理工具

一站式工具，用於建立、檢查和驗證台灣郵遞區號資料集。

## 功能

### 1. 建立資料庫 (build) - 預設

將台灣郵遞區號資料（DBF/CSV/JSON）轉換為 SQLite 資料庫。

```bash
dotnet run
# 或
dotnet run -- build
```

**預設：**
- 輸入：`../../dataset/rall1.dbf`
- 輸出：`../Peak/zipcode.db`（自動內嵌到 Peak.dll）

**自訂輸入輸出：**
```bash
dotnet run -- build <輸入檔案> <輸出資料庫>

# 範例
dotnet run -- build ../../dataset/rall1.dbf ../Peak/zipcode.db
dotnet run -- build ../../dataset/zipcode.json ../Peak/zipcode.db
dotnet run -- build ../../dataset/data.csv ../Peak/zipcode.db
```

### 2. 檢查 DBF 結構 (inspect)

檢查 .dbf 檔案的欄位結構和資料內容。

```bash
dotnet run -- inspect <file.dbf>
```

**範例：**
```bash
dotnet run -- inspect ../../dataset/rall1.dbf
```

**輸出：**
- DBF 檔案資訊（欄位數量等）
- 欄位結構（欄位名稱、型別、長度）
- 前 5 筆記錄範例

### 3. 驗證資料集 (validate)

驗證 JSON 資料集的正確性。

```bash
dotnet run -- validate <file.json> [--verbose]
```

**範例：**
```bash
# 基本驗證
dotnet run -- validate ../../dataset/zipcode.json

# 詳細驗證
dotnet run -- validate ../../dataset/zipcode.json --verbose
```

**檢查項目：**
- ❌ 錯誤：JSON 格式、郵遞區號格式（3/5/6 碼）、必要欄位
- ⚠️ 警告：英文名稱缺失、重複地址
- ℹ️ 資訊：提示訊息

### 4. 統計資訊 (stats)

顯示 JSON 資料集的統計資訊。

```bash
dotnet run -- stats <file.json>
```

**範例：**
```bash
dotnet run -- stats ../../dataset/zipcode.json
```

**輸出：**
- 總縣市數、區域數、道路數、規則數
- 錯誤/警告/資訊數量

## 支援格式

### 1. DBF 格式（推薦）

來源：中華郵政「3+3 郵遞區號應用系統」

**使用欄位：**
- `CITY`, `AREA`, `ROAD`: 縣市、區域、道路
- `ZIPCODE`: 郵遞區號（6 碼）
- `SCOOP`: 投遞範圍

**特點：**
- 官方資料，最準確
- BIG5 編碼，自動處理
- 約 80,000 筆記錄

### 2. JSON 格式

階層式結構：

```json
{
  "臺北市": {
    "en": "Taipei City",
    "areas": {
      "信義區": {
        "en": "Xinyi Dist.",
        "roads": {
          "市府路": {
            "en": "Shifu Rd.",
            "scopes": [
              { "scope": "全", "zipcode": 110204 }
            ]
          }
        }
      }
    }
  }
}
```

### 3. CSV 格式

欄位：`zipcode, city, area, road, scope`

```csv
zipcode,city,area,road,scope
110204,臺北市,信義區,市府路,全
```

## 更新資料集流程

1. 取得最新的 `rall1.dbf`，放置於 `dataset/` 目錄
2. 建立資料庫：
   ```bash
   cd src/Peak.Builder
   dotnet run
   ```
3. 重新建置 Peak 專案以內嵌新資料庫：
   ```bash
   cd ../Peak
   dotnet build
   ```

## 輸出資料庫

**位置：** `src/Peak/zipcode.db`

- 大小：約 27 MB
- 記錄：約 80,000 筆
- 自動內嵌到 `Peak.dll` 作為資源
- 建立時間：約 10-12 秒

**資料表：**
1. `precise` - 精確規則匹配
2. `gradual` - 漸進式查詢索引

## 技術細節

### 依賴套件

- **DbfDataReader** (0.8.0): 讀取 .dbf 檔案
- **CsvHelper**: 讀取 CSV 檔案
- **System.Text.Json**: 讀取/驗證 JSON
- **System.Text.Encoding.CodePages**: BIG5 編碼支援

### 編碼處理

```csharp
// 自動註冊 BIG5 編碼
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
var big5 = Encoding.GetEncoding("big5");
```

### 漸進式索引

為支援部分地址查詢，會插入所有地址前綴：

```
臺北市信義區市府路 → 插入：
  - 臺北市
  - 臺北市信義區
  - 臺北市信義區市府路
```

## 完整命令列表

```bash
# 建立資料庫（預設）
dotnet run
dotnet run -- build
dotnet run -- build <input> <output>

# 檢查 DBF
dotnet run -- inspect <file.dbf>

# 驗證 JSON
dotnet run -- validate <file.json>
dotnet run -- validate <file.json> --verbose

# 統計資訊
dotnet run -- stats <file.json>

# 說明
dotnet run -- help
```

## 故障排除

### 找不到輸入檔案

```bash
ls ../../dataset/rall1.dbf
```

### BIG5 編碼錯誤

確保 `System.Text.Encoding.CodePages` 套件已安裝且版本 >= 10.0.0

### 記憶體不足

```bash
export DOTNET_GCHeapHardLimit=2000000000
dotnet run
```

## 授權

本工具為 Peak 專案的一部分，遵循 MIT 授權條款。
