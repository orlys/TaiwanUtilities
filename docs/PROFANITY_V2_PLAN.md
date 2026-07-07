# ChineseProfanity v2 實作計畫

> 分支：`refactor/profanity-v2`
> 目標：強化繁體中文與台語（閩南語）髒話偵測的內部邏輯——規避變體處理、台語系統性覆蓋、管線整潔化。
> **公開 API 完全凍結**，本文件為分工執行規格（Codex / Opus / Sonnet）。

## 0. 凍結面（不可更動）

```csharp
// public API — 簽章與行為語義不變
ChineseProfanity.Censor(string?) → bool
ChineseProfanity.Replace(string?) → string?
ChineseProfanity.Replace(string?, char) → string?
ReplacementCharacters.{WhiteCircle, HalfWidthAsterisk, FullWidthAsterisk}

// internal 契約 — ChineseProfanity.cs 呼叫點不變
ProfanityAnalyzer.Analyze(ReadOnlySpan<char>) → List<(int index, int length)>
```

`(index, length)` 必須是**原始輸入字串**的座標（Replace 靠它遮罩原文）。
既有 ChineseProfanityTest 全數維持綠燈。多 TFM（net10/net8/netstandard2.0/net472）、零新依賴、不用 regex。

## 1. 三大工作包

### WP1（Codex）：管線重構 + 規避變體處理

**1a. 正規化層（新增，Tokenize 前）**

建 `Internals/ProfanityNormalizer.cs`：輸入 span，輸出同長度 `char[]`（**1:1 映射，index 不變**）：
- 全形 ASCII → 半形（`Ｂ`→`B`、`＊`→`*`）
- ASCII 小寫 → 大寫（`b`→`B`、`g`→`G`）
- 簡體/異體 → 繁體摺疊表（curated 1:1 表，只收詞庫相關字）：
  `妈→媽 干→幹 奸→姦 骂→罵 赣→贛 鸟→鳥 鸡→雞 滚→滾 烂→爛 贱→賤 残→殘 废→廢 脑→腦 蛋→蛋(略) 逼→逼(略) 傻→傻(略)`
  （實作時：逐一檢視 lexicon/compound 的每個字，補其常見簡體/異體形；**不確定的不加**）
- **`乾` 不摺疊**（乾淨/餅乾誤殺風險高於收益）；`干→幹` 要摺疊（「干你娘」是高頻規避），配套：SafeWordDictionary 的詞條**建表時用同一張摺疊表正規化**，兩側一致（干貝→幹貝，safe 表同樣摺疊後可命中）
- Analyzer/SafeWord/Lexicon/Compound 全部改吃正規化後的 span；回傳座標不受影響（1:1）

**1b. 跳隔字元的 compound 匹配**

`CompoundMatcher`（從 `CompoundVerbPattern` 抽離成獨立 static class，不再繼承 `ConstructionPattern`）：
- Trie walk 時允許跳過「邊界字元」（空白、標點、`*`、`.`、`~`、`-`、`_`、`。`、`、` 等 — 沿用現有 IsBoundary 判定）
- 每兩個實字元之間最多跳 2 個邊界字元（與 MAX_TOKEN_GAP 一致）
- 回傳的 length **涵蓋被跳過的分隔符**（`幹.你.娘` 整段 5 字元遮罩）
- 匹配至少 2 個實字元才算（避免單字+分隔的誤判）

**1c. 管線整潔化（行為不變的重構）**

- Phase 0 與 Tokenize 合併為**單次掃描**：每個位置只查一次 SafeWord（現在查兩遍）
- `HashSet<int> compoundCovered` → `bool[] covered`（text.Length）
- `ProfanityLexicon.Classify`：改用 TrieDictionary 的 streaming walk（單次由短到長走訪，沿路記錄最後命中），淘汰「len 1..6 各查一次」；TrieDictionary 若無此 API 就加一個 internal `LongestMatch(span, index, out value)` 方法
- `CompoundVerbPattern` 從 `s_patterns`/pattern 階層移除（它的 TryMatch 本來就永遠 null）

### WP2（Opus）：詞庫擴充 — 台語與繁中系統性覆蓋

**語言學紅線（最重要）**：
- 使用者對台語正確性極度敏感（前例：「欠賽」因不自然被移除，正確是「欠駛」）
- **不確定的詞寧可不加**——最小誤殺 > 最大偵測
- 每個新詞條在測試中要有對應案例；可能誤殺的鄰近詞要加 SafeWord + 負面測試

**台語擴充候選（實作時逐一審慎判斷，不是照單全收）**：
- 代詞：`恁`（已有）確認 `恁娘`、`恁老母`、`恁祖媽`（女性自嗆「老娘」，罵境用法）、`恁爸`／`恁北`（= 拎北）
- 動詞+三小 compound 系列：`衝三小`、`創三小`、`公三小`、`講三小`、`問三小`、`吵三小`、`哭三小`（現有 看/殺/沙/莎三小）
- `機掰`/`雞掰` 衍生：`機掰人`、`雞掰人`、`膣屄`（本字）、`GY`、`G8`、`g8`（經 1a 正規化後 `GY`/`G8` 大寫形一致）
- `哭枵`（哭夭本字）、`靠妖`、`哭夭`
- `姦恁娘`、`幹恁娘`、`幹恁老師`、`幹拎老師`、`塞恁娘`？（塞 是否為 賽 的常見書寫變體→查證，不確定就不加）
- `賭爛`/`肚爛`/`杜爛`（tōo-lān）→ 屬輕度不滿，**建議不加**（誤殺日常抱怨）
- `俗辣`（已有）、`卒仔`、`遜咖`？（遜咖偏戲謔，建議不加）
- `破麻`（已有）、`臭俗辣`
- 繁中補充：`王八`（獨立）、`不要臉`？（偏罵但非髒話，不加）、`他媽的` 系列由 pattern 涵蓋確認即可
- SafeWord 防護同步補：新詞條的合法同形（如 `公三小` vs `公三個小時`？→ 驗證 pattern 邊界）

**產出**：只動 `ProfanityLexicon.cs`、`SafeWordDictionary.cs`、`CompoundMatcher`（詞條區）+ 對應測試素材清單（交給 WP3 的清單檔）

### WP3（Sonnet）：測試擴充

- 既有 12 個測試方法全綠（回歸底線）
- 新增 Theory 組：
  1. **簡繁/變體規避**：`操你妈`、`干你娘`、`幹.你.娘`、`幹 你 娘`、`靠~北`、`ㄇ的`？（不在範圍就略）、`G8`、`g8`、`ＧＹ`
  2. **台語正向**：WP2 產出的每個新詞條至少一案例
  3. **誤殺防護（負向，同等重要）**：`干貝很新鮮`、`若干年後`、`乾淨`、`餅乾`、`公車三小時`、`他媽媽很好`（既有？確認）、`馬的傳人`？、`操場`、`體操`、`注射`、`射擊比賽`、`阿姨`、`很屌的表演`（既有 safe）等
  4. **Replace 座標正確性**：分隔穿插匹配後遮罩範圍含分隔符、前後文完整保留
- 測試放進現有 `ChineseProfanityTest.cs` 或同目錄新檔

## 2. 執行順序與驗收

```
WP1（Codex，管線+正規化）→ build + 既有測試綠
→ WP2（Opus，詞庫）→ build + 綠
→ WP3（Sonnet，測試）→ 全套綠
→ Fable 驗收：diff review + 全 TFM build + 全套測試
```

驗收條件：
- [ ] 公開 API 無任何簽章變更（`git diff` 檢查 public 面）
- [ ] 全套測試通過（既有 + 新增），四 TFM 建置零警告
- [ ] `操你妈`／`干你娘`／`幹.你.娘` 可偵測；`干貝`／`乾淨`／`操場` 不誤殺
- [ ] 台語新詞條各有正向測試；語言學紅線遵守（不自然的詞不出現）

## WP2 產出：新詞條與測試素材

> 執行者：Opus。範圍嚴守——僅動 `CompoundMatcher.cs`、`ProfanityLexicon.cs`、`SafeWordDictionary.cs` 的詞條區。
> 決策方法：以一次性 probe console 針對「候選詞 + 其合法鄰近文本」實測現況，再逐條判斷。
> Release net8.0 建置零警告；`FullyQualifiedName~ChineseProfanity` 既有 161 測試全綠。

### (a) 新增詞條

| 詞條 | 檔案 | 類別／機制 | 判斷理由 |
| --- | --- | --- | --- |
| `哭夭` | CompoundMatcher | 閩南語 compound | khàu-iau，通行寫法，表抱怨／該死；高頻且無合法同形 |
| `哭枵` | CompoundMatcher | 閩南語 compound | 哭夭之本字寫法，同上 |
| `機掰人` | CompoundMatcher | compound（完整遮罩） | 原 `機掰` 由 BodyPart 命中僅遮 `○○人`；補 compound 讓整詞遮 `○○○` |
| `雞掰人` | CompoundMatcher | compound（完整遮罩） | 同上，`雞掰` 異寫 |
| `膣屄` | CompoundMatcher | 閩南語 compound（本字） | 機掰／雞掰之本字（tsi-bai）；單獨 `膣`（醫學詞）不觸發，需 `膣+屄` 相連 |
| `卒仔` | CompoundMatcher | 閩南語 compound | tsut-á，膽小鬼；需 `卒+仔` 相連，與 卒業／士卒／小卒／過河卒 不衝突 |
| `王八` | ProfanityLexicon（Slur） | 繁中補充 | 幾乎恆為罵詞（烏龜／戴綠帽）；`王八蛋`／`王八羔子` 仍以最長匹配優先命中 |

### SafeWord 新增（誤殺防護）

| 安全詞 | 保護對象 | 理由 |
| --- | --- | --- |
| `三小時` `三小節` `三小段` `三小隊` `三小組` `三小塊` | `三小`（既有 Slur） | 修正**既有誤殺**：`公車三小時`／`開會三小時後` 會命中 `三小`。SafeWord 於各位置優先命中，`三小時` 先被判安全 |

### (b) 正向測試例句（每詞至少一例，交 WP3）

- `哭夭` — `你在那邊哭夭什麼`、`哭夭啦真衰`
- `哭枵` — `哭枵喔又塞車`
- `機掰人` — `你這個機掰人`
- `雞掰人` — `真是有夠雞掰人`
- `膣屄` — `罵一句膣屄`
- `卒仔` — `你這個卒仔不敢來`、`一群卒仔`
- `王八` — `你這個王八`、`烏龜王八`（遮 `烏龜○○`）
- 回歸（既有機制仍應命中，勿因新詞破壞）：`衝三小`、`賽三小`、`幹恁娘`、`靠夭`

### (c) 誤殺防護例句（負向，同等重要，交 WP3）

- `三小`／時間量詞：`公車三小時`、`開會三小時後`、`等了三小時`、`分成三小組` → 皆**不得**命中
- `卒` 合法：`卒業典禮`、`身先士卒`、`他只是個小卒`、`過河卒子` → 不得命中
- `膣` 醫學：`陰道膣部檢查` → 不得命中（無 `屄` 相連）
- `枵` 合法／中性：`枵鬼`（單用非本清單詞）→ 不得命中
- `夭` 合法：`夭折`、`夭壽`（單用）、`哭泣` → 不得命中

### 略過的候選及理由（遵守語言學紅線：不確定寧可不加、最小誤殺優先）

| 候選 | 決定 | 理由 |
| --- | --- | --- |
| `衝/創/公/講/問/吵/哭三小` | 不加 compound | `三小` 已為 Slur，全部已命中；真正問題是 `三小時` 誤殺，改以 SafeWord 修正 |
| `GY` `G8` `g8` `ＧＹ` `Ｇ８` | **不加** | 誤殺風險過高：`G8`＝八大工業國高峰會、`GY`＝常見英文縮寫／公司名；紅線「最小誤殺」優先 |
| `姦恁娘` `幹恁娘` `幹恁老師` `幹拎老師` | 不加（已涵蓋） | `恁`＝Pronoun、`娘/老母/老師`＝Kinship，VerbKinshipPattern 已完整命中，加 compound 為冗餘 |
| 裸詞 `恁娘` `恁老母` `恁爸` `恁北` `恁祖媽` | **不加** | `恁`＝台語「你（們）的」，`恁兜/恁厝/恁爸媽都好嗎` 為中性台語；裸「你母」語義曖昧，僅動詞形明確為罵，故只保留動詞形 |
| `塞恁娘` 的 `塞` | 不加 | 計畫標「查證」；`塞`是否為`賽`常見書寫變體未能確認，拿不準不加 |
| `靠妖` | 不加 | 通行寫法為 `靠腰/靠邀`（已收）；`靠妖` 罕見且與 `靠妖精` 誤殺邊界不乾淨 |
| `膣`（本字單用） | 不加 | 醫學詞，單用誤殺；僅收 `膣屄` |
| `賭爛/肚爛/杜爛` | 不加 | 計畫標「建議不加」——輕度不滿，誤殺日常抱怨 |
| `遜咖` | 不加 | 計畫標「建議不加」——偏戲謔 |
| `不要臉` | 不加 | 計畫標「不加」——偏罵但非髒話 |
| `臭俗辣` | 不加（已涵蓋） | `臭`＝PejorativePrefix、`俗辣`＝Slur，PrefixedSlurPattern 已命中 |
| `他媽的` 系列 | 不加（已涵蓋） | 由 VerbKinship／Exclamatory pattern 涵蓋，實測命中 |
