# TaiwanUtilities
[![.NET](https://github.com/Orlys/TaiwanUtilities/actions/workflows/ci.yml/badge.svg)](https://github.com/Orlys/TaiwanUtilities/actions/workflows/ci.yml) ![NuGet Version](https://img.shields.io/nuget/v/TaiwanUtilities)

> 跟台灣相關的工具庫 

## 類型一覽

### `ChineseDecimal` 型別
- 提供中文大小寫數字與 ```decimal``` 間的隱含互換，但解析部分不支援小數點
- 支援 `Newtonsoft.Json` 與 `System.Text.Json` 的序列化與反序列化，無須預先註冊轉換器
- 可補足 ```InternationalNumericFormatter.dll``` 中無法處理的部分
```csharp
using TaiwanUtilities;

var expect = 2369;
var actual = ChineseDecimal.Parse("貳佰參陸九");
// result: pass
Assert.Equal<decimal>(expect, actual);
```

### `RocDateTime` 型別

- 支援中文日期時間字串解析
- 可在 `DateTime` 與 `DateTimeOffset` 結構之間隱含轉換
- 支援 `Newtonsoft.Json` 與 `System.Text.Json` 的序列化與反序列化，無須預先註冊轉換器
- 民國年與西元年間的轉換，另外可處理民國前的時間

```csharp
using TaiwanUtilities;

var expect = new DateTime(1934, 6, 9);
var actual = RocDateTime.Parse("民國貳參年陸月玖日");
// result: pass
Assert.Equal<DateTime>(expect, actual);
```
