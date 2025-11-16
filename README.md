# TaiwanUtilities
[![.NET](https://github.com/Orlys/TaiwanUtilities/actions/workflows/ci.yml/badge.svg)](https://github.com/Orlys/TaiwanUtilities/actions/workflows/ci.yml) [![NuGet Version](https://img.shields.io/nuget/v/TaiwanUtilities)](https://www.nuget.org/packages/TaiwanUtilities)


> 跟台灣相關的工具庫，支援中文數字、民國日期、身分證字號驗證等功能

## 中文數字 `ChineseDecimal`
> 支援大寫、小寫中文數字轉換成 ```decimal``` 間的隱含互換，但不支援小數點。
> 可補足 [```InternationalNumericFormatter```](https://www.microsoft.com/zh-tw/download/details.aspx?id=18970) 中無法處理的部分，
> 且輸出可選中文大小寫
```csharp
using TaiwanUtilities;

// value: 2369
decimal value = ChineseDecimal.Parse("貳千參陸九");
// chineseUpper: 貳仟參佰陸拾玖
string chineseUpper = value.ToString("TW");
// chineseLower: 二千三百六十九
string chineseLower = value.ToString("tw");
```

## 民國日期時間 `RocDateTime`
> 支援中文日期時間字串解析，且可隱含轉換為 `DateTime` 與 `DateTimeOffset`，並支援民國前的時間。
> 支援判斷是否為台灣國定假日(使用行政院公告之行事曆)，在不更新此套件的情況下支援自動下載最新的行事曆資料

| **中文數字格式** | 輸出範例 | 說明 |
|---------|---------|------|
| `民國年` | `民國一百一十二年` 或 `民國前一年` | 中文數字民國年 |
| `年` | `一百一十二年` 或 `民前一年` | 中文數字年 |
| `月` | `三月` | 中文數字月 |
| `日` | `五日` | 中文數字日 |
| `時` | `十四時` | 中文數字時 |
| `分` | `三十分` | 中文數字分 |
| `秒` | `四十五秒` | 中文數字秒 |

| **阿拉伯數字格式** | 輸出範例 | 說明 |
|---------|---------|------|
| `yyy`, `year` | `112` 或 `^001` | 3 位數年份 |
| `MM`, `month` | `03` | 2 位數月份 |
| `dd`, `day` | `05` | 2 位數日期 |
| `HH`, `hour` | `14` | 2 位數小時(24小時制) |
| `mm`, `min`, `minute` | `30` | 2 位數分鐘 |
| `ss`, `sec`, `second` | `45` | 2 位數秒數 |

| **組合格式** | 輸出範例 | 說明 |
|---------|---------|------|
| `m` | `112/03` | 年/月格式 |
| `M` | `112年03月` | 年月中文格式 |
| `d`, `date` | `112/03/05` | 簡短日期 |
| `D`, `DATE` | `112年3月5日` | 完整中文日期 |
| `t`, `time` | `14:30:45` | 簡短時間 |
| `T`, `TIME` | `14時30分45秒` | 完整中文時間 |
| `f`, `full` | `112/03/05 14:30:45` | 完整日期時間(簡短) |
| `F`, `FULL` | `112年3月5日14時30分45秒` | 完整日期時間(中文) |
| `g` | `112/03/05 14:30:45` | 通用格式(簡短) |
| `G` | `112年3月5日14時30分45秒` | 通用格式(完整) |
| `民國日期` | `民國一一二年三月五日` | 中文民國日期 |
| `日期` | `一一二年三月五日` | 中文日期 |
| `時間` | `十四時三十分四十五秒` | 中文時間 |

```csharp
using TaiwanUtilities;

// a: 114年10月24日
RocDateTime a = new DateTime(2025, 10, 24);
// b: 民前4年6月9日
RocDateTime b = new DateTime(1908, 6, 9);
// isHoliday: true, 原因: 光復節補假
bool isHoliday = a.IsHoliday;

// s: 一百一十四年十月二十四日 〇時〇分〇秒
string s = a.ToString("年月日 時分秒");

// s: 114/10/24 14:30:45
string s = a.ToString("g");
```

## 格式驗證
> 支援多種台灣常用編號的格式驗證，如身分證字號、統一編號等
```csharp
using TaiwanUtilities;

// 驗證營利事業統一編號(統編)
Assert.True(BusinessAdministrationNumber.Validate("12345675"));

// 驗證自然人憑證號碼
Assert.True(CitizenDigitalCertificateNumber.Validate("AB12345678901234"));

// 驗證電子發票捐贈碼
Assert.True(ElectronicInvoiceDonateCode.Validate("2134567"));

// 驗證電子發票手機條碼
Assert.True(ElectronicInvoiceMobileBarCode.Validate("2134567"));

// 驗證身分證字號
Assert.True(NationalIdentificationCardNumber.Validate("Y190290172"));
```

## 中文髒話 `ChineseProfanity`
> 台味滿滿的髒話、不雅字彙過濾器。沒在管你嚴重性的，覺得髒就是髒。

```csharp
using TaiwanUtilities;

// 審查髒話
// isProfane: true
Assert.True(ChineseProfanity.Censor("幹你娘都是說說的而已"));

// 取代髒話
// cleanText: ***都是說說的而已，****
string cleanText = ChineseProfanity.Replace("幹你娘都是說說的而已，屌你老母", '*');

// 理解髒話
// isProfane: false
Assert.False(ChineseProfanity.Censor("這串葡萄誰寫的？程式寫這樣乾脆別寫了"));
```

## (實驗性) 郵務地址 `PostalAddress`
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


## (實驗性) 郵政代碼 `ZipCode`
- 此功能開發中，不建議在生產環境使用
- 台灣郵遞區號查詢功能(目前僅支援三碼)
```csharp
using TaiwanUtilities;

Assert.Equal<string>("100", ZipCode.Find("臺北市中正區"));
```

## 感謝
- `RocDateTime` 中的 `IsHoliday` 屬性來自 [ruyutai/TaiwanCalendar](https://github.com/ruyut/TaiwanCalendar)
- `TaiwanIdValidator` 原始版本為 [enylin/taiwan-id-validator](https://github.com/enylin/taiwan-id-validator)，該儲存庫採用 [MIT](https://github.com/enylin/taiwan-id-validator/blob/main/LICENSE) 授權條款  
<!--
- `ZipCode` 的點子來自 [recca0120/twzipcode](https://github.com/recca0120/twzipcode) 這個儲存庫，該儲存庫採用 [MIT](https://github.com/recca0120/twzipcode/blob/main/LICENSE) 授權條款  
-->


此儲存庫基於「取之於社群，回饋於社群」的愛與信念而存在，感謝以上原作者為開源社群的貢獻