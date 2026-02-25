# PostalAddressGenerator API 文檔

## 概述

`PostalAddressGenerator` 是 TaiwanUtilities 提供的公開 API，用於生成符合台灣郵遞規則的真實地址。此類別從 SQLite 資料庫中讀取投遞規則，並根據這些規則生成有效的地址。

## 命名空間

```csharp
using TaiwanUtilities;
```

## 類別定義

```csharp
public class PostalAddressGenerator
```

---

## 建構子

### PostalAddressGenerator()

創建一個新的地址生成器實例。

```csharp
PostalAddressGenerator generator = new PostalAddressGenerator();
```

---

## 公開方法

### Generate(int count = 10)

生成指定數量的隨機地址。

**參數**:
- `count` (int, 可選): 要生成的地址數量，預設為 10

**返回值**:
- `List<GeneratedPostalAddress>`: 生成的地址清單

**範例**:
```csharp
PostalAddressGenerator generator = new PostalAddressGenerator();
List<GeneratedPostalAddress> addresses = generator.Generate(20);
```

### Generate(AddressGenerationOptions options)

使用指定選項生成地址。

**範例**:
```csharp
List<GeneratedPostalAddress> addresses = generator.Generate(new AddressGenerationOptions
{
    City = "臺北市",
    District = "信義區",
    RequireLane = true,
    EvenOdd = EvenOddRule.OddOnly,
    Count = 10
});
```

---

## Fluent API 方法

### FromCity(string city)

指定要生成地址的縣市。

```csharp
generator.FromCity("臺北市").Generate(10);
```

### FromDistrict(string district)

指定要生成地址的行政區。

```csharp
generator.FromCity("臺北市").FromDistrict("信義區").Generate(10);
```

### FromRoad(string road)

指定要生成地址的路街名。

```csharp
generator
    .FromCity("臺北市")
    .FromDistrict("信義區")
    .FromRoad("市府路")
    .Generate(5);
```

### WithLane() / WithAlley() / WithSubNumber()

要求生成的地址包含巷號/弄號/門牌附號。

```csharp
generator.WithLane().WithAlley().Generate(5);
```

### OddNumbersOnly() / EvenNumbersOnly()

只生成單號/雙號門牌的地址。

```csharp
generator.OddNumbersOnly().Generate(10);
```

### Reset()

重置所有篩選條件和選項。

```csharp
generator.FromCity("臺北市").Generate(5);
generator.Reset();
generator.FromCity("高雄市").Generate(5);
```

---

## 類別：AddressGenerationOptions

地址生成選項類別。

### 屬性

| 屬性 | 類型 | 預設值 | 說明 |
|------|------|--------|------|
| `City` | `string?` | `null` | 縣市名稱 |
| `District` | `string?` | `null` | 行政區名稱 |
| `Road` | `string?` | `null` | 路街名 |
| `RequireLane` | `bool` | `false` | 要求包含巷號 |
| `RequireAlley` | `bool` | `false` | 要求包含弄號 |
| `RequireSubNumber` | `bool` | `false` | 要求包含門牌附號 |
| `EvenOdd` | `EvenOddRule` | `Any` | 單雙號規則 |
| `Count` | `int` | `10` | 要生成的地址數量 |

---

## 類別：GeneratedPostalAddress

生成的郵政地址類別。

### 屬性

| 屬性 | 類型 | 說明 |
|------|------|------|
| `Address` | `PostalAddress` | 解析後的地址物件 |
| `FullAddress` | `string` | 完整地址字串 |
| `ZipCode` | `string` | 郵遞區號 |
| `Rule` | `PostalRule?` | 匹配的投遞規則（如果有） |
| `Source` | `GenerationSource` | 生成來源 |

### 方法

#### Validate()

驗證地址是否正確。

```csharp
foreach (GeneratedPostalAddress addr in addresses)
{
    bool isValid = addr.Validate();
    Console.WriteLine($"{addr.FullAddress}: {(isValid ? "有效" : "無效")}");
}
```

---

## 使用範例

### 基本用法

```csharp
using TaiwanUtilities;

PostalAddressGenerator generator = new PostalAddressGenerator();
List<GeneratedPostalAddress> addresses = generator.Generate(10);

foreach (GeneratedPostalAddress addr in addresses)
{
    Console.WriteLine(addr.FullAddress);
    Console.WriteLine($"  郵遞區號: {addr.ZipCode}");
    Console.WriteLine($"  來源: {addr.Source}");
}
```

### 使用 Fluent API

```csharp
// 生成臺北市信義區的單號地址
List<GeneratedPostalAddress> addresses = new PostalAddressGenerator()
    .FromCity("臺北市")
    .FromDistrict("信義區")
    .OddNumbersOnly()
    .Generate(20);
```

### 複雜篩選

```csharp
// 生成包含巷弄的雙號地址
List<GeneratedPostalAddress> addresses = new PostalAddressGenerator()
    .WithLane()
    .WithAlley()
    .EvenNumbersOnly()
    .Generate(15);

foreach (GeneratedPostalAddress addr in addresses)
{
    Console.WriteLine($"{addr.FullAddress}");
    Console.WriteLine($"  巷: {addr.Address.Lane}");
    Console.WriteLine($"  弄: {addr.Address.Alley}");
    Console.WriteLine($"  門牌: {addr.Address.Number}號 (雙號)");
}
```

### 批次生成不同縣市

```csharp
PostalAddressGenerator generator = new PostalAddressGenerator();
string[] cities = new[] { "臺北市", "新北市", "臺中市", "高雄市" };

foreach (string city in cities)
{
    List<GeneratedPostalAddress> addresses = generator.FromCity(city).Generate(5);
    Console.WriteLine($"\n{city}:");

    foreach (GeneratedPostalAddress addr in addresses)
    {
        Console.WriteLine($"  {addr.FullAddress}");
    }

    generator.Reset();
}
```

### 驗證生成的地址

```csharp
PostalAddressGenerator generator = new PostalAddressGenerator();
List<GeneratedPostalAddress> addresses = generator.Generate(50);

int validCount = 0;
foreach (GeneratedPostalAddress addr in addresses)
{
    if (addr.Validate())
    {
        validCount++;
    }
}

Console.WriteLine($"驗證通過率: {(double)validCount / addresses.Count * 100:F1}%");
```

---

## 相關 API

- [PostalAddress](API.md#postaladdress-類別) - 結構化地址物件
- [ZipCode.Find()](API.md#find) - 郵遞區號查詢
- [DeliveryRule](API.md#deliveryrule-類別) - 投遞規則
