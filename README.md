# TaiwanUtilities
[![.NET](https://github.com/Orlys/TaiwanUtilities/actions/workflows/ci.yml/badge.svg)](https://github.com/Orlys/TaiwanUtilities/actions/workflows/ci.yml) [![NuGet Version](https://img.shields.io/nuget/v/TaiwanUtilities)](https://www.nuget.org/packages/TaiwanUtilities)


> 跟台灣相關的工具庫 

## 類型一覽
### 純值型別
純值類型均有以下特性
1. 提供隱含轉換至原生相似、對應類型
2. 無須預先註冊轉換器，原生支援 `Newtonsoft.Json` 與 `System.Text.Json` 的序列化與反序列化
3. 原生支援 `JavaScriptSerializer` 的直接序列化與反序列化

#### `ChineseDecimal` 結構
- 提供中文大小寫數字與 ```decimal``` 間的隱含互換，但解析部分不支援小數點
- 可補足 [```InternationalNumericFormatter.dll```](https://www.microsoft.com/zh-tw/download/details.aspx?id=18970) 中無法處理的部分
```csharp
using TaiwanUtilities;

var expect = 2369;
var actual = ChineseDecimal.Parse("貳佰參陸九");
// result: pass
Assert.Equal<decimal>(expect, actual);
```

#### `RocDateTime` 結構
- 支援中文日期時間字串解析
- 可在 `DateTime` 與 `DateTimeOffset` 結構之間隱含轉換
- 民國年與西元年間的轉換，另外可處理民國前的時間

```csharp
using TaiwanUtilities;

var expect = new DateTime(1934, 6, 9);
var actual = RocDateTime.Parse("民國貳參年陸月玖日");
// result: pass
Assert.Equal<DateTime>(expect, actual);
```

#### `PostalAddress` 類別 (實驗性)
- 此功能開發中，不建議在生產環境使用
- 台灣地址解析
- 支援區域的各種組合，包含市、縣、區、鎮、鄉、里、村、鄰等
- 支援地址的各種格式，包含街道、巷弄、路線等
- 支援地址的門牌號碼、樓層、單元等
- 詳細請參考 [docs/experimental.md](https://github.com/orlys/TaiwanUtilities/blob/master/docs/experimental.md#postaladdress)
```csharp
using TaiwanUtilities;
var expect = "信義路二段";
var actual = PostalAddress.Parse("臺北市中正區信義路二段100號").Road;
// result: pass
Assert.Equal<string>(expect, actual);
```

### 靜態型別
#### `TaiwanIdValidator` 類別
- 中華民國身分證字號驗證驗證
- 新/舊版臺灣地區無戶籍國民、外國人、大陸地區人民及香港或澳門居民之專屬代號驗證
- 營利事業統一編號驗證 (支援新/舊版統一編號檢查)
- 自然人憑證編號驗證
- 電子發票手機條碼驗證
- 電子發票捐贈碼驗證
```csharp
using TaiwanUtilities;

// result: pass
Assert.True(TaiwanIdValidator.IsIdentityCardNumber("Y190290172"));
```

#### `ZipCode` 類別 (實驗性)
- 此功能開發中，不建議在生產環境使用
- 台灣郵遞區號查詢功能(目前僅支援三碼)
```csharp
using TaiwanUtilities;

Assert.Equal<string>("100", ZipCode.Find("臺北市中正區"));
```



## 感謝
- `TaiwanIdValidator` 原始版本為 [enylin/taiwan-id-validator](https://github.com/enylin/taiwan-id-validator)，該儲存庫採用 [MIT](https://github.com/enylin/taiwan-id-validator/blob/main/LICENSE) 授權條款  
<!--
- `ZipCode` 的點子來自 [recca0120/twzipcode](https://github.com/recca0120/twzipcode) 這個儲存庫，該儲存庫採用 [MIT](https://github.com/recca0120/twzipcode/blob/main/LICENSE) 授權條款  
-->


此儲存庫基於「取之於社群，回饋於社群」的愛與信念而存在，感謝以上原作者為開源社群的貢獻