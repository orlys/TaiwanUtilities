# PostalAddressGenerator API

## 概述

`PostalAddressGenerator` 用於生成符合台灣郵遞投遞規則的隨機地址，主要用於測試。
地址從 SQLite 資料庫中的投遞規則生成，確保符合實際的門牌號碼範圍與單雙號規則。

---

## 基本用法

```csharp
using TaiwanUtilities;

var generator = new PostalAddressGenerator();
List<GeneratedPostalAddress> addresses = generator.Generate(10);

foreach (var addr in addresses)
{
    Console.WriteLine($"{addr.FullAddress} ({addr.ZipCode})");
}
```

---

## PostalAddressGenerator

```csharp
public class PostalAddressGenerator
```

### Generate

```csharp
public List<GeneratedPostalAddress> Generate(
    int count,
    Action<int, int>? progressCallback = null)
```

**參數：**
- `count` — 要生成的地址數量
- `progressCallback` — 進度回調 `(已生成數, 總數)`，可選

**生成策略：**
1. 優先從 `postal_rules` 表隨機選取規則生成
2. 不足時從漸進式索引表補充

---

## GeneratedPostalAddress

```csharp
public class GeneratedPostalAddress
```

### 屬性

| 名稱 | 型別 | 說明 |
|------|------|------|
| `Address` | `PostalAddress` | 結構化地址物件 |
| `FullAddress` | `string` | 完整地址字串 |
| `ZipCode` | `string` | 郵遞區號 |
| `Rule` | `PostalRule?` | 匹配的投遞規則 |
| `Source` | `PostalGenerationSource` | 生成來源 |

### 方法

#### Validate

```csharp
public bool Validate()
```

驗證生成的地址是否能正確查詢到郵遞區號。

#### GetValidation

```csharp
public PostalAddressValidation GetValidation()
```

取得詳細的地址驗證結果。

---

## PostalGenerationSource 列舉

| 值 | 說明 |
|----|------|
| `PostalRules` | 從投遞規則生成 |
| `PostalDatabase` | 從漸進式索引生成 |

---

## 使用範例

### 批次生成並驗證

```csharp
var generator = new PostalAddressGenerator();
var addresses = generator.Generate(100);

int validCount = addresses.Count(a => a.Validate());
Console.WriteLine($"驗證通過率: {(double)validCount / addresses.Count * 100:F1}%");
```

### 帶進度回調

```csharp
var generator = new PostalAddressGenerator();
var addresses = generator.Generate(10000, (current, total) =>
{
    if (current % 1000 == 0)
        Console.WriteLine($"進度: {current}/{total}");
});
```

---

## 相關 API

- [PostalAddress](API.md#postaladdress-類別)
- [ZipCode.Find()](API.md#find)
- [PostalDeliveryRule](API.md#deliveryrule-類別)
