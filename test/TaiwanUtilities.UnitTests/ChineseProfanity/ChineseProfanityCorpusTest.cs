namespace TaiwanUtilities.UnitTests;

using System;
using System.IO;
using System.Linq;
using Xunit;

public class ChineseProfanityCorpusTest
{
    private const int MaxFailuresToPrint = 25;

    [Fact]
    public static void FixtureContainsExactlyTenThousandUniqueProfaneSentences()
    {
        var sentences = ReadProfanityCorpus();

        Assert.Equal(ChineseProfanityCorpusGenerator.TargetCorpusSize, sentences.Length);
        Assert.Equal(sentences.Length, sentences.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public static void FixtureMatchesTheCheckedInGenerator()
    {
        var fixture = ReadProfanityCorpus();
        var generated = ChineseProfanityCorpusGenerator.GenerateProfanityCorpus();

        Assert.Equal(ChineseProfanityCorpusGenerator.TargetCorpusSize, generated.Count);
        Assert.Equal(fixture, generated);
    }

    [Fact]
    public static void FixtureUsesManyBalancedSentencePrefixes()
    {
        var prefixGroups = ReadProfanityCorpus()
            .GroupBy(sentence => sentence[..Math.Min(8, sentence.Length)], StringComparer.Ordinal)
            .Select(group => group.Count())
            .ToArray();

        Assert.True(prefixGroups.Length >= 15, "Expected at least 15 distinct 8-character prefixes.");
        Assert.True(prefixGroups.Max() <= 750, "Expected no 8-character prefix to dominate the corpus.");
    }

    [Fact]
    public static void ProfanityCorpusSentencesShouldAllBeDetected()
    {
        var misses = ReadProfanityCorpus()
            .Where(sentence => !ChineseProfanity.Censor(sentence))
            .Take(MaxFailuresToPrint)
            .ToArray();

        Assert.True(
            misses.Length == 0,
            "ChineseProfanity.Censor missed generated profanity corpus sentences:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, misses));
    }

    [Fact]
    public static void BenignSafeWordControlSentencesShouldNotBeDetected()
    {
        var normalSentences = new[]
        {
            "公司的幹部正在討論下週的訓練計畫。",
            "這位主管做事很幹練，處理流程也很清楚。",
            "大家今天在操場集合，準備進行體能測驗。",
            "新系統的操作手冊已經放在桌上。",
            "體操選手完成了一套漂亮的動作。",
            "這場賽馬活動吸引很多觀眾。",
            "籃球賽最後一分鐘非常緊張。",
            "決賽時間改到週日下午。",
            "駕駛員提醒乘客繫好安全帶。",
            "車輛行駛到山路時需要放慢速度。",
            "自動駕駛技術還需要更多測試。",
            "糙米飯搭配青菜很適合午餐。",
            "這塊木板表面有點粗糙。",
            "草地上剛澆過水，請不要踩踏。",
            "他正在起草新的會議紀錄。",
            "薰衣草的香味很溫和。",
            "贛江流域最近降雨偏多。",
            "贛州的交通建設正在改善。",
            "船隻慢慢靠近碼頭。",
            "這份資料非常可靠。",
            "請把椅子靠右擺整齊。",
            "滾動條可以拖到頁面底部。",
            "水滾了之後再放麵條。",
            "孩子在草地上翻滾玩耍。",
            "颱風逐漸逼近海岸。",
            "不要逼迫同事接受臨時安排。",
            "這張照片看起來非常逼真。",
            "公園裡有很多鳥類棲息。",
            "候鳥每年秋天往南飛。",
            "鳥瞰城市可以看到河岸線。",
            "早餐吃雞蛋和吐司。",
            "晚餐準備雞肉湯和青菜。",
            "雞農說明今年的飼養情況。",
            "馬路施工期間請改道。",
            "馬拉松比賽清晨開跑。",
            "斑馬線前要禮讓行人。",
            "媽媽今天去市場買菜。",
            "媽祖廟週末有遶境活動。",
            "新娘正在確認婚禮流程。",
            "爸爸帶孩子去圖書館。",
            "姐姐下午要去上課。",
            "空姐正在協助乘客入座。",
            "老爹把工具收進倉庫。",
            "老師說明作業繳交方式。",
            "老子思想是哲學課的主題。",
            "全家便利商店在路口旁邊。",
            "全家人一起到台南旅行。",
            "明太祖是歷史課今天的內容。",
            "死亡統計需要嚴謹的資料來源。",
            "醫師正在說明死因判定流程。",
            "臭豆腐是夜市常見小吃。",
            "臭氧層變化受到科學家關注。",
            "警方已經破案並移送地檢署。",
            "團隊突破了原本的技術限制。",
            "水果腐爛後要盡快清理。",
            "陽光照在水面上十分燦爛。",
            "商品以賤價出售時仍要標示清楚。",
            "日本藝妓文化有很長的歷史。",
            "護理師正在準備注射疫苗。",
            "射擊比賽需要遵守安全規範。",
            "火箭發射前會進行最後檢查。",
            "這招很屌，是稱讚他的創意。",
            "超屌的設計吸引很多人討論。",
            "請記得晚上倒垃圾。",
            "垃圾分類可以減少處理成本。",
            "垃圾車每天固定時間經過。",
            "廢物利用是環保教育的一部分。",
            "低能耗設備可以節省電費。",
            "森林步道今天暫停開放。",
            "林務局公布新的造林計畫。",
            "番茄湯味道很清爽。",
            "番薯是常見的農產品。",
            "他拎著水果走進辦公室。",
            "拎包出門前要確認鑰匙。",
            "三小時後會議才開始。",
            "這次活動分成三小組進行。",
            "靠杯子太近可能會碰倒。",
            "馬桶的蓋子需要更換。",
            "馬祖的廟宇保存得很好。",
            "幹細胞研究需要嚴格審查。",
        };

        var falsePositives = normalSentences
            .Where(ChineseProfanity.Censor)
            .Take(MaxFailuresToPrint)
            .ToArray();

        Assert.True(
            falsePositives.Length == 0,
            "ChineseProfanity.Censor flagged benign safe-word control sentences:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, falsePositives));
    }

    private static string[] ReadProfanityCorpus()
    {
        var path = Path.Combine(
            AppContext.BaseDirectory,
            "ChineseProfanity",
            "Fixtures",
            "profanity_corpus_10k.txt");

        Assert.True(File.Exists(path), "Missing profanity corpus fixture: " + path);

        return File.ReadAllLines(path)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();
    }
}
