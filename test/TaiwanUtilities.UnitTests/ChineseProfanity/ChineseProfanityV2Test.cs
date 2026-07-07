namespace TaiwanUtilities.UnitTests;

using Xunit;

/// <summary>
/// WP3 測試擴充：簡繁/變體規避、台語新詞條、誤殺防護、Replace 座標正確性。
/// 對應 PROFANITY_V2_PLAN.md § WP3。
/// </summary>
public class ChineseProfanityV2Test
{
    // ================================================================
    // 1. 簡繁/變體規避（正向）
    //    注意：GY / G8 計畫書明確不加（§ 略過候選），不測。
    // ================================================================

    [Theory]
    [InlineData("操你妈")]           // 妈→媽 正規化，VerbKinshipPattern 命中
    [InlineData("干你娘")]           // 干→幹 正規化，VerbKinshipPattern 命中
    [InlineData("幹.你.娘")]         // CompoundMatcher boundary-gap walk（. 為 boundary）
    [InlineData("幹 你 娘")]         // 空白也是 boundary
    [InlineData("靠~北")]            // ~ 是 boundary，CompoundMatcher 靠北 命中
    public static void 簡繁變體規避應被偵測(string profaneText)
    {
        Assert.True(ChineseProfanity.Censor(profaneText));
    }

    // ================================================================
    // 2. 台語新詞條（正向）— WP2 產出 (b) 清單
    // ================================================================

    [Theory]
    // 哭夭
    [InlineData("你在那邊哭夭什麼")]
    [InlineData("哭夭啦真衰")]
    // 哭枵（本字）
    [InlineData("哭枵喔又塞車")]
    // 機掰人
    [InlineData("你這個機掰人")]
    // 雞掰人
    [InlineData("真是有夠雞掰人")]
    // 膣屄
    [InlineData("罵一句膣屄")]
    // 卒仔
    [InlineData("你這個卒仔不敢來")]
    [InlineData("一群卒仔")]
    // 王八（獨立詞，Slur）
    [InlineData("你這個王八")]
    [InlineData("烏龜王八")]
    public static void 台語與繁中新詞條應被偵測(string profaneText)
    {
        Assert.True(ChineseProfanity.Censor(profaneText));
    }

    [Theory]
    // 回歸案例：既有機制仍應命中，勿因新詞破壞
    [InlineData("衝三小")]           // 三小 是 Slur（衝 不是動詞，三小 獨立 Slur 命中）
    [InlineData("賽三小")]           // CompoundMatcher
    [InlineData("幹恁娘")]           // VerbKinshipPattern：幹(Verb)+恁(Pronoun)+娘(Kinship)
    [InlineData("靠夭")]             // CompoundMatcher
    public static void 回歸案例應被偵測(string profaneText)
    {
        Assert.True(ChineseProfanity.Censor(profaneText));
    }

    // ================================================================
    // 3. 誤殺防護（負向）— 全部 Censor 應為 false
    // ================================================================

    [Theory]
    // 三小 + 時間量詞系列（WP2 SafeWord 修正）
    [InlineData("公車三小時")]
    [InlineData("開會三小時後")]
    [InlineData("等了三小時")]
    [InlineData("分成三小組")]
    // 卒 合法用法（需 卒+仔 相連才觸發）
    [InlineData("卒業典禮")]
    [InlineData("身先士卒")]
    [InlineData("他只是個小卒")]
    [InlineData("過河卒子")]
    // 膣 醫學用法（無 屄 相連）
    [InlineData("陰道膣部檢查")]
    // 夭 合法用法（單用，非 compound）
    [InlineData("夭折")]
    [InlineData("夭壽")]
    [InlineData("哭泣")]
    // 干/幹 → 正規化後也應被 SafeWord 或 IsolatedExpletive 規則保護
    [InlineData("干貝很新鮮")]
    [InlineData("若干年後")]
    // 乾 — 不在正規化表，不會被摺疊成幹
    [InlineData("乾淨的房間")]
    [InlineData("餅乾很好吃")]
    // 操/體操
    [InlineData("操場跑步")]
    [InlineData("體操選手")]
    // 射
    [InlineData("注射疫苗")]
    [InlineData("射擊比賽")]
    public static void 計畫書誤殺防護例句不應被偵測(string normalText)
    {
        Assert.False(ChineseProfanity.Censor(normalText));
    }

    [Theory]
    // 中性台語
    [InlineData("恁兜")]           // 恁(Pronoun)+兜（非詞彙），無 Kinship/Particle 接續
    [InlineData("恁爸媽都好嗎")]   // 恁(Pronoun)+爸(Kinship)，後接 媽(Kinship) 非 Particle，不觸發
    public static void 中性台語不應被誤殺(string neutralText)
    {
        Assert.False(ChineseProfanity.Censor(neutralText));
    }

    // ================================================================
    // 4. Replace 座標正確性
    // ================================================================

    /// <summary>
    /// 分隔穿插案例：遮罩範圍應涵蓋分隔符，前後文字完整保留。
    /// 「幹.你.娘」5 字元全遮，前綴「這是」後綴「啊」保留。
    /// </summary>
    [Fact]
    public static void 分隔穿插案例Replace應涵蓋分隔符()
    {
        var input = "這是幹.你.娘啊";

        Assert.True(ChineseProfanity.Censor(input));

        // 完整輸出比對：遮罩恰為 5 字元（含分隔符），尾端「啊」保留
        Assert.Equal("這是○○○○○啊", ChineseProfanity.Replace(input));
    }

    /// <summary>
    /// 空白穿插案例：「幹 你 娘」整段被遮。
    /// </summary>
    [Fact]
    public static void 空白穿插案例Replace應正確遮罩()
    {
        var input = "幹 你 娘";
        var result = ChineseProfanity.Replace(input);

        Assert.NotNull(result);
        Assert.False(ChineseProfanity.Censor(result));

        // 應為全部遮蔽（5 字元全替換為 ○）
        Assert.Equal("○○○○○", result);
    }

    /// <summary>
    /// Replace(text, '*') 自訂字元驗證。
    /// </summary>
    [Fact]
    public static void 自訂字元Replace應使用指定字元遮罩()
    {
        var input = "幹你娘";
        var result = ChineseProfanity.Replace(input, '*');

        Assert.NotNull(result);
        Assert.Equal("***", result);
        Assert.False(ChineseProfanity.Censor(result));
    }

    /// <summary>
    /// 靠~北：獨立與前後文包圍兩種形式皆應被偵測，遮罩含分隔符。
    /// </summary>
    [Fact]
    public static void 靠波浪北獨立與包圍形式皆應被偵測()
    {
        // 獨立形式
        Assert.True(ChineseProfanity.Censor("靠~北"));
        Assert.Equal("○○○", ChineseProfanity.Replace("靠~北"));

        // 前後文包圍形式
        Assert.True(ChineseProfanity.Censor("你靠~北啦"));
        Assert.Equal("你○○○啦", ChineseProfanity.Replace("你靠~北啦"));

        // 句讀分隔的合法文本不受跳隔匹配誤傷
        Assert.False(ChineseProfanity.Censor("危險性。交通部負責"));
    }

    /// <summary>
    /// 含髒話的長句：Replace 後安全詞（操場、媽媽）不被遮蔽。
    /// </summary>
    [Fact]
    public static void Replace後安全詞不被遮蔽()
    {
        var input = "在操場上幹你娘真是廢物，媽媽說要注意品行";
        var result = ChineseProfanity.Replace(input);

        Assert.NotNull(result);
        Assert.Contains("操場", result);
        Assert.Contains("媽媽", result);
        Assert.False(ChineseProfanity.Censor(result));
    }

    // ================================================================
    // 5. 台語新詞條個別 Replace 輸出長度驗證
    //    確認 index/length 座標正確（Replace 後字元數與原文一致）
    // ================================================================

    [Theory]
    [InlineData("哭夭")]       // 2 字 → ○○
    [InlineData("哭枵")]       // 2 字 → ○○
    [InlineData("機掰人")]     // 3 字 → ○○○
    [InlineData("雞掰人")]     // 3 字 → ○○○
    [InlineData("膣屄")]       // 2 字 → ○○
    [InlineData("卒仔")]       // 2 字 → ○○
    [InlineData("王八")]       // 2 字 → ○○
    public static void 台語新詞條Replace長度應與原文一致(string profaneWord)
    {
        var result = ChineseProfanity.Replace(profaneWord);
        Assert.NotNull(result);
        // Replace 應為等長替換
        Assert.Equal(profaneWord.Length, result!.Length);
        // 全部字元應為 ○
        Assert.All(result, ch => Assert.Equal('○', ch));
    }
}
