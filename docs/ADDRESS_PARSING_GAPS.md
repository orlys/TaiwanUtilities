# Address Parsing Gaps

Generated: 2026-07-08 23:35:31 +08:00

## Scope

- Data source: `temp/rall1.dbf` read with Big5 via `DbfDataReader`.
- Library under test: `dotnet build src/TaiwanUtilities/ -c Release`, then `net8.0` assembly.
- Distinct groups: `(CITY, AREA, ROAD)`.
- Parse round-trip address: `CITY + AREA + ROAD + "1號"`; comparison uses parsed `Road + Section` because the public API structures section separately.
- Find round-trip address: first usable DBF rule per group, using DBF lane/alley and a parity-aware house number derived from `NO_BGN/NO_END/EVEN`.

## Totals

| Metric | Count | Rate |
|---|---:|---:|
| DBF rows | 79,876 | - |
| Distinct `(CITY, AREA, ROAD)` groups | 44,658 | 100.00% |
| Parse mis-splits / misses | 6,756 | 15.13% |
| Find misses / wrong zips | 1,551 | 3.47% |
| Chinese/Arabic ordinal variant failures | 241 | 0.54% |

## Parse Failure Classes

### 路名被當成 locality

Count: 5,133

| Example address | Expected road | Parsed city | Parsed district | Parsed road | Parsed section | Parsed locality |
|---|---|---|---|---|---|---|
| 南投縣仁愛鄉卜溪部落1號 | 卜溪部落 | 南投縣 | 仁愛鄉 |  |  | 卜溪部落 |
| 南投縣仁愛鄉德鹿灣部落1號 | 德鹿灣部落 | 南投縣 | 仁愛鄉 |  |  | 德鹿灣部落 |
| 南投縣仁愛鄉新望洋1號 | 新望洋 | 南投縣 | 仁愛鄉 |  |  | 新望洋 |
| 南投縣仁愛鄉莎都部落1號 | 莎都部落 | 南投縣 | 仁愛鄉 |  |  | 莎都部落 |
| 南投縣仁愛鄉華崗1號 | 華崗 | 南投縣 | 仁愛鄉 |  |  | 華崗 |

### 其他 parse 誤切

Count: 918

| Example address | Expected road | Parsed city | Parsed district | Parsed road | Parsed section | Parsed locality |
|---|---|---|---|---|---|---|
| 南投縣信義鄉新鄉路三號農路1號 | 新鄉路三號農路 | 南投縣 | 信義鄉 | 農路 |  |  |
| 南投縣信義鄉新鄉路五號農路1號 | 新鄉路五號農路 | 南投縣 | 信義鄉 | 農路 |  |  |
| 南投縣信義鄉新鄉路六號農路1號 | 新鄉路六號農路 | 南投縣 | 信義鄉 | 農路 |  |  |
| 南投縣信義鄉新鄉路四號農路1號 | 新鄉路四號農路 | 南投縣 | 信義鄉 | 農路 |  |  |
| 南投縣南投市中興路中一巷1號 | 中興路中一巷 | 南投縣 | 南投市 | 中興路中1巷 |  |  |

### 中文序號邊界

Count: 345

| Example address | Expected road | Parsed city | Parsed district | Parsed road | Parsed section | Parsed locality |
|---|---|---|---|---|---|---|
| 嘉義縣大林鎮大埔美園區十一路1號 | 大埔美園區十一路 | 嘉義縣 | 園區 | 11路 |  | 大埔美 |
| 嘉義縣大林鎮大埔美園區十二路1號 | 大埔美園區十二路 | 嘉義縣 | 園區 | 12路 |  | 大埔美 |
| 嘉義縣大林鎮大埔美園區十八路1號 | 大埔美園區十八路 | 嘉義縣 | 園區 | 18路 |  | 大埔美 |
| 嘉義縣大林鎮大埔美園區十六路1號 | 大埔美園區十六路 | 嘉義縣 | 園區 | 16路 |  | 大埔美 |
| 嘉義縣朴子市馬稠後園區十一路1號 | 馬稠後園區十一路 | 嘉義縣 | 馬稠後園區 | 11路 |  |  |

### 罕見道路單位字

Count: 304

| Example address | Expected road | Parsed city | Parsed district | Parsed road | Parsed section | Parsed locality |
|---|---|---|---|---|---|---|
| 南投縣中寮鄉永嘉新村1號 | 永嘉新村 | 南投縣 | 中寮鄉 |  |  |  |
| 南投縣仁愛鄉定遠新村1號 | 定遠新村 | 南投縣 | 仁愛鄉 |  |  |  |
| 南投縣竹山鎮中正新村1號 | 中正新村 | 南投縣 | 竹山鎮 |  |  |  |
| 南投縣竹山鎮吉祥新村1號 | 吉祥新村 | 南投縣 | 竹山鎮 |  |  |  |
| 南投縣竹山鎮回嗂新村1號 | 回嗂新村 | 南投縣 | 竹山鎮 |  |  |  |

### 路名被內部單位字提前截斷

Count: 52

| Example address | Expected road | Parsed city | Parsed district | Parsed road | Parsed section | Parsed locality |
|---|---|---|---|---|---|---|
| 宜蘭縣三星鄉三星路八段２３６巷長埤1號 | 三星路八段２３６巷長埤 | 宜蘭縣 | 三星鄉 | 三星路 | 8段 | 長埤 |
| 宜蘭縣冬山鄉義成路三段台電巷1號 | 義成路三段台電巷 | 宜蘭縣 | 冬山鄉 | 義成路 | 3段 |  |
| 屏東縣屏東市林森路東三段1號 | 林森路東三段 | 屏東縣 | 屏東市 | 林森路 | 3段 |  |
| 屏東縣屏東市林森路東二段1號 | 林森路東二段 | 屏東縣 | 屏東市 | 林森路 | 2段 |  |
| 屏東縣屏東市林森路東五段1號 | 林森路東五段 | 屏東縣 | 屏東市 | 林森路 | 5段 |  |

### 特殊行政地名不符合縣市/區 tokenizer

Count: 4

| Example address | Expected road | Parsed city | Parsed district | Parsed road | Parsed section | Parsed locality |
|---|---|---|---|---|---|---|
| 南海諸南沙南沙1號 | 南沙 | 南海諸 |  |  |  |  |
| 南海諸東沙東沙1號 | 東沙 | 南海諸 |  |  |  |  |
| 宜蘭縣釣魚臺列釣魚臺列嶼1號 | 釣魚臺列嶼 | 宜蘭縣 |  |  |  |  |
| 釣魚臺釣魚臺列釣魚臺列嶼1號 | 釣魚臺列嶼 | 釣魚臺 |  |  |  |  |

## Find Failure Classes

### 資料只有全區/巷弄規則，probe 門牌是推導值

Count: 1,382

| Probe address | DBF scoop | Expected zip sample | Result | Actual zip | Parsed road |
|---|---|---:|---|---:|---|
| 南投縣中寮鄉永嘉新村1號 | 全 | 541012 | NotFound |  |  |
| 南投縣仁愛鄉定遠新村1號 | 全 | 546001 | NotFound |  |  |
| 南投縣信義鄉新鄉路三號農路1號 | 全 | 556004 | NotFound |  | 農路 |
| 南投縣信義鄉新鄉路五號農路1號 | 全 | 556004 | NotFound |  | 農路 |
| 南投縣信義鄉新鄉路六號農路1號 | 全 | 556004 | NotFound |  | 農路 |

### 解析後找不到規則

Count: 128

| Probe address | DBF scoop | Expected zip sample | Result | Actual zip | Parsed road |
|---|---|---:|---|---:|---|
| 嘉義市東區台林街1號 | 單 135號以下 | 600063 | NotFound |  | 臺林街 |
| 嘉義縣朴子市馬稠後園區十六路50號 | 雙  50號至  52號 | 613012 | NotFound |  | 16路 |
| 宜蘭縣三星鄉三星路八段２３６巷長埤50號 | 雙  50號至  52號 | 266002 | NotFound |  | 三星路 |
| 彰化縣芳苑鄉草漢路草一段1號 | 連 402巷以下 | 528008 | NotFound |  | 草漢路 |
| 新北市汐止區新台五路一段1號 | 單  39號以下 | 221006 | NotFound |  | 新臺5路 |

### 罕見道路單位字

Count: 26

| Probe address | DBF scoop | Expected zip sample | Result | Actual zip | Parsed road |
|---|---|---:|---|---:|---|
| 宜蘭縣蘇澳鎮港區1號 | 連  49號以下 | 270011 | NotFound |  |  |
| 彰化縣彰化市介壽新村1號 | 連  23號以下 | 500017 | NotFound |  |  |
| 桃園市中壢區華夏一村1號 | 連  96號以下 | 320018 | NotFound |  |  |
| 桃園市平鎮區居易四區1號 | 1號 | 324013 | NotFound |  |  |
| 桃園市復興區溪口台1號 | 連   6號以下 | 336041 | NotFound |  |  |

### 查到其他群組郵遞區號

Count: 6

| Probe address | DBF scoop | Expected zip sample | Result | Actual zip | Parsed road |
|---|---|---:|---|---:|---|
| 彰化縣彰化市彰南路一段台化一莊1號 | 全 | 500041 | ExactMatch | 500052 | 彰南路 |
| 桃園市中壢區中山東路一段２７６巷中興二村1號 | 全 | 320043 | ExactMatch | 320042 | 中山東路 |
| 桃園市平鎮區中豐路南勢一段1號 | 單全 | 324037 | ExactMatch | 324017 | 中豐路 |
| 桃園市平鎮區民族路雙連三段1號 | 全 | 324011 | ExactMatch | 324010 | 民族路 |
| 桃園市平鎮區民族路雙連二段1號 | 全 | 324011 | ExactMatch | 324007 | 民族路 |

### 巷弄型規則與門牌推導不相容

Count: 5

| Probe address | DBF scoop | Expected zip sample | Result | Actual zip | Parsed road |
|---|---|---:|---|---:|---|
| 新竹縣竹北市光明十一路3號 | 單   3號以上 | 302003 | NotFound |  | 光明11路 |
| 新竹縣竹北市縣政十一街1號 | 單  11號以下 | 302048 | NotFound |  | 縣政11街 |
| 臺中市西屯區西屯路三段宏福一巷2號 | 2號 | 407131 | NotFound |  | 西屯路3段宏福1巷 |
| 臺中市西屯區西屯路二段上石南八巷15號 | 單  15號至  17號 | 407043 | NotFound |  | 西屯路2段上石南8巷 |
| 高雄市楠梓區大學二十六街1號 | 單1305號以下 | 811034 | NotFound |  | 大學26街 |

### 特殊行政地名 exact-text fallback 不能處理門牌

Count: 4

| Probe address | DBF scoop | Expected zip sample | Result | Actual zip | Parsed road |
|---|---|---:|---|---:|---|
| 南海諸南沙南沙1號 | 全 | 819001 | NotFound |  |  |
| 南海諸東沙東沙1號 | 全 | 817001 | NotFound |  |  |
| 宜蘭縣釣魚臺列釣魚臺列嶼1號 | 全 | 290001 | NotFound |  |  |
| 釣魚臺釣魚臺列釣魚臺列嶼1號 | 全 | 290001 | NotFound |  |  |

## Ordinal Variant Failures

### 段號中文/阿拉伯變體

Count: 123

| Variant address | Expected road | Parsed road | Parsed section |
|---|---|---|---|
| 宜蘭縣三星鄉三星路8段２３６巷長埤1號 | 三星路八段２３６巷長埤 | 三星路 | 8段 |
| 宜蘭縣三星鄉三星路8段長埤1號 | 三星路八段長埤 |  |  |
| 宜蘭縣五結鄉中正路1段篤行三村1號 | 中正路一段篤行三村 |  |  |
| 宜蘭縣冬山鄉義成路3段台電巷1號 | 義成路三段台電巷 | 義成路 | 3段 |
| 宜蘭縣宜蘭市中山路3段中央商場1號 | 中山路三段中央商場 |  |  |

### 路名序號阿拉伯變體

Count: 118

| Variant address | Expected road | Parsed road | Parsed section |
|---|---|---|---|
| 嘉義縣大林鎮大埔美園區20路1號 | 大埔美園區二十路 | 20路 |  |
| 嘉義縣大林鎮大埔美園區11路1號 | 大埔美園區十一路 | 11路 |  |
| 嘉義縣大林鎮大埔美園區12路1號 | 大埔美園區十二路 | 12路 |  |
| 嘉義縣大林鎮大埔美園區18路1號 | 大埔美園區十八路 | 18路 |  |
| 嘉義縣大林鎮大埔美園區16路1號 | 大埔美園區十六路 | 16路 |  |

## Classification Assessment

| Area | Class | Count | Assessment |
|---|---|---:|---|
| Parse | 路名被當成 locality | 5,133 | 多數是模型 gap：DBF ROAD 可是部落/地名，API 已放入 Locality；若 Find 要支援，需把 locality-like ROAD 納入 lookup key |
| Parse | 其他 parse 誤切 | 918 | 混合：需要逐類拆分，代表例多為農路、商場、眷村等 postal ROAD 延伸 key |
| Parse | 中文序號邊界 | 345 | 可修：行政區/園區與道路序號的 longest-match 邊界 |
| Parse | 罕見道路單位字 | 304 | 可修：郵政資料已列為 ROAD，tokenizer/lookup 應接受新村、台、區等非標準道路 key |
| Parse | 路名被內部單位字提前截斷 | 52 | 可修：用資料驅動最長路名壓過 generic 單位字切割 |
| Parse | 特殊行政地名不符合縣市/區 tokenizer | 4 | 資料/模型 gap：不是一般縣市/區/路地址 |
| Find | 資料只有全區/巷弄規則，probe 門牌是推導值 | 1,382 | 資料/模型 gap 為主：DBF 沒有可驗證門牌範圍，synthetic 1號 只能定位不支援族群 |
| Find | 解析後找不到規則 | 128 | 可修為主：正字/異體字、序號、延伸 key 導致 lookup key 對不上 |
| Find | 罕見道路單位字 | 26 | 可修：postal ROAD key 存在但 parser 沒有產生可查詢 Road |
| Find | 查到其他群組郵遞區號 | 6 | 高優先可修：誤切後落到較寬規則，會回傳錯 zip |
| Find | 巷弄型規則與門牌推導不相容 | 5 | 混合：有些是 probe 推導弱點，有些是序號正規化後 lookup key 對不上 |
| Find | 特殊行政地名 exact-text fallback 不能處理門牌 | 4 | 資料/模型 gap：目前 fallback 只支援無門牌 exact text |
| Variant | 段號中文/阿拉伯變體 | 123 | 明確可修：正規化/longest-match 邊界 |
| Variant | 路名序號阿拉伯變體 | 118 | 明確可修：正規化/longest-match 邊界 |

## Fixability

### Clearly fixable in parser/lookup

- Road names truncated at internal unit characters: lookup already has authoritative road names, so tokenizer should prefer the data-driven longest road match before generic unit token cuts.
- Non-standard but postal-data-backed road units such as `橋`, `崙`, `台`: these are valid `ROAD` values and should be treated as road/locality keys during lookup.
- Chinese/Arabic ordinal variants that normalize to an existing road key: this is a normalization gap, not a DBF data gap.

### Data/model ambiguity

- Empty `ROAD` rows and special territories (`南海諸島`, `釣魚臺列嶼`) are not normal city/district/road addresses. Some can be exact-text matched without a house number, but adding `1號` is outside the current city/district/road model.
- Groups whose DBF rules contain only broad `全` or lane-level scopes may fail a synthetic `1號` probe even when a real deliverable address would need more context.

## Priority

1. Data-driven road longest-match before generic tokenization for all non-empty `ROAD` values. This should collapse most parse truncations and many downstream find misses.
2. Add a first-class path for postal rows whose `ROAD` is empty or locality-like, instead of forcing every group into `Road + Number`.
3. Normalize Chinese/Arabic ordinal forms at the lookup key boundary and cover both road-name ordinals and section ordinals in tests.
