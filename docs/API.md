# TaiwanUtilities Postal API 文件

完整的 Postal 模組 API 參考文件，包含所有公開類別、方法和屬性的詳細說明。

---

## 目錄

1. [ZipCode 類別](#zipcode-類別) - 郵遞區號查詢（靜態工具類別）
2. [PostalAddress 類別](#postaladdress-類別) - 地址解析與驗證
3. [AddressUtils 類別](#addressutils-類別) - 地址正規化工具
4. [ZipCodeResult 類別](#zipcoderesult-類別) - 查詢結果
5. [AddressValidationResult 類別](#addressvalidationresult-類別) - 驗證結果
6. [DeliveryRule 類別](#deliveryrule-類別) - 投遞規則
7. [PostalAddressSuggestion 類別](#postaladdresssuggestion-類別) - 地址候選
8. [Database 類別](#database-類別) - 資料庫管理（進階）
9. [使用範例](#使用範例)

---

## ZipCode 類別

台灣郵遞區號查詢的主要入口，提供所有查詢、驗證和建議功能。

```csharp
public static class ZipCode
```

> **注意：** `ZipCode` 是靜態類別，所有方法都是靜態方法，無需建立實例。

### 靜態方法

#### Find

查詢地址的郵遞區號（返回詳細結果）。

```csharp
public static ZipCodeResult Find(string address)
```

**參數：**
- `address` (string)：台灣地址字串

**返回值：**
- `ZipCodeResult`：完整的查詢結果

**範例：**
```csharp
ZipCodeResult result = ZipCode.Find("臺北市信義區市府路1號");
Console.WriteLine($"郵遞區號: {result.ZipCode}");          // 110204
Console.WriteLine($"類型: {result.ResultType}");           // ExactMatch
Console.WriteLine($"縣市: {result.Address.City}");         // 臺北市
Console.WriteLine($"區: {result.Address.District}");       // 信義區
if (result.MatchedRule != null)
    Console.WriteLine($"規則: {result.MatchedRule.GetDescription()}");
```

#### ValidateAddress

驗證地址是否合法且門牌號碼在合理範圍內。

```csharp
public static AddressValidationResult ValidateAddress(string address)
```

**參數：**
- `address` (string)：完整地址字串

**返回值：**
- `AddressValidationResult`：驗證結果

**範例：**
```csharp
// 有效地址
AddressValidationResult result1 = ZipCode.ValidateAddress("臺北市信義區市府路1號");
// result1.IsValid = true, result1.ZipCode = "110204"

// 門牌號碼超出範圍
AddressValidationResult result2 = ZipCode.ValidateAddress("臺北市信義區市府路99999號");
// result2.IsValid = false, result2.FailureReason = NumberOutOfRange

// 不存在的地址
AddressValidationResult result3 = ZipCode.ValidateAddress("臺北市不存在區某某路1號");
// result3.IsValid = false, result3.FailureReason = AddressNotFound
```

#### GetDeliveryRules

取得地址的所有投遞規則。

```csharp
public static List<ZipCodeDeliveryRule> GetDeliveryRules(string address)
```

**參數：**
- `address` (string)：地址字串

**返回值：**
- `List<ZipCodeDeliveryRule>`：郵遞區號和投遞規則的清單

**範例：**
```csharp
List<ZipCodeDeliveryRule> rules = ZipCode.GetDeliveryRules("臺北市中正區三元街");

foreach (ZipCodeDeliveryRule item in rules)
{
    Console.WriteLine($"郵遞區號: {item.ZipCode}");
    Console.WriteLine($"規則類型: {item.Rule.Type}");
    Console.WriteLine($"規則描述: {item.Rule.GetDescription()}");
}
```

#### GetSuggestions

取得地址候選清單（自動完成）。

```csharp
public static List<PostalAddressSuggestion> GetSuggestions(string partialAddress, int maxResults = 10)
```

**參數：**
- `partialAddress` (string)：部分地址
- `maxResults` (int, 可選)：最大返回數量，預設為 10

**返回值：**
- `List<PostalAddressSuggestion>`：候選地址清單

**範例：**
```csharp
List<PostalAddressSuggestion> suggestions = ZipCode.GetSuggestions("臺北市中正區中", 5);

foreach (PostalAddressSuggestion suggestion in suggestions)
{
    Console.WriteLine($"地址: {suggestion.AddressText}");
    Console.WriteLine($"郵遞區號: {suggestion.ZipCode}");
}
```

---

## PostalAddress 類別

代表結構化的台灣郵政地址，提供地址解析和驗證功能。

### 屬性

#### 基本組件

| 屬性名稱 | 型別 | 說明 |
|---------|------|------|
| `City` | `string?` | 縣市（如：臺北市） |
| `District` | `string?` | 行政區（如：信義區） |
| `Village` | `string?` | 村里 |
| `Neighborhood` | `string?` | 鄰 |
| `Road` | `string?` | 路街（如：市府路） |
| `Section` | `string?` | 段 |
| `Lane` | `string?` | 巷 |
| `Alley` | `string?` | 弄 |
| `Number` | `int?` | 門牌號碼 |
| `SubNumbers` | `List<int>?` | 附號（支援多層如：150之1之1之1 → [1, 1, 1]） |
| `Floor` | `string?` | 樓層 |
| `Locality` | `string?` | 地區名稱（部落、眷村、聚落等） |

#### 原始資料

| 屬性名稱 | 型別 | 說明 |
|---------|------|------|
| `RawAddress` | `string` | 原始地址字串 |
| `NormalizedAddress` | `string` | 正規化地址字串 |

### 靜態方法

#### Parse

從地址字串解析組件。

```csharp
public static PostalAddress Parse(string address)
```

**範例：**
```csharp
PostalAddress address = PostalAddress.Parse("臺北市信義區市府路1之2號3樓");
Console.WriteLine($"縣市: {address.City}");              // 臺北市
Console.WriteLine($"區: {address.District}");            // 信義區
Console.WriteLine($"路: {address.Road}");                // 市府路
Console.WriteLine($"號: {address.Number}");              // 1
Console.WriteLine($"附號: {address.SubNumbers[0]}");     // 2
Console.WriteLine($"樓: {address.Floor}");               // 3樓
```

#### TryParse

嘗試解析地址，失敗時返回 false。

```csharp
public static bool TryParse(string address, out PostalAddress result)
```

#### Validate

驗證地址組件有效性（縣市、行政區、路街是否存在）。

```csharp
public static PostalAddressValidation Validate(PostalAddress address)
```

**範例：**
```csharp
PostalAddress address = PostalAddress.Parse("臺北市信義區市府路1號");
PostalAddressValidation validation = PostalAddress.Validate(address);
Console.WriteLine($"縣市有效: {validation.IsValidCity}");      // true
Console.WriteLine($"行政區有效: {validation.IsValidDistrict}");  // true
Console.WriteLine($"路街有效: {validation.IsValidRoad}");       // true
Console.WriteLine($"整體有效: {validation.IsValid}");           // true

foreach (string msg in validation.Messages)
{
    Console.WriteLine($"訊息: {msg}");
}
```

### 實例方法

#### GetFullNumber

取得完整門牌號碼（含所有附號）。

```csharp
public string GetFullNumber()
```

**範例：**
```csharp
PostalAddress address1 = PostalAddress.Parse("臺北市信義區市府路1之2號");
Console.WriteLine(address1.GetFullNumber()); // "1之2號"

PostalAddress address2 = PostalAddress.Parse("台中市中區平等街150之1之1之1號");
Console.WriteLine(address2.GetFullNumber()); // "150之1之1之1號"
```

#### GetBaseAddress

取得基本地址（縣市+區+路/村里/歷史地名）。

```csharp
public string GetBaseAddress()
```

**範例：**
```csharp
PostalAddress address1 = PostalAddress.Parse("臺北市信義區市府路1號");
Console.WriteLine(address1.GetBaseAddress()); // "臺北市信義區市府路"

PostalAddress address2 = PostalAddress.Parse("高雄市阿蓮區再興23號");
Console.WriteLine(address2.GetBaseAddress()); // "高雄市阿蓮區再興"
```

---

## AddressUtils 類別

地址正規化工具（靜態類別）。

```csharp
public static class AddressUtils
```

### 靜態方法

#### Normalize

正規化地址字串（統一格式）。

```csharp
public static string Normalize(string address)
```

**範例：**
```csharp
string normalized = AddressUtils.Normalize("台北市，信義區，市府路１號");
// 返回 "臺北市信義區市府路1號"

string normalized2 = AddressUtils.Normalize("信義路一段");
// 返回 "信義路1段"
```

---

## ZipCodeResult 類別

郵遞區號查詢的完整結果。

### 屬性

| 屬性名稱 | 型別 | 說明 |
|---------|------|------|
| `ResultType` | `ZipCodeResultType` | 結果類型（ExactMatch, PartialMatch, NotFound） |
| `ZipCode` | `string` | 郵遞區號（3或6碼） |
| `ZipCode3` | `string` | 3碼郵遞區號 |
| `ZipCode5` | `string?` | 6碼郵遞區號（如果有） |
| `OriginalAddress` | `string` | 原始地址 |
| `NormalizedAddress` | `string` | 正規化地址 |
| `Address` | `PostalAddress?` | 解析的郵政地址組件 |
| `MatchedRule` | `DeliveryRule?` | 匹配的投遞規則 |
| `MatchedScope` | `string?` | 匹配的地址範圍 |
| `IsValid` | `bool` | 是否找到郵遞區號 |
| `IsExactMatch` | `bool` | 是否為完整匹配 |
| `Messages` | `List<string>` | 額外訊息 |
| `Suggestions` | `List<string>` | 建議的地址候選 |

### 列舉：ZipCodeResultType

查詢結果類型：

- `ExactMatch`：完整匹配（有明確規則）
- `PartialMatch`：部分匹配（漸進式查詢）
- `NotFound`：未找到

**範例：**
```csharp
ZipCodeResult result = ZipCode.Find("臺北市信義區市府路1號");

switch (result.ResultType)
{
    case ZipCodeResultType.ExactMatch:
        Console.WriteLine($"找到完整匹配: {result.ZipCode}");
        break;
    case ZipCodeResultType.PartialMatch:
        Console.WriteLine($"找到部分匹配: {result.ZipCode}");
        break;
    case ZipCodeResultType.NotFound:
        Console.WriteLine("找不到地址");
        break;
}
```

---

## AddressValidationResult 類別

地址驗證結果。

### 屬性

| 屬性名稱 | 型別 | 說明 |
|---------|------|------|
| `IsValid` | `bool` | 驗證是否通過 |
| `ZipCode` | `string` | 找到的郵遞區號（如果驗證通過） |
| `NormalizedAddress` | `string` | 正規化後的地址 |
| `Messages` | `List<string>` | 驗證訊息 |
| `FailureReason` | `ValidationFailureReason` | 驗證失敗的原因 |
| `Suggestions` | `List<string>` | 建議的正確地址（如果有） |

### 列舉：ValidationFailureReason

驗證失敗原因：

- `None`：無（驗證通過）
- `InvalidFormat`：地址格式無效
- `AddressNotFound`：找不到地址
- `NumberOutOfRange`：門牌號碼超出範圍
- `NumberRuleMismatch`：門牌號碼不符合規則（例如：單雙號）
- `DistrictNotFound`：區域不存在
- `StreetNotFound`：街道不存在

**範例：**
```csharp
AddressValidationResult result = ZipCode.ValidateAddress("臺北市信義區市府路99999號");

if (!result.IsValid)
{
    Console.WriteLine($"驗證失敗: {result.FailureReason}");
    foreach (string msg in result.Messages)
    {
        Console.WriteLine($"  - {msg}");
    }

    if (result.Suggestions.Count > 0)
    {
        Console.WriteLine("建議的地址:");
        foreach (string suggestion in result.Suggestions)
        {
            Console.WriteLine($"  - {suggestion}");
        }
    }
}
```

---

## DeliveryRule 類別

代表郵遞投遞規則。

### 屬性

| 屬性名稱 | 型別 | 說明 |
|---------|------|------|
| `Type` | `RuleType` | 規則類型 |
| `StartNumber` | `int?` | 起始號碼 |
| `EndNumber` | `int?` | 結束號碼 |
| `SpecificNumber` | `int?` | 指定號碼 |
| `SpecificSubNumber` | `int?` | 指定附號 |
| `RawRule` | `string` | 原始規則字串 |

### 靜態方法

#### Parse

從規則字串解析投遞規則。

```csharp
public static DeliveryRule Parse(string fullRuleString)
```

**範例：**
```csharp
DeliveryRule rule = DeliveryRule.Parse("臺北市中正區三元街單147號以下");
Console.WriteLine(rule.Type);          // LessOrEqual
Console.WriteLine(rule.EndNumber);     // 147
Console.WriteLine(rule.GetDescription()); // "單號，147號以下"
```

### 實例方法

#### Matches

檢查地址是否符合規則。

```csharp
public bool Matches(PostalAddress components)
```

**範例：**
```csharp
DeliveryRule rule = DeliveryRule.Parse("臺北市中正區三元街單147號以下");
PostalAddress addr1 = PostalAddress.Parse("臺北市中正區三元街145號");
PostalAddress addr2 = PostalAddress.Parse("臺北市中正區三元街150號");
PostalAddress addr3 = PostalAddress.Parse("臺北市中正區三元街146號");

Console.WriteLine(rule.Matches(addr1)); // true（145是單號且小於147）
Console.WriteLine(rule.Matches(addr2)); // false（150大於147）
Console.WriteLine(rule.Matches(addr3)); // false（146是雙號）
```

#### GetDescription

取得人類可讀的規則描述。

```csharp
public string GetDescription()
```

**範例：**
```csharp
DeliveryRule rule1 = DeliveryRule.Parse("臺北市中正區三元街單147號以下");
Console.WriteLine(rule1.GetDescription()); // "單號，147號以下"

DeliveryRule rule2 = DeliveryRule.Parse("臺北市信義區市府路1號至45號");
Console.WriteLine(rule2.GetDescription()); // "1號至45號"

DeliveryRule rule3 = DeliveryRule.Parse("臺北市大安區復興南路雙200號以上");
Console.WriteLine(rule3.GetDescription()); // "雙號，200號以上"
```

### 列舉：RuleType

規則類型：

- `All`：全部門牌
- `Odd`：單號
- `Even`：雙號
- `Specific`：指定號碼
- `Range`：號碼範圍
- `GreaterOrEqual`：大於等於
- `LessOrEqual`：小於等於
- `WithSubNumber`：含附號
- `SubNumberOnly`：僅附號
- `SubNumberAbove`：附號以上
- `SubNumberBelow`：附號以下

---

## PostalAddressSuggestion 類別

郵政地址候選項目（用於自動完成）。

### 屬性

| 屬性名稱 | 型別 | 說明 |
|---------|------|------|
| `AddressText` | `string` | 候選地址字串 |
| `ZipCode` | `string` | 郵遞區號 |
| `Address` | `PostalAddress?` | 結構化郵政地址 |

**範例：**
```csharp
List<PostalAddressSuggestion> suggestions = ZipCode.GetSuggestions("臺北市中正區中", 5);

foreach (PostalAddressSuggestion suggestion in suggestions)
{
    Console.WriteLine($"地址: {suggestion.AddressText}");
    Console.WriteLine($"郵遞區號: {suggestion.ZipCode}");

    if (suggestion.Address != null)
    {
        Console.WriteLine($"縣市: {suggestion.Address.City}");
        Console.WriteLine($"區: {suggestion.Address.District}");
        Console.WriteLine($"路: {suggestion.Address.Road}");
    }
    Console.WriteLine();
}
```

---

## Database 類別

郵遞區號資料庫管理類別（進階用法）。

### 靜態方法

#### UseExternalDatabase

使用外部資料庫路徑（用於測試或自訂資料庫）。

```csharp
public static void UseExternalDatabase(string dbPath)
```

**注意：**
- 此方法必須在首次使用 Database 單例之前呼叫
- 如果單例已經初始化，此方法將拋出 InvalidOperationException

**範例：**
```csharp
// 必須在任何查詢之前呼叫
Database.UseExternalDatabase("D:\\custom_zipcode.db");

// 現在所有查詢都使用自訂資料庫
ZipCodeResult result = ZipCode.Find("臺北市信義區市府路1號");
```

#### CheckForUpdatesAsync

檢查是否有可用的資料庫更新。

```csharp
public static Task<DatabaseUpdateInfo?> CheckForUpdatesAsync(CancellationToken ct = default)
```

**範例：**
```csharp
DatabaseUpdateInfo? updateInfo = await Database.CheckForUpdatesAsync();
if (updateInfo != null && updateInfo.HasUpdate)
{
    Console.WriteLine($"發現新版本: {updateInfo.RemoteVersion.Version}");
    Console.WriteLine($"目前版本: {updateInfo.LocalVersion?.Version}");
    Console.WriteLine($"記錄數量: {updateInfo.RemoteVersion.RecordCount}");
}
```

#### UpdateAsync

從 GitHub Release 更新資料庫。

```csharp
public static Task<bool> UpdateAsync(CancellationToken ct = default)
```

**範例：**
```csharp
bool success = await Database.UpdateAsync();
if (success)
{
    Console.WriteLine("資料庫更新成功！");
    Console.WriteLine($"新版本: {Database.CurrentVersion?.Version}");
}
```

#### Reload

強制重新載入資料庫（清除所有執行緒的快取）。

```csharp
public static void Reload()
```

---

## 使用範例

### 1. 基本查詢

```csharp
using TaiwanUtilities;

ZipCodeResult result = ZipCode.Find("臺北市信義區市府路1號");
Console.WriteLine(result.ZipCode);  // 110204
```

### 2. 地址解析

```csharp
PostalAddress address = PostalAddress.Parse("臺北市信義區市府路1之2號3樓");

Console.WriteLine($"縣市: {address.City}");              // 臺北市
Console.WriteLine($"區: {address.District}");            // 信義區
Console.WriteLine($"路: {address.Road}");                // 市府路
Console.WriteLine($"號: {address.Number}");              // 1
Console.WriteLine($"附號: {address.SubNumbers?[0]}");    // 2
Console.WriteLine($"樓: {address.Floor}");               // 3樓
Console.WriteLine($"完整號碼: {address.GetFullNumber()}"); // 1之2號
```

### 3. 批次查詢

```csharp
string[] addresses = new[]
{
    "臺北市信義區市府路1號",
    "高雄市左營區大中一路331號",
    "新北市板橋區中山路一段2號"
};

foreach (string addr in addresses)
{
    ZipCodeResult result = ZipCode.Find(addr);
    Console.WriteLine($"{addr} => {result.ZipCode}");
}
```

### 4. 地址驗證

```csharp
// 驗證有效地址
AddressValidationResult result1 = ZipCode.ValidateAddress("臺北市信義區市府路1號");
Console.WriteLine($"有效: {result1.IsValid}");        // true
Console.WriteLine($"郵遞區號: {result1.ZipCode}");    // 110204

// 驗證無效地址（門牌號碼超出範圍）
AddressValidationResult result2 = ZipCode.ValidateAddress("臺北市信義區市府路99999號");
Console.WriteLine($"有效: {result2.IsValid}");        // false
Console.WriteLine($"失敗原因: {result2.FailureReason}"); // NumberOutOfRange
```

### 5. 投遞規則查詢

```csharp
List<ZipCodeDeliveryRule> rules = ZipCode.GetDeliveryRules("臺北市中正區三元街");

foreach (ZipCodeDeliveryRule item in rules)
{
    Console.WriteLine($"郵遞區號: {item.ZipCode}");
    Console.WriteLine($"規則類型: {item.Rule.Type}");
    Console.WriteLine($"規則描述: {item.Rule.GetDescription()}");
    Console.WriteLine();
}
```

### 6. 地址自動完成

```csharp
List<PostalAddressSuggestion> suggestions = ZipCode.GetSuggestions("臺北市中正區中", 5);

foreach (PostalAddressSuggestion suggestion in suggestions)
{
    Console.WriteLine($"{suggestion.AddressText} [{suggestion.ZipCode}]");
}
```

### 7. 漸進式匹配

```csharp
ZipCodeResult result1 = ZipCode.Find("臺北市");
Console.WriteLine($"{result1.ZipCode} ({result1.ResultType})");  // 1 (PartialMatch)

ZipCodeResult result2 = ZipCode.Find("臺北市信義區");
Console.WriteLine($"{result2.ZipCode} ({result2.ResultType})");  // 110 (PartialMatch)

ZipCodeResult result3 = ZipCode.Find("臺北市信義區市府路1號");
Console.WriteLine($"{result3.ZipCode} ({result3.ResultType})");  // 110204 (ExactMatch)
```

### 8. 規則匹配測試

```csharp
DeliveryRule rule = DeliveryRule.Parse("臺北市中正區三元街單147號以下");
string[] addresses = new[]
{
    "臺北市中正區三元街145號",  // true（單號且小於147）
    "臺北市中正區三元街150號",  // false（大於147）
    "臺北市中正區三元街146號",  // false（雙號）
};

foreach (string addr in addresses)
{
    PostalAddress parsed = PostalAddress.Parse(addr);
    bool matches = rule.Matches(parsed);
    Console.WriteLine($"{addr} => {matches}");
}
```

### 9. 地址正規化

```csharp
string normalized = AddressUtils.Normalize("台北市，信義區，市府路１號");
// 返回 "臺北市信義區市府路1號"

string normalized2 = AddressUtils.Normalize("信義路一段");
// 返回 "信義路1段"
```

---

## 執行緒安全性

- `Database` 類別使用單例模式，執行緒安全
- `ZipCode` 是靜態類別，所有方法都是執行緒安全的
- `PostalAddress` 和 `DeliveryRule` 類別是不可變的，可以安全地在多執行緒間共享
- 所有查詢方法都是執行緒安全的

**範例：**
```csharp
// 多執行緒查詢
IEnumerable<Task<ZipCodeResult>> tasks = addresses.Select(addr => Task.Run(() =>
{
    return ZipCode.Find(addr);
}));

ZipCodeResult[] results = await Task.WhenAll(tasks);
```

---

## 其他資源

- [README.md](../README.md)：專案概述和快速開始
- [QUICKSTART.md](QUICKSTART.md)：5 分鐘入門指南
- [EMBEDDED_RESOURCE.md](EMBEDDED_RESOURCE.md)：內嵌資源技術細節
- [地址生成器 API](POSTAL_ADDRESS_GENERATOR_API.md)：測試地址生成
