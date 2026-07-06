# Postal.Builder - 郵遞區號資料工具

將中華郵政 3+3 郵遞區號資料（DBF）展開為靜態 C# 資料，並提供檢查與驗證工具。

## 功能

### 1. 生成靜態資料 (codegen) — 主要用途

將 `rall1.dbf` 展開為 `PostalData.g.cs`（全域 SoA 陣列 + 路名 blob + 階層索引），
編譯進 `TaiwanUtilities.dll`，執行期零讀檔、零反射。

```bash
dotnet run --framework net10.0 -- codegen <input.dbf> <output.g.cs>

# 範例（於 repo 根目錄）
dotnet run --project tools/postal/Postal.Builder --framework net10.0 -- codegen temp/rall1.dbf src/TaiwanUtilities/Postal/PostalData.g.cs
```

CI（`update-postal-database.yml`）每季自動執行此命令並 commit 生成物。

### 2. 檢查 DBF 結構 (inspect)

```bash
dotnet run -- inspect <file.dbf>
```

輸出 DBF 欄位結構與前 5 筆記錄範例。

### 3. 驗證資料集 (validate)

```bash
dotnet run -- validate <file> [--verbose]
```

檢查郵遞區號格式（3/5/6 碼）、必要欄位、重複地址等。

### 4. 統計資訊 (stats)

```bash
dotnet run -- stats <file>
```

### 5. 欄位分析 (analyze-department / analyze-roads)

```bash
dotnet run -- dept    # 分析 DEPARTMENT 欄位
dotnet run -- roads   # 分析路名分佈
```

## 輸入格式

來源：中華郵政「3+3 郵遞區號應用系統」`rall1.dbf`
（可用 `tools/postal/Download-PostalDatabase.ps1` 自動下載）

**使用欄位：**
- `CITY`, `AREA`, `ROAD`: 縣市、區域、道路
- `ZIPCODE`: 郵遞區號（6 碼）
- `SCOOP`: 投遞範圍
- `LANE`, `ALLEY`, `NO_BGN`, `NO_END`, `EVEN` 等結構化欄位

**特點：**
- 官方資料，最準確
- BIG5 編碼，自動處理
- 約 80,000 筆記錄

## 輸出

**位置：** `src/TaiwanUtilities/Postal/PostalData.g.cs`

- 純靜態 C# 資料（primitive array initializer → PE RVA blob）
- 約 45,000 個路名群組、80,000 筆規則
- 由手寫的 `PostalLookup`（三層二分搜尋）查詢

## 技術細節

### 依賴套件

- **DbfDataReader** (0.8.0): 讀取 .dbf 檔案
- **CsvHelper**: 讀取 CSV 資料來源
- **System.Text.Encoding.CodePages**: BIG5 編碼支援

### 編碼處理

```csharp
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
Encoding big5 = Encoding.GetEncoding("big5");
```

## 授權

本工具為 TaiwanUtilities 專案的一部分，遵循 MIT 授權條款。
