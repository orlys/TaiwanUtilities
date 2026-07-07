# 🇹🇼 TaiwanUtilities

[![CI/CD](https://github.com/Orlys/TaiwanUtilities/actions/workflows/ci.yml/badge.svg)](https://github.com/Orlys/TaiwanUtilities/actions/workflows/ci.yml) [![NuGet Version](https://img.shields.io/nuget/v/TaiwanUtilities)](https://www.nuget.org/packages/TaiwanUtilities) [![Tests](https://img.shields.io/badge/tests-1012_passed-brightgreen)](https://github.com/Orlys/TaiwanUtilities/actions/workflows/ci.yml) [![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

台灣專用 .NET 工具庫，涵蓋郵遞區號查詢、中文數字轉換、民國日期、證號驗證與中文髒話過濾。

```bash
dotnet add package TaiwanUtilities
```

### ✨ 核心功能

- 🏠 **郵遞區號查詢** — 內嵌全台近 8 萬筆投遞規則，資料編譯進組件、執行期零讀檔，支援 3+3 碼精確查詢與地址解析
- 🔢 **中文數字轉換** — 中文大小寫數字、貨幣與 `decimal` 之間的解析與格式化
- 📅 **民國日期** — 支援中文日期時間解析、國定假日查詢，可隱含轉換為 `DateTime`
- ✅ **證號驗證** — 身分證、統編、自然人憑證、手機條碼、捐贈碼
- 🤬 **中文髒話過濾** — 語言學構式分析，支援閩南語與國語、規避變體，低誤判率

### 🎯 相容性

| 框架 | 版本 |
|------|------|
| .NET | 10 / 8 |
| .NET Standard | 2.0 |
| .NET Framework | 4.7.2 |

.NET 10 / 8 無執行期第三方相依；舊框架會帶入相容套件（Polyfill、System.Text.Json 等，net472 另含 Lib.Harmony）以支援相同 API。

---

## 🏠 郵遞區號 `ZipCode`

內嵌中華郵政全台近 8 萬筆投遞規則（`PostalRulesEngine.CurrentVersion.RecordCount` 為當期精確筆數）。資料由建置工具展開為靜態 C# 陣列（Struct-of-Arrays）編譯進組件，查詢採三層二分搜尋（縣市 → 行政區 → 路名），**執行期不讀檔、不需預熱**；查詢核心不串接索引鍵、不做雜湊配置。

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
// addr.Floor: "5樓"
// addr.SubFloor: 3

// 自動完成建議
var suggestions = ZipCode.GetSuggestions("台北市信義區市府", maxResults: 5);

// 地址驗證
var validation = ZipCode.ValidateAddress("台北市信義區市府路1號");
// validation.IsValid: true

// 取得投遞規則（含結構化巷弄資訊）
var rules = ZipCode.GetDeliveryRules("台北市信義區市府路");
```

> 郵遞資料由 CI 每季（配合中華郵政更新時程）自動重新下載、生成並提交，版本資訊可由 `PostalRulesEngine.CurrentVersion` 取得。

## 🔢 中文數字 `ChineseNumeric` / `ChineseCurrency`

中文大小寫數字與 `decimal` 之間的解析與格式化，涵蓋繁體/簡體大小寫及全形/半形數字；貨幣以獨立型別 `ChineseCurrency` 處理，遵循中央銀行支票規範。

```csharp
using TaiwanUtilities;

// 解析中文數字
ChineseNumeric value = ChineseNumeric.Parse("貳千參陸九"); // 2369

// 格式化為中文大寫 / 小寫
string upper = value.ToString("TW"); // 貳仟參佰陸拾玖
string lower = value.ToString("tw"); // 二千三百六十九

// 貨幣：與 decimal 雙向轉換
ChineseCurrency price = (ChineseCurrency)123.45m;
string twd = price.ToString("twd"); // 壹佰貳拾參元肆角伍分
```

## 📅 民國日期 `RocDateTime`

支援中文日期時間解析，可隱含轉換為 `DateTime` / `DateTimeOffset`，支援民國前紀年。
內嵌行政院公告之國定假日資料（民國 87 年至今），支援執行時自動更新最新行事曆。

```csharp
using TaiwanUtilities;

RocDateTime a = new DateTime(2025, 10, 24);  // 114年10月24日
RocDateTime b = new DateTime(1908, 6, 9);    // 民前4年6月9日

// 國定假日查詢
RocHoliday holiday = a.Holiday;
// holiday.IsHoliday: true
// holiday.Description: "補假"

// 格式化
string s1 = a.ToString("D");  // 114年10月24日
string s2 = a.ToString("g");  // 114/10/24 00:00:00

// 手動新增假日
RocHolidayDataSet.Add(
    new RocDateTime(114, 12, 25),
    new RocHoliday(true, HolidayRole.All, "聖誕節"));
```

#### 假日資料自動更新

`RocHolidayDataSet` 採用三層資料查詢機制：手動增刪 > 執行時更新 > 編譯時嵌入。
支援從遠端（GitHub Release / data.gov.tw）或本地 CSV 檔案更新。

```csharp
// 從遠端更新（自動判斷當前年與下一年）
await RocHolidayDataSet.UpdateAsync();

// 從本地 CSV 檔案更新
await RocHolidayDataSet.UpdateFromAsync("holidays.csv");

// 從串流更新
await RocHolidayDataSet.UpdateFromStreamAsync(stream);

// 重置回嵌入資料
RocHolidayDataSet.Reload();
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

台味滿滿的中文髒話過濾器，採**語言學構式分析**而非黑名單比對：先將輸入正規化（簡繁、全半形、規避字元），再以字典樹逐字掃描分類詞性（動詞、代詞、親屬、身體、貶義前綴、貶義名詞等），最後由構式規則（如 `動詞+代詞+親屬`）判定是否成罵。同一個字在不同語境下語義不同（`操場` 安全、`操你` 是罵），因此以構式而非單字判定，兼顧偵測率與低誤判。

支援國語、閩南語（賽/駛/拎/恁 等）及常見規避變體（簡體代換、字元穿插如 `幹.你.娘`、全形字母）。

```csharp
using TaiwanUtilities;

// 偵測
ChineseProfanity.Censor("幹你娘都是說說的而已"); // true

// 規避變體同樣攔截（簡體、穿插分隔符）
ChineseProfanity.Censor("干你娘");    // true（干→幹 正規化）
ChineseProfanity.Censor("幹.你.娘"); // true（跳過分隔符）

// 取代（遮罩涵蓋分隔符，其餘文字保留）
ChineseProfanity.Replace("幹你娘都是說說的而已", '*');
// ***都是說說的而已

// 語境理解（不誤判）
ChineseProfanity.Censor("今天在操場跑步三圈"); // false
ChineseProfanity.Censor("程式寫這樣乾脆別寫了"); // false
```

---

## 📁 專案結構

```
TaiwanUtilities/
├── src/TaiwanUtilities/          # 主要程式庫
│   ├── ChineseDecimal/           # 中文數字 / 貨幣模組
│   ├── ChineseProfanity/         # 中文髒話過濾模組
│   ├── Postal/                   # 郵遞區號模組（含生成的 PostalData.g.cs）
│   ├── RocDateTime/              # 民國日期模組
│   └── Validators/               # 證號驗證模組
├── test/TaiwanUtilities.UnitTests/
├── tools/postal/                 # 郵遞區號資料工具
│   └── Postal.Builder/           # DBF → 靜態 C# 生成工具
└── docs/                         # 技術文件
```

## 🛠️ 開發

```bash
# 建置
dotnet build src/TaiwanUtilities/

# 測試
dotnet test test/TaiwanUtilities.UnitTests/

# 發布新版本（推送 v* 標籤，CI 自動打包並發佈至 NuGet）
git tag v1.6.0 && git push origin v1.6.0
```

版本號由 [MinVer](https://github.com/adamralph/minver) 依 git 標籤決定，不需手動維護 `PackageVersion`。

## 📄 授權

[MIT License](LICENSE)

## 🙏 感謝

- 郵遞區號資料來源：[中華郵政](https://www.post.gov.tw/)，採用[政府資料開放授權條款](https://data.gov.tw/license)
- 國定假日資料來源：[行政院](https://data.gov.tw/dataset/14718)，採用政府資料開放授權條款
- 身分證驗證原始版本：[enylin/taiwan-id-validator](https://github.com/enylin/taiwan-id-validator)（MIT 授權）

此儲存庫基於「取之於社群，回饋於社群」的愛與信念而存在 ❤️
