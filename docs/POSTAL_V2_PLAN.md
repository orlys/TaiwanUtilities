# Postal Lookup v2 實作計畫

> 分支：`experiment/nested-switch-v2`
> 目標：**組件最小、執行最快、啟動最快**。規則編譯完全在 CI 完成（下載 DBF → Postal.Builder 展開為 C# → commit → 編入 binary），執行期零讀檔、零反射，AOT/trimming 友善。
> 本文件為交接規格，供後續 session（Opus/Sonnet/Codex）直接執行。

## 0. 結論先行：為什麼不是巢狀 switch

v1 PoC（`PostalLookup.g.cs` stub + `--nested` codegen）走 city→district→road 三層 string switch。實測推算：

- 45,000 個 case ≈ 2MB IL + 374 個生成方法（每個都要 JIT），而 case 字面值（路名）在列舉需求下**反正還是要以資料形式存在**
- Roslyn string switch 底層也是 hash + 比對，對 ~120 case 的方法並不比 7 次二分搜尋比對快多少

v2 改為「**純資料 + 手寫查詢**」：

1. 所有數值規則存成**全域 SoA 陣列**（primitive array initializer → 編譯器放進 PE `.rdata` 的 field RVA blob：IL 極小、載入是一次 memcpy）
2. 路名存成**單一巨型字串常數**（US heap 只有一條字串）+ offset 陣列，查詢時 char-by-char 比對、**不建立任何字串物件**
3. `PostalLookup` 改為**手寫**類別（generator 不再產生任何方法），三層二分搜尋
4. `PostalData.Rules` 字典刪除：省下 45k 條 `"縣市|區|路"` 複合鍵（~1MB 字串）、45k 次 dict insert、67 萬個小陣列配置（目前每個 `PostalRuleSet` 持有 15 個獨立陣列 × 45k 組）

預期效果：源碼 812k 行 → ~20k 行；啟動從「67 萬次配置 + FrozenDictionary 建構」變成「~20 個陣列 memcpy」；查詢零配置（現況每次查詢都 `string.Concat` 配置一條鍵）。

## 1. 產出檔案清單

| # | 檔案 | 動作 | 摘要 |
|---|------|------|------|
| 1 | `tools/postal/Postal.Builder/Program.cs` | 改寫 codegen 區段 | 只輸出資料（見 §2 佈局）；刪除 `WriteNestedSwitchFile`、`--nested`、字典生成；新增 ushort 範圍驗證 |
| 2 | `src/TaiwanUtilities/Postal/PostalData.g.cs` | 重新生成 | 純資料，佈局見 §2 |
| 3 | `src/TaiwanUtilities/Postal/PostalLookup.g.cs` | **刪除** | stub 機制廢除 |
| 4 | `src/TaiwanUtilities/Postal/Internals/PostalLookup.cs` | **新增（手寫）** | 三層二分搜尋 + `EnumerateGroups`，見 §3 |
| 5 | `src/TaiwanUtilities/Postal/Internals/PostalRuleSet.cs` | 改寫 | 從「15 個陣列的持有者」變成 `(Offset, Count)` 視圖，SIMD 掃描全域陣列切片，見 §4 |
| 6 | `src/TaiwanUtilities/Postal/PostalRulesEngine.cs` | 修改 | 所有查詢（road / 中文序數 road / locality / village+locality）走 `PostalLookup`；`CityExists`/`DistrictExists` 從 O(45k) 前綴掃描變 O(log n)；`GetStats` 改讀 offset 陣列 |
| 7 | `src/TaiwanUtilities/Postal/ZipCode.cs` | 修改 | 點查 ×2 → `PostalLookup.FindGroup`；全表列舉 ×2 → `EnumerateGroups()` |
| 8 | `src/TaiwanUtilities/Postal/PostalDatabase.cs` | 修改 | 點查 ×1 → `PostalLookup.FindGroup` |
| 9 | `src/TaiwanUtilities/Postal/PostalAddressGenerator.cs` | 修改 | 全表列舉 → `EnumerateGroups()` |
| 10 | `test/TaiwanUtilities.UnitTests/Postal/SpecialCityTests.cs` | 修改 | `PostalData.Rules.Keys` → `EnumerateGroups()` |
| 11 | `.github/workflows/update-postal-database.yml` | 幾乎不動 | 輸出檔名不變；僅確認 log 文案 |

其餘引用 `PostalData.ZipCodePool/Departments/Offices/Scopes/SpecialRoadNames` 的程式碼**不需改動**（字串池保留原樣）。

## 2. `PostalData.g.cs` 資料佈局（generator 輸出規格）

所有「群組」= 原字典的一個鍵（city|district|road），總數 ~45k；「規則」= DBF 一列，總數 ~80k。

```csharp
internal static class PostalData
{
    // ── 階層（ordinal 排序，供二分搜尋）──
    internal static readonly string[] CityNames;          // ~22，StringComparer.Ordinal 排序
    internal static readonly int[]    CityDistrictOffsets;// 長度 = CityNames.Length + 1（前綴和）
    internal static readonly string[] DistrictNames;      // ~370，各 city 區段內 ordinal 排序
    internal static readonly int[]    DistrictGroupOffsets;// 長度 = DistrictNames.Length + 1

    // ── 路名：單一 blob + 偏移（各 district 區段內 ordinal 排序）──
    internal const string RoadBlob = "…";  // 以每 ~8000 字元一段的 "…" + "…" 常數串接輸出（編譯期合併為單一字面值）
    internal static readonly int[] RoadOffsets;           // 長度 = groupCount + 1；第 g 組路名 = RoadBlob[RoadOffsets[g] .. RoadOffsets[g+1]]

    // ── 群組 → 規則 ──
    internal static readonly int[] GroupRuleOffsets;      // 長度 = groupCount + 1（前綴和，指向下方 SoA）

    // ── 規則 SoA（長度 = RecordCount ≈ 80k；primitive initializer → RVA blob）──
    internal static readonly int[]    NumberStarts, NumberEnds;   // 門牌可能 >65535，維持 int；End 無上限以 int.MaxValue
    internal static readonly ushort[] LaneStarts, LaneEnds;       // Builder 驗證 ≤ 65534；65535 = 無上限 sentinel
    internal static readonly ushort[] AlleyStarts, AlleyEnds;     // 同上
    internal static readonly ushort[] SubStarts, SubEnds;         // 之號；SubEnds 65535 = 無上限 sentinel
    internal static readonly byte[]   RuleFlags;                  // bit0 HasLane, bit1 HasAlley, bits2-3 EvenOdd(0=不限,1=單,2=雙)
    internal static readonly ushort[] ZipIdx, DeptIdx, OfficeIdx, ScopeIdx; // 指向下方字串池；Builder 驗證池大小 < 65535

    // ── 字串池與其他（與現況相同）──
    internal static readonly string[] ZipCodePool, Departments, Offices, Scopes;
    internal static readonly HashSet<string> SpecialRoadNames;
    internal static readonly string GeneratedDate; internal static readonly int RecordCount;
}
```

生成規則：

- 數值陣列以 initializer 直出（每行 ~100 個值），**不要**拆 `InitRulesN` 方法——primitive initializer 走 `RuntimeHelpers.InitializeArray`，方法體大小不是問題；string 陣列（池、CityNames、DistrictNames）維持現有做法即可（數量小）
- **排序一致性是正確性關鍵**：三層皆以 `StringComparer.Ordinal`（= UTF-16 code unit 序）排序，與 §3 的 char-by-char 比對語意一致
- Builder 對每個 ushort 欄位做上限驗證，超界直接 `throw`（升級該欄位為 int 是後續手動決策，不靜默降級）
- `RoadBlob` 每段常數 ≤ 8000 字元，以 `+` 串接（編譯期合併，US heap 單一條目）

## 3. `PostalLookup.cs`（手寫，~120 行）

```csharp
internal static class PostalLookup
{
    // 回傳群組索引，-1 = 找不到。全程零配置。
    internal static int FindGroup(string city, string district, string road);

    internal static bool CityExists(string city);                    // 二分搜尋 CityNames
    internal static bool DistrictExists(string city, string district);

    // 供列舉消費者（Generator、ZipCode 全表掃描、測試）；road 字串延遲物化
    internal static IEnumerable<(string City, string District, string Road, int GroupIndex)> EnumerateGroups();

    internal static int GroupCount { get; }   // = RoadOffsets.Length - 1
    internal static string GetRoad(int group);// RoadBlob.Substring(...)（僅列舉/除錯用，熱路徑不呼叫）
}
```

- `FindGroup`：city 二分（22 項）→ district 在 `CityDistrictOffsets` 切片內二分 → road 在 `DistrictGroupOffsets` 切片內二分，road 比對用手寫 `CompareOrdinal(string road, string blob, int start, int len)`（char-by-char，避免依賴 System.Memory，全 TFM 通用）
- 熱路徑不呼叫 `Substring`、不 `Concat`、不算 hash

## 4. `PostalRuleSet.cs` 改寫

```csharp
internal readonly struct PostalRuleSet   // 8 bytes：純視圖
{
    public readonly int Offset;   // 進入全域 SoA 的起點
    public readonly int Count;
    // TryMatch / ScalarVerify 簽名不變，內部改讀 PostalData.NumberStarts[Offset + i] 等
}
```

- SIMD 路徑保留：`Vector256.LoadUnsafe(ref PostalData.NumberStarts[Offset + i])`，NET8+ 條件編譯與現況相同
- `ScalarVerify` 改讀 `RuleFlags` 位元與 ushort 陣列（sentinel 65535 視為無上限）；語意與現行 15 陣列版本完全等價
- 消費端（`ZipCode.cs`、`PostalDatabase.cs` 直接讀 `ruleSet.ZipCodeIndices[i]` 之處）改為 `PostalData.ZipIdx[ruleSet.Offset + i]`

## 5. 消費端替換對照

| 現況 | v2 |
|------|----|
| `PostalData.Rules.TryGetValue($"{c}\|{d}\|{r}", out rs)` | `var g = PostalLookup.FindGroup(c, d, r); if (g >= 0) rs = new PostalRuleSet(GroupRuleOffsets[g], GroupRuleOffsets[g+1] - GroupRuleOffsets[g])` |
| `foreach (var kvp in PostalData.Rules)` + 手動 split 鍵 | `foreach (var (city, district, road, g) in PostalLookup.EnumerateGroups())` |
| `Rules.Keys.StartsWith(prefix)` 掃描（CityExists 等） | `PostalLookup.CityExists / DistrictExists` |
| `GetStats`：遍歷字典 | `GroupCount` + `RecordCount`，O(1) |

`PostalRulesEngine.Find` 的 fallback 順序維持不變：road → `ArabicToChineseInRoad(road)` → locality → village+locality，全部改呼叫 `FindGroup`（鍵空間相同，覆蓋率不變）。

## 6. 執行步驟（建議分工）

1. **Builder 改寫**（最精細，建議 Opus 或 Codex）：改 `Program.cs` codegen 區段。輸入 DBF 取得方式：`.\tools\postal\Download-PostalDatabase.ps1` → `temp\rall1.dbf`
2. **執行期類別**（Opus）：§3 `PostalLookup.cs` 新增、§4 `PostalRuleSet.cs` 改寫
3. **重新生成**：`dotnet run --project tools/postal/Postal.Builder --framework net10.0 -- codegen temp\rall1.dbf src\TaiwanUtilities\Postal\PostalData.g.cs`，並刪除 `PostalLookup.g.cs`
4. **消費端 + 測試改寫**（Sonnet 可）：§5 對照表逐一替換（§1 表格 #6–#10）
5. **驗證**：
   - `dotnet test test/TaiwanUtilities.UnitTests/ --filter "FullyQualifiedName~Postal"` 全綠 → 再跑全套（基準 1074 tests）
   - 量測並記錄：`TaiwanUtilities.dll` 各 TFM 大小（改前 vs 改後）、簡單 Stopwatch 跑 100 萬次 `PostalRulesEngine.Find` 比較改前後（不需引入 BenchmarkDotNet）
6. **CI**：確認 `update-postal-database.yml` 的 codegen/commit step 檔名無誤（應不需改）

## 7. 驗收條件

- [x] 所有 Postal 測試通過（168），全套 1052 測試零失敗
- [x] `TaiwanUtilities.dll`（net8.0）比 `experiment/nested-switch` 分支明顯縮小
- [x] 查詢路徑零配置（`PostalLookup.FindGroup` 不建立字串/鍵/hash）
- [x] 全部四個 TFM 建置通過，零警告（不新增任何 NuGet 依賴）
- [x] `PostalData.g.cs` 為唯一生成檔，Builder 單一指令產出

## 8. 實測結果（2026-07-06，net8.0 Release）

| 指標 | v1（dictionary） | v2（SoA + 二分搜尋） | 改善 |
|---|---|---|---|
| `TaiwanUtilities.dll` | 13.51 MB | 5.30 MB | **−61%** |
| 靜態資料初始化 | 10,548 ms | 556 ms | **−95%（19×）** |
| 常駐 managed heap | 30.0 MB | 3.9 MB | **−87%** |
| `Find` 端到端 | 36.2 µs/op | 32.1 µs/op | −11% |
| `Find` 配置 | 13,984 B/op | 13,864 B/op | −120 B（複合鍵消失） |
| `PostalData.g.cs` 源碼 | 812,935 行（~40 MB） | ~7.3 MB | −82% |

v1 初始化 10.5 秒的主因是 JIT 需編譯 563 個各 1,400 行的 `InitRulesN` 方法並執行
45k 次 dict insert + 67 萬個小陣列配置；v2 的 RVA blob 由 runtime memcpy 載入。

**後續機會（超出本次範圍）**：`Find` 端到端 32 µs 的瓶頸在
`ZipCodeResult.ExactMatch` 會重新 tokenize + re-parse 地址（每次 ~14 KB 配置），
查詢本身僅奈秒級。要壓端到端延遲，下一刀在結果物件的延遲物化。
