# Expression Tree Per-Road 編譯決策樹

## 架構概述

PreloadedRulesEngine 使用 .NET Expression Tree 為每條路（city|district|road）的規則集合
編譯出一個原生 `Func<int, int, int, int, int>` 決策樹 delegate，
把資料驅動的規則「烘焙」成原生程式碼，消除 `foreach` 遍歷。

### 核心型別

| 型別 | 位置 | 說明 |
|------|------|------|
| `CompiledRoadMatcher` | `Internals/CompiledRule.cs` | delegate：`(number, subNumber, lane, alley) => ruleIndex` |
| `CompiledRoad` | 同上 | readonly struct，持有 Matcher + RuleMetadata[] |
| `RuleMetadata` | 同上 | readonly struct，持有 ZipCode/Department/Office/Scope |
| `RoadRuleCompiler` | 同上 | 靜態編譯器，將 `List<PostalRule>` → `CompiledRoad` |

### 編譯流程

```
PostalRule[]  ──→  按特異性排序  ──→  Expression 條件鏈  ──→  Lambda.Compile()
                                                              ↓
                                                    CompiledRoadMatcher delegate
```

1. **排序**：按特異性降序（巷 > 弄 > 附號 > 特定號 > 範圍 > 單雙號 > 全部）
2. **條件建構**：每條規則生成 `Expression` AND 鏈
3. **決策樹堆疊**：從最後一條規則往前建構 if-else 鏈
4. **編譯**：`Expression.Lambda<CompiledRoadMatcher>().Compile()`

### 匹配維度

每條規則的條件可包含以下維度的 AND 組合：

| 維度 | Expression | 語義 |
|------|-----------|------|
| 巷號（有限制）| `lane >= LaneStart && lane <= LaneEnd` | 地址必須有巷且在範圍內 |
| 巷號（無限制）| `lane == 0` | 地址不應有巷 |
| 弄號 | 同巷號邏輯 | |
| 單號 | `(number & 1) == 1` | |
| 雙號 | `(number & 1) == 0` | |
| 主號範圍 | `number >= Start && number <= End` | |
| 附號範圍 | `subNumber >= StartSub && subNumber <= EndSub` | |

### 資料流

```
ZipCode.Find("臺北市大安區和平東路96巷17弄1號")
  → PostalAddress.Parse()  → { City="臺北市", District="大安區", Road="和平東路", Lane="96巷", Alley="17弄", Number=1 }
  → PreloadedRulesEngine.Find(addr)
    → key = "臺北市|大安區|和平東路"
    → ParseNumericPrefix("96巷") → 96
    → ParseNumericPrefix("17弄") → 17
    → compiledRoad.Matcher(1, 0, 96, 17) → ruleIndex
    → Metadata[ruleIndex].ZipCode
```

## 效能特性

- **啟動開銷**：約 100-200ms（一次性編譯所有路的決策樹）
- **查詢延遲**：< 0.5ms（呼叫已編譯的 delegate）
- **記憶體占用**：約 6-8 MB（80,000+ 條規則 + delegate）
- **併發安全**：唯讀資料結構，無鎖競爭

## 檔案清單

| 檔案 | 說明 |
|------|------|
| `src/TaiwanUtilities/Postal/Internals/CompiledRule.cs` | 編譯器核心（RoadRuleCompiler + CompiledRoad + RuleMetadata） |
| `src/TaiwanUtilities/Postal/PreloadedRulesEngine.cs` | 引擎整合（LoadAllRules 編譯 + Find 呼叫 delegate） |
| `test/TaiwanUtilities.UnitTests/Postal/PreloadedRulesEngineTests.cs` | 測試（含巷弄/附號/單雙號案例） |
