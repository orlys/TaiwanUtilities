# TaiwanUtilities Postal API 文件

郵遞區號模組的完整 API 參考。

---

## 目錄

1. [ZipCode](#zipcode-類別) — 郵遞區號查詢
2. [PostalAddress](#postaladdress-類別) — 地址解析
3. [ZipCodeResult](#zipcoderesult-類別) — 查詢結果
4. [PostalValidationResult](#addressvalidationresult-類別) — 驗證結果
5. [PostalDeliveryRule](#deliveryrule-類別) — 投遞規則
6. [PostalAddressSuggestion](#postaladdresssuggestion-類別) — 地址候選
7. [PostalRulesEngine](#postalrulesengine-類別) — 引擎與資料版本

---

## ZipCode 類別

郵遞區號查詢的主要入口。

```csharp
public static class ZipCode
```

### Find

```csharp
public static ZipCodeResult Find(string address)
```

查詢地址的郵遞區號。支援完整地址（精確匹配）和部分地址（漸進式匹配）。

```csharp
ZipCodeResult result = ZipCode.Find("臺北市信義區市府路1號");
// result.ZipCode: "110204"
// result.ResultType: ExactMatch
```

### ValidateAddress

```csharp
public static PostalValidationResult ValidateAddress(string address)
```

驗證地址是否合法，包含門牌號碼範圍檢查。

```csharp
var r1 = ZipCode.ValidateAddress("臺北市信義區市府路1號");
// r1.IsValid: true, r1.ZipCode: "110204"

var r2 = ZipCode.ValidateAddress("臺北市信義區市府路99999號");
// r2.IsValid: false, r2.FailureReason: NumberOutOfRange
```

### GetDeliveryRules

```csharp
public static List<ZipCodeDeliveryRule> GetDeliveryRules(string address)
```

取得地址的所有投遞規則。

```csharp
var rules = ZipCode.GetDeliveryRules("臺北市中正區三元街");
foreach (var item in rules)
{
    Console.WriteLine($"{item.ZipCode} — {item.Rule.GetDescription()}");
}
```

### GetSuggestions

```csharp
public static List<PostalAddressSuggestion> GetSuggestions(
    string partialAddress, int maxResults = 10)
```

取得地址候選清單（自動完成）。

```csharp
var suggestions = ZipCode.GetSuggestions("臺北市中正區中", 5);
foreach (var s in suggestions)
{
    Console.WriteLine($"{s.AddressText} [{s.ZipCode}]");
}
```

---

## PostalAddress 類別

結構化的台灣郵政地址。

```csharp
public class PostalAddress
```

### 屬性

| 屬性 | 型別 | 說明 |
|------|------|------|
| `City` | `string?` | 縣市 |
| `District` | `string?` | 行政區 |
| `Village` | `string?` | 村里 |
| `Neighborhood` | `string?` | 鄰 |
| `Road` | `string?` | 路街 |
| `Section` | `string?` | 段 |
| `Lane` | `string?` | 巷 |
| `Alley` | `string?` | 弄 |
| `SubAlley` | `string?` | 子弄 |
| `Number` | `int?` | 門牌號碼 |
| `SubNumbers` | `List<int>?` | 附號（多層：150之1之1 → [1, 1]） |
| `IsTemporary` | `bool` | 臨時門牌 |
| `IsBasement` | `bool` | 地下室 |
| `Floor` | `string?` | 樓層 |
| `SubFloor` | `int?` | 之幾樓 |
| `Room` | `string?` | 室 |
| `Locality` | `string?` | 地區名稱（部落、眷村等） |
| `RawAddress` | `string` | 原始地址 |
| `NormalizedAddress` | `string` | 正規化地址 |

### 靜態方法

#### Parse / TryParse

```csharp
public static PostalAddress Parse(string address)
public static bool TryParse(string? address, out PostalAddress? result)
```

```csharp
var addr = PostalAddress.Parse("臺北市信義區市府路1之2號3樓");
// addr.City: "臺北市", addr.Road: "市府路", addr.Number: 1
```

#### Validate

```csharp
public static PostalAddressValidation Validate(PostalAddress address)
```

驗證地址組件有效性（縣市、行政區、路街是否存在於資料庫中）。

```csharp
var addr = PostalAddress.Parse("臺北市信義區市府路1號");
var v = PostalAddress.Validate(addr);
// v.IsValidCity: true
// v.IsValidDistrict: true
// v.IsValidRoad: true
// v.IsValid: true
```

### 實例方法

```csharp
public string GetFullNumber()     // "1之2號"
public string GetBaseAddress()    // "臺北市信義區市府路"
public override string ToString()
```

---

## PostalAddressValidation

```csharp
public record PostalAddressValidation
```

| 屬性 | 型別 | 說明 |
|------|------|------|
| `IsValidCity` | `bool` | 縣市有效 |
| `IsValidDistrict` | `bool` | 行政區有效 |
| `IsValidRoad` | `bool` | 路街有效 |
| `IsValidLocality` | `bool` | 地區名有效 |
| `IsValidLane` | `bool` | 巷有效 |
| `IsValidAlley` | `bool` | 弄有效 |
| `IsValidNumber` | `bool` | 門牌有效 |
| `MatchedRule` | `PostalRule?` | 匹配的規則 |
| `Messages` | `List<string>` | 訊息 |
| `IsValid` | `bool` | 整體是否有效 |

---

## ZipCodeResult 類別

```csharp
public record ZipCodeResult
```

### 屬性

| 屬性 | 型別 | 說明 |
|------|------|------|
| `ResultType` | `ZipCodeResultType` | ExactMatch / PartialMatch / NotFound |
| `ZipCode` | `string` | 郵遞區號 |
| `ZipCode3` | `string` | 3 碼郵遞區號 |
| `ZipCode5` | `string?` | 5+ 碼郵遞區號 |
| `OriginalAddress` | `string` | 原始地址 |
| `NormalizedAddress` | `string` | 正規化地址 |
| `Address` | `PostalAddress?` | 解析的地址組件 |
| `MatchedRule` | `PostalDeliveryRule?` | 匹配的投遞規則 |
| `MatchedScope` | `string?` | 匹配的範圍 |
| `Department` | `string?` | 投遞局 |
| `Office` | `string?` | 投遞處 |
| `IsValid` | `bool` | 是否找到結果 |
| `IsExactMatch` | `bool` | 是否完整匹配 |
| `Messages` | `List<string>` | 額外訊息 |
| `Suggestions` | `List<string>` | 候選地址 |

---

## PostalValidationResult 類別

```csharp
public record PostalValidationResult
```

| 屬性 | 型別 | 說明 |
|------|------|------|
| `IsValid` | `bool` | 驗證通過 |
| `ZipCode` | `string` | 郵遞區號 |
| `NormalizedAddress` | `string` | 正規化地址 |
| `Messages` | `List<string>` | 驗證訊息 |
| `FailureReason` | `PostalValidationFailureReason` | 失敗原因 |
| `Suggestions` | `List<string>` | 建議地址 |

### PostalValidationFailureReason 列舉

| 值 | 說明 |
|----|------|
| `None` | 無（通過） |
| `InvalidFormat` | 格式無效 |
| `AddressNotFound` | 找不到地址 |
| `NumberOutOfRange` | 門牌超出範圍 |
| `NumberRuleMismatch` | 不符合單雙號規則 |
| `DistrictNotFound` | 區域不存在 |
| `StreetNotFound` | 街道不存在 |

---

## PostalDeliveryRule 類別

投遞規則。

```csharp
public class PostalDeliveryRule
```

### 屬性

| 屬性 | 型別 | 說明 |
|------|------|------|
| `Type` | `PostalRuleType` | 規則類型 |
| `StartNumber` | `int?` | 起始號碼 |
| `EndNumber` | `int?` | 結束號碼 |
| `SpecificNumber` | `int?` | 指定號碼 |
| `SpecificSubNumber` | `int?` | 指定附號 |
| `RawRule` | `string` | 原始規則字串 |

### 方法

```csharp
public static PostalDeliveryRule Parse(string fullRuleString)
public bool Matches(PostalAddress components)
public string GetDescription()
```

```csharp
var rule = PostalDeliveryRule.Parse("臺北市中正區三元街單147號以下");
Console.WriteLine(rule.Type);             // LessOrEqual
Console.WriteLine(rule.GetDescription()); // "單號，147號以下"

var addr = PostalAddress.Parse("臺北市中正區三元街145號");
Console.WriteLine(rule.Matches(addr));    // true
```

### PostalRuleType 列舉

| 值 | 說明 |
|----|------|
| `All` | 全部 |
| `Odd` | 單號 |
| `Even` | 雙號 |
| `Specific` | 指定號碼 |
| `Range` | 範圍 |
| `GreaterOrEqual` | 以上 |
| `LessOrEqual` | 以下 |
| `WithSubNumber` | 含附號 |
| `SubNumberOnly` | 僅附號 |
| `SubNumberAbove` | 附號以上 |
| `SubNumberBelow` | 附號以下 |

---

## PostalAddressSuggestion 類別

```csharp
public record PostalAddressSuggestion(string AddressText, string ZipCode, PostalAddress? Address)
```

---

## PostalRulesEngine 類別

規則引擎（資料已編譯進 binary，執行期零讀檔）。

```csharp
public static class PostalRulesEngine
```

### 靜態屬性

| 屬性 | 型別 | 說明 |
|------|------|------|
| `CurrentVersion` | `PostalDatabaseVersionInfo` | 目前資料版本 |

### PostalDatabaseVersionInfo

```csharp
public record PostalDatabaseVersionInfo
```

| 屬性 | 型別 | 說明 |
|------|------|------|
| `Version` | `string` | 資料生成日期（yyyy-MM-dd） |
| `RecordCount` | `int` | 規則筆數 |
| `BuilderVersion` | `string` | 建置工具版本 |

---

## 執行緒安全性

- `ZipCode`、`PostalRulesEngine` — 靜態唯讀資料，所有方法執行緒安全
- `PostalAddress`、`PostalDeliveryRule` — 不可變物件，可安全共享
- `ZipCodeResult`、`PostalValidationResult` — record 型別，不可變

---

## 其他資源

- [README.md](../README.md) — 專案概述
- [快速入門](QUICKSTART.md) — 5 分鐘上手
- [內嵌資源](EMBEDDED_RESOURCE.md) — 資料庫嵌入技術
- [地址生成器](POSTAL_ADDRESS_GENERATOR_API.md) — 測試地址生成
