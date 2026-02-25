# 🇹🇼 TaiwanUtilities

[![CI/CD](https://github.com/Orlys/TaiwanUtilities/actions/workflows/ci.yml/badge.svg)](https://github.com/Orlys/TaiwanUtilities/actions/workflows/ci.yml) [![NuGet Version](https://img.shields.io/nuget/v/TaiwanUtilities)](https://www.nuget.org/packages/TaiwanUtilities) [![Tests](https://img.shields.io/badge/tests-999_passed-brightgreen)](https://github.com/Orlys/TaiwanUtilities/actions/workflows/ci.yml) [![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

台灣專用 .NET 工具庫，涵蓋郵遞區號查詢、中文數字轉換、民國日期、證號驗證與中文髒話過濾。

```bash
dotnet add package TaiwanUtilities
```

### ✨ 核心功能

- 🏠 **郵遞區號查詢** — 內嵌全台 80,000+ 筆投遞規則，支援 3+2 碼精確查詢與地址解析
- 🔢 **中文數字轉換** — 中文大小寫數字與 `decimal` 之間的解析與格式化
- 📅 **民國日期** — 支援中文日期時間解析、國定假日查詢，可隱含轉換為 `DateTime`
- ✅ **證號驗證** — 身分證、統編、自然人憑證、手機條碼、捐贈碼
- 🤬 **中文髒話過濾** — 三層語言學架構，支援閩南語與國語，低誤判率

### 🎯 相容性

| 框架 | 版本 |
|------|------|
| .NET | 10 / 8 |
| .NET Standard | 2.0 |
| .NET Framework | 4.7.2 |

---

## 🏠 郵遞區號 `ZipCode`

內嵌中華郵政全台 80,000+ 筆投遞規則，查詢引擎採用記憶體內預載 + SQLite 雙層架構，首次查詢自動預熱，後續查詢在微秒級完成。

```csharp
using TaiwanUtilities;

// 查詢郵遞區號
var result = ZipCode.Find("台北市信義區市府路1號");
// result.ZipCode: "110204"
// result.ResultType: ExactMatch

// 地址解析
var addr = PostalAddress.Parse("台北市信義區市府路1號5樓之3");
// addr.City: "臺北市"
// addr.District: "信義區"
// addr.Road: "市府路"
// addr.Number: 1
// addr.Floor: "5"
// addr.SubFloor: 3

// 自動完成建議
var suggestions = ZipCode.GetSuggestions("台北市信義區市府", maxResults: 5);

// 地址驗證
var validation = ZipCode.ValidateAddress("台北市信義區市府路1號");
// validation.IsValid: true

// 取得投遞規則
var rules = ZipCode.GetDeliveryRules("台北市信義區市府路");
```

## 🔢 中文數字 `ChineseDecimal`

中文大小寫數字與 `decimal` 之間的解析與格式化，涵蓋繁體/簡體大小寫及全形/半形數字。

```csharp
using TaiwanUtilities;

// 解析中文數字
decimal value = ChineseDecimal.Parse("貳千參陸九"); // 2369

// 格式化為中文大寫
string upper = value.ToString("TW"); // 貳仟參佰陸拾玖

// 格式化為中文小寫
string lower = value.ToString("tw"); // 二千三百六十九
```

## 📅 民國日期 `RocDateTime`

支援中文日期時間解析，可隱含轉換為 `DateTime` / `DateTimeOffset`，支援民國前紀年。
內嵌行政院公告之國定假日資料（民國 87 年至今），支援執行時自動下載最新行事曆。

```csharp
using TaiwanUtilities;

RocDateTime a = new DateTime(2025, 10, 24);  // 114年10月24日
RocDateTime b = new DateTime(1908, 6, 9);    // 民前4年6月9日

// 國定假日查詢
RocHoliday holiday = a.Holiday;
// holiday.IsHoliday: true
// holiday.Description: "光復節補假"

// 格式化
string s1 = a.ToString("年月日 時分秒");  // 一百一十四年十月二十四日 〇時〇分〇秒
string s2 = a.ToString("g");              // 114/10/24 00:00:00

// 手動新增假日
RocHolidayDataSet.Default.Add(
    new DateTime(2025, 12, 25),
    new RocHoliday(true, HolidayRole.All, "聖誕節"));

// 下載最新行事曆
await RocHolidayDataSet.Default.DownloadAsync(115);
```

<details>
<summary>格式字串對照表</summary>

| 格式 | 範例 | 說明 |
|------|------|------|
| `民國年` | `民國一百一十二年` | 中文民國年 |
| `年` | `一百一十二年` | 中文年 |
| `月` | `三月` | 中文月 |
| `日` | `五日` | 中文日 |
| `時` | `十四時` | 中文時 |
| `分` | `三十分` | 中文分 |
| `秒` | `四十五秒` | 中文秒 |
| `yyy` | `112` | 3 位數年份 |
| `MM` | `03` | 2 位數月份 |
| `dd` | `05` | 2 位數日期 |
| `HH` | `14` | 2 位數小時 |
| `mm` | `30` | 2 位數分鐘 |
| `ss` | `45` | 2 位數秒數 |
| `d` | `112/03/05` | 簡短日期 |
| `D` | `112年3月5日` | 完整中文日期 |
| `t` | `14:30:45` | 簡短時間 |
| `T` | `14時30分45秒` | 完整中文時間 |
| `f` | `112/03/05 14:30:45` | 完整日期時間 |
| `F` | `112年3月5日14時30分45秒` | 完整中文日期時間 |
| `民國日期` | `民國一一二年三月五日` | 中文民國日期 |

</details>

## ✅ 格式驗證

支援多種台灣常用證號的格式驗證。

```csharp
using TaiwanUtilities;

// 身分證字號
NationalIdentificationCardNumber.Validate("Y190290172"); // true

// 營利事業統一編號
BusinessAdministrationNumber.Validate("12345675"); // true

// 自然人憑證號碼
CitizenDigitalCertificateNumber.Validate("AB12345678901234"); // true

// 電子發票手機條碼
ElectronicInvoiceMobileBarCode.Validate("2134567"); // true

// 電子發票捐贈碼
ElectronicInvoiceDonateCode.Validate("2134567"); // true
```

## 🤬 中文髒話 `ChineseProfanity`

台味滿滿的中文髒話過濾器，基於三層語言學架構（罵人、性器官、排泄物）實作。
支援閩南語、國語及常見變體，低誤判率。

```csharp
using TaiwanUtilities;

// 偵測
ChineseProfanity.Censor("幹你娘都是說說的而已"); // true

// 取代
ChineseProfanity.Replace("幹你娘都是說說的而已，屌你老母", '*');
// ***都是說說的而已，****

// 語境理解（不誤判）
ChineseProfanity.Censor("這串葡萄誰寫的？程式寫這樣乾脆別寫了"); // false
```

---

## 📁 專案結構

```
TaiwanUtilities/
├── src/TaiwanUtilities/          # 主要程式庫
│   ├── ChineseDecimal/           # 中文數字模組
│   ├── ChineseProfanity/         # 中文髒話過濾模組
│   ├── Postal/                   # 郵遞區號模組
│   ├── RocDateTime/              # 民國日期模組
│   └── Validators/               # 證號驗證模組
├── test/TaiwanUtilities.UnitTests/
├── tools/Postal.Builder/         # 郵遞區號資料庫建立工具
└── docs/                         # 技術文件
```

## 🛠️ 開發

```bash
# 建置
dotnet build src/TaiwanUtilities/

# 測試（951 個測試案例）
dotnet test test/TaiwanUtilities.UnitTests/

# 發布新版本
git tag v1.2.0 && git push origin v1.2.0
```

## 📄 授權

[MIT License](LICENSE)

## 🙏 感謝

- 郵遞區號資料來源：[中華郵政](https://www.post.gov.tw/)，採用[政府資料開放授權條款](https://data.gov.tw/license)
- 國定假日資料來源：[行政院](https://data.gov.tw/dataset/14718)，採用政府資料開放授權條款
- 身分證驗證原始版本：[enylin/taiwan-id-validator](https://github.com/enylin/taiwan-id-validator)（MIT 授權）

此儲存庫基於「取之於社群，回饋於社群」的愛與信念而存在 ❤️
