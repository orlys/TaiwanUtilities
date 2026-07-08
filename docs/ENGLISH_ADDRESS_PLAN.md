# 英文地址轉換功能 實作計畫

> 目標：中文台灣地址 → 官方格式英文地址。
> 資料基礎：中華郵政 rall1.dbf 的官方英文欄位（ECITY / EAREA / EROAD，漢語拼音）。

## 0. 範圍（已定案）

- **方向**：僅中 → 英（英 → 中反查暫不做，英文解析歧義大，日後再議）
- **深度**：完整地址組裝（含 No./Sec./Ln./Aly./F. 反序組裝），非只翻地名
- **路名資料**：嵌入官方 EROAD（準確含歷史拼法 Tamsui/Keelung，不用演算法羅馬化）

## 1. 資料事實（已驗證，2026-07）

| 項目 | 數字 | 備註 |
|---|---|---|
| DBF 總列數 | 79,876 | |
| distinct 城市\|區\|路 | 44,658 | = 現有 PostalLookup 群組數 |
| EROAD 空白列 | **0（100% 覆蓋）** | 每條路都有官方英文 |
| ECITY/EAREA 空白列 | 各 4 | 特殊領地（南海諸島/釣魚臺），edge case |
| `EROAD` 已含「段」 | 是 | 「忠孝東路一段」→「Sec. 1, Zhongxiao E. Rd.」 |

英文地址採**反序**（小到大）+ 逗號分隔，例：
```
中：臺北市中正區忠孝東路一段1號5樓
英：5F., No. 1, Sec. 1, Zhongxiao E. Rd., Zhongzheng Dist., Taipei City
```

## 2. 資料層（Postal.Builder codegen 擴充）

在 `PostalData.g.cs` 新增三組英文資料，與現有階層平行對齊：

```csharp
// 與 CityNames[22] 同索引
internal static readonly string[] EnglishCityNames;
// 與 DistrictNames[~370] 同索引
internal static readonly string[] EnglishDistrictNames;
// 與群組（RoadOffsets）同索引，44,658 條；含段的完整路名英文
internal static readonly string   EnglishRoadBlob;    // 單一大字串（同 RoadBlob 手法）
internal static readonly int[]     EnglishRoadOffsets; // 群組 → EnglishRoadBlob 切片
```

- **城市/區英文**（22 + 370 條）：小資料，直接 string[]
- **路名英文**（44,658 條，~1.1MB 文字）：用 RoadBlob 同款「單一巨型字串 + offset」手法，避免 4.5 萬個字串物件
- Builder 讀 DBF 時一併取 ECITY/EAREA/EROAD；同一 (city) / (city,area) 的英文取第一筆非空值（實測一致，4 筆空白領地填 `city`/`area` 中文或留空）
- **成本**：組件預估 +1~1.5MB（純英文文字，RVA blob，啟動零配置）

## 3. 查詢層（PostalLookup 擴充）

現有 `FindGroup(city, district, road)` 只回群組索引。英文組裝需要城市索引 c 與區索引 d（取 EnglishCityNames[c] / EnglishDistrictNames[d]）。新增一個回傳三索引的查詢：

```csharp
// 回傳 (cityIdx, districtIdx, groupIdx)，任一 < 0 表未命中
internal static bool TryFindIndexed(string city, string district, string road,
    out int cityIdx, out int districtIdx, out int groupIdx);
```

（把現有 FindGroup 的三層二分搜尋沿途索引一併輸出即可，零額外成本。）

## 4. 執行層（公開 API）

```csharp
public partial class PostalAddress
{
    /// <summary>轉換為官方格式英文地址；無法解析或查無路名英文時回 null。</summary>
    public string? ToEnglish();
}
```

流程：
1. 已解析的 `PostalAddress`（City/District/Road/Section/Lane/Alley/Number/SubNumbers/Floor/SubFloor）
2. `PostalLookup.TryFindIndexed` 取城市/區/路英文（路的英文已含 Sec.）
3. 機械格式化門牌層級（見 §5）
4. 反序組裝，逗號分隔

同時提供靜態便利方法（可選）：
```csharp
// ZipCode.ToEnglishAddress("臺北市中正區忠孝東路一段1號5樓") → 上面英文
public static string? ToEnglishAddress(string address);  // = Parse 後 ToEnglish
```

## 5. 門牌層級英譯規則（純機械，零資料）

| 中文組件 | 英文 | 範例 |
|---|---|---|
| N號 | No. N | 1號 → No. 1 |
| N之M號 | No. N-M | 1之2號 → No. 1-2 |
| N樓 | NF. | 5樓 → 5F. |
| N樓之M | NF.-M | 5樓之3 → 5F.-3 |
| 地下N樓 | B1F. 等 | 需確認 DBF/慣例 |
| N巷 | Ln. N | 182巷 → Ln. 182 |
| N弄 | Aly. N | 3弄 → Aly. 3 |
| N段 | Sec. N | **已含於 EROAD，不另處理** |
| 里/村 | Vil. | 官方英文通常保留里，鄰(鄰)省略 |

**組裝順序**（小到大）：`[F.], No., [Aly.], [Ln.], <EROAD 含 Sec. + 路名>, <EAREA 區>, <ECITY 市>`

郵遞區號放置：官方英文常見「... City」後加 3 碼或 6 碼，或置於開頭。預設**不加**，另開 overload 可選加（待定）。

## 6. 邊界情況

- **特殊領地（4 筆缺 ECITY/EAREA）**：南海諸島/釣魚臺 → 英文城市/區留空或以拼音代填；`ToEnglish` 對這類回 null 或部分英文（政策待定，建議 null + 文件說明）
- **查無路名英文**（實測 0 筆，但防禦性處理）：回 null
- **地址解析失敗 / 缺城市**：回 null（與 `ZipCode.Find` 一致的寬容策略）
- **中文序數/阿拉伯數字路名**：沿用現有 `ArabicToChineseInRoad` 正規化後再查（四維3路 → 四維三路 → 查 EROAD）

## 7. 執行步驟與分工建議

1. **Builder codegen 擴充**（Codex）：讀 ECITY/EAREA/EROAD → 輸出三組英文資料；重生 PostalData.g.cs
2. **PostalLookup.TryFindIndexed**（Opus/Sonnet）：三索引查詢
3. **PostalAddress.ToEnglish + 機械格式化**（Opus）：§5 規則 + 反序組裝
4. **測試**（Sonnet）：對照官方樣本地址（各縣市代表地址 → 已知官方英文），含門牌/樓/巷弄/段組合、歷史拼法（淡水 Tamsui、基隆 Keelung）、特殊領地、無門牌等

## 8. 驗收條件

- [ ] `PostalAddress.ToEnglish()` 對代表性地址產出正確官方格式英文（比對中華郵政中英對照樣本）
- [ ] 歷史拼法正確（Tamsui/Keelung/Kinmen 等，因嵌入官方 EROAD 而非演算法）
- [ ] 門牌/樓/之號/巷弄反序組裝正確
- [ ] 四 TFM 建置零警告；組件增量記錄於 PR
- [ ] 公開 API 僅新增（PostalAddress.ToEnglish、可選 ZipCode.ToEnglishAddress），無破壞性變更
- [ ] 特殊領地與解析失敗回 null，不拋例外
