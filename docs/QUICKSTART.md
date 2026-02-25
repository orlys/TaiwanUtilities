# 快速入門指南

## 安裝

透過 NuGet 安裝：

```bash
dotnet add package TaiwanUtilities
```

## 使用範例

### 基本查詢

```csharp
using TaiwanUtilities;

// 查詢郵遞區號
ZipCodeResult result = ZipCode.Find("臺北市信義區市府路1號");
Console.WriteLine(result.ZipCode);  // 輸出: 110204

// 解析地址組件
PostalAddress address = PostalAddress.Parse("臺北市信義區市府路1號");
Console.WriteLine(address.City);      // 輸出: 臺北市
Console.WriteLine(address.District);  // 輸出: 信義區
```

### 漸進式查詢

即使地址不完整也能查詢：

```csharp
ZipCodeResult result1 = ZipCode.Find("臺北市");
Console.WriteLine(result1.ZipCode);  // 1

ZipCodeResult result2 = ZipCode.Find("臺北市信義區");
Console.WriteLine(result2.ZipCode);  // 110

ZipCodeResult result3 = ZipCode.Find("臺北市信義區市府路");
Console.WriteLine(result3.ZipCode);  // 110204

ZipCodeResult result4 = ZipCode.Find("臺北市信義區市府路1號");
Console.WriteLine(result4.ZipCode);  // 110204
```

### 地址正規化

```csharp
// 自動處理各種格式
string normalized = AddressUtils.Normalize("台北市，信義區，市府路１號");
// 返回: "臺北市信義區市府路1號"

// 中文數字轉換
string normalized2 = AddressUtils.Normalize("臺北市中正區信義路一段");
// 返回: "臺北市中正區信義路1段"
```

### 地址解析和組件提取

```csharp
PostalAddress address = PostalAddress.Parse("臺北市信義區市府路1之2號3樓");
Console.WriteLine(address.City);           // 臺北市
Console.WriteLine(address.District);       // 信義區
Console.WriteLine(address.Road);           // 市府路
Console.WriteLine(address.Number);         // 1
Console.WriteLine(address.SubNumbers[0]);  // 2
Console.WriteLine(address.Floor);          // 3樓
Console.WriteLine(address.GetFullNumber()); // 1之2號
```

### 支援的格式

此套件可以處理各種地址格式：

- **繁簡轉換**：台 ↔ 臺
- **數字格式**：
  - 全形數字：１２３ → 123
  - 中文數字：三十八號 → 38號（路名中的數字不轉換，如「八德路」）
- **分隔符**：自動移除逗號、空格、全形空格
- **附號格式**：1-1號、1之1號 都支援

## 其他模組

### 中文數字

```csharp
// 中文數字解析
long value = ChineseNumeric.Parse("壹仟零伍拾");  // 1050

// 格式化為大寫中文數字
string text = ChineseNumeric.Format(1050);  // "壹仟零伍拾"
```

### 民國曆

```csharp
// 取得今天的民國日期
RocDateTime today = RocDateTime.Now;
Console.WriteLine(today);  // 115/02/25

// 查詢是否為假日
bool isHoliday = RocDateTime.IsHoliday(DateTime.Today);
```

### 身分證驗證

```csharp
bool isValid = TaiwanId.IsValid("A123456789");
```

## 效能

- **資料庫大小**：約 50 MB
- **查詢速度**：毫秒級
- **並行查詢**：執行緒安全，支援高併發

## 執行測試

```bash
dotnet test test/TaiwanUtilities.UnitTests/
```

## 更多資訊

- [完整 API 文件](API.md)
- [內嵌資源說明](EMBEDDED_RESOURCE.md)
- [授權說明](LICENSING.md)
