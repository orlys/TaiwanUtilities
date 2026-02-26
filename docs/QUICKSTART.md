# 快速入門指南

## 安裝

```bash
dotnet add package TaiwanUtilities
```

支援 .NET 10 / .NET 8 / .NET Standard 2.0 / .NET Framework 4.7.2

---

## 郵遞區號查詢

```csharp
using TaiwanUtilities;

// 查詢郵遞區號
ZipCodeResult result = ZipCode.Find("臺北市信義區市府路1號");
Console.WriteLine(result.ZipCode);      // 110204
Console.WriteLine(result.ResultType);   // ExactMatch

// 漸進式查詢（地址不完整也能查）
ZipCodeResult r1 = ZipCode.Find("臺北市");           // ZipCode: "1"
ZipCodeResult r2 = ZipCode.Find("臺北市信義區");      // ZipCode: "110"
ZipCodeResult r3 = ZipCode.Find("臺北市信義區市府路"); // ZipCode: "110204"
```

## 地址解析

```csharp
PostalAddress addr = PostalAddress.Parse("臺北市信義區市府路1之2號3樓");
Console.WriteLine(addr.City);       // 臺北市
Console.WriteLine(addr.District);   // 信義區
Console.WriteLine(addr.Road);       // 市府路
Console.WriteLine(addr.Number);     // 1
Console.WriteLine(addr.Floor);      // 3
Console.WriteLine(addr.GetFullNumber());  // 1之2號
```

## 地址正規化

```csharp
// 自動處理繁簡、全半形、標點
string n1 = PostalAddressUtils.Normalize("台北市，信義區，市府路１號");
// "臺北市信義區市府路1號"

string n2 = PostalAddressUtils.Normalize("信義路一段");
// "信義路1段"
```

## 地址驗證

```csharp
PostalValidationResult v = ZipCode.ValidateAddress("臺北市信義區市府路1號");
Console.WriteLine(v.IsValid);   // true
Console.WriteLine(v.ZipCode);   // 110204

PostalValidationResult v2 = ZipCode.ValidateAddress("臺北市信義區市府路99999號");
Console.WriteLine(v2.IsValid);        // false
Console.WriteLine(v2.FailureReason);  // NumberOutOfRange
```

---

## 中文數字

```csharp
using TaiwanUtilities;

// 解析（中文 → decimal）
ChineseNumeric cn = ChineseNumeric.Parse("貳千參佰陸拾玖");
decimal value = cn;  // 2369

// 格式化（decimal → 中文）
ChineseNumeric num = 2369m;
Console.WriteLine(num.ToString("TW"));  // 貳仟參佰陸拾玖
Console.WriteLine(num.ToString("tw"));  // 二千三百六十九
Console.WriteLine(num.ToString("FW"));  // ２３６９
```

---

## 民國日期

```csharp
using TaiwanUtilities;

// 隱含轉換
RocDateTime roc = new DateTime(2025, 10, 24);  // 114年10月24日

// 格式化
Console.WriteLine(roc.ToString("d"));   // 114/10/24
Console.WriteLine(roc.ToString("D"));   // 114年10月24日
Console.WriteLine(roc.ToString("年月日"));  // 一百一十四年十月二十四日

// 國定假日
RocHoliday holiday = roc.Holiday;
Console.WriteLine(holiday.IsHoliday);    // true
Console.WriteLine(holiday.Description);  // 光復節補假

// 從遠端更新假日資料（自動判斷當前年與下一年）
await RocHolidayDataSet.UpdateAsync();

// 或從本地 CSV 檔案更新
await RocHolidayDataSet.UpdateFromAsync("holidays.csv");
```

---

## 證號驗證

```csharp
using TaiwanUtilities;

NationalIdentificationCardNumber.Validate("Y190290172");          // 身分證
BusinessAdministrationNumber.Validate("12345675");                // 統編
CitizenDigitalCertificateNumber.Validate("AB12345678901234");     // 自然人憑證
ElectronicInvoiceMobileBarCode.Validate("/ABC1234");              // 手機條碼
ElectronicInvoiceDonateCode.Validate("2134567");                  // 捐贈碼
```

---

## 中文髒話過濾

```csharp
using TaiwanUtilities;

// 偵測
ChineseProfanity.Censor("幹你娘都是說說的而已");  // true

// 取代
ChineseProfanity.Replace("幹你娘都是說說的而已", '*');
// "***都是說說的而已"

// 不誤判
ChineseProfanity.Censor("程式寫這樣乾脆別寫了");  // false
```

---

## 更多資訊

- [Postal API 文件](API.md)
- [內嵌資源說明](EMBEDDED_RESOURCE.md)
- [授權說明](LICENSING.md)
