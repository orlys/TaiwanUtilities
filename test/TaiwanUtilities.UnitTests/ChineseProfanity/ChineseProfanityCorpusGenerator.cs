namespace TaiwanUtilities.UnitTests;

using System;
using System.Collections.Generic;

internal static class ChineseProfanityCorpusGenerator
{
    internal const int TargetCorpusSize = 10_000;

    private static readonly string[] ProfaneVerbs =
    [
        "肏", "操", "幹", "姦", "草", "淦", "贛", "耖", "襙", "鄵", "糙", "駛", "賽",
    ];

    private static readonly string[] Pronouns =
    [
        "你", "妳", "祢", "汝", "他", "她", "它", "牠", "祂", "恁", "琳", "您", "林", "拎",
    ];

    private static readonly string[] KinshipTerms =
    [
        "娘親", "親娘", "奶奶", "全家", "祖宗", "姑媽", "姑奶奶",
        "太祖", "開基祖", "阿公", "阿嬤", "啊公", "啊嬤",
        "老母", "老師", "老子",
        "媽", "娘", "爸", "爹", "馬", "妹", "姊", "姐",
    ];

    private static readonly string[] BodyParts =
    [
        "雞巴", "機掰", "雞掰", "鮑魚", "屁眼", "龜頭",
        "屄", "逼", "屌", "鳥", "B",
    ];

    private static readonly string[] PejorativePrefixes =
    [
        "死", "臭", "破", "賤", "爛",
    ];

    private static readonly string[] Slurs =
    [
        "畜生", "畜牲", "廢物", "廢柴", "廢人", "垃圾", "人渣",
        "婊子", "賤人", "賤貨", "蕩婦", "淫婦", "娼妓",
        "白痴", "白癡", "白目", "白爛", "智障", "智缺", "腦殘", "腦弱", "腦包",
        "低能兒", "低能", "弱智", "弱雞", "北七", "北柒", "北爛",
        "笨蛋", "混蛋", "混帳", "王八", "王八蛋", "王八羔子", "王八犢子", "傻瓜", "傻子",
        "傻逼", "傻屄", "傻屌", "窩囊廢", "窩囊",
        "敗類", "低端", "孬種", "孬貨", "孬包", "孬孬", "慫包",
        "俗辣", "乞丐", "要飯", "狗崽子", "兔崽子",
        "龜公", "龜狗", "龜兒子", "龜孫子", "龜崽子",
        "咖小", "三小", "三洨",
        "社會敗類",
        "狗娘養的", "狗東西", "狗種", "狗魚", "狗碎",
        "雜種", "雜魚", "雜碎",
        "妓女",
        "騷包", "騷貨",
        "懶趴", "懶叫", "覽趴", "覽叫",
        "啟智", "乞智",
        "淫蕩", "淫水", "淫紋", "淫窟",
        "青番", "生番", "番子", "番仔",
        "慫樣", "鳥蛋", "卵蛋", "滾蛋",
        "賤畜", "賤狗", "賤種", "賤民",
        "破麻", "破狗", "破雞", "破妓", "破鞋", "破腦", "破格",
        "破屄", "破逼", "破鮑魚",
        "爛人", "爛貨", "爛屄", "爛逼", "爛鮑魚",
        "死傻逼", "死傻屄", "死傻屌",
        "臭逼", "臭屄", "臭屌",
        "傻B",
        "87",
    ];

    private static readonly string[] Particles =
    [
        "的", "勒", "啊", "了個",
    ];

    private static readonly string[] MildExpletives =
    [
        "靠", "滾",
    ];

    private static readonly string[] Compounds =
    [
        "打手槍", "賣淫", "賣逼", "賣屄", "賣身", "賣B",
        "強姦", "強上", "姦屍", "手淫", "姦淫",
        "性愛", "做愛", "口交", "乳交", "足交", "肛交", "援交", "性交",
        "自慰", "站壁", "破處", "陽痿",
        "去死", "放屁", "天殺的", "下三濫",
        "吃屎", "吃大便", "喝尿", "食屎", "食大便",
        "肉便器", "母貓", "母狗", "公狗", "小母狗",
        "狗屎", "狗崽",
        "幹砲", "幹炮", "淦砲", "淦炮", "贛砲", "贛炮",
        "欠幹", "欠淦", "欠贛", "破幹", "破淦", "破贛", "下幹", "下淦", "下贛", "狗幹",
        "操蛋", "肏蛋", "草蛋", "糙蛋",
        "媽逼", "媽屄", "馬逼", "馬屄", "嬤逼", "嬤屄",
        "媽了個逼", "媽了個屄", "馬了個逼",
        "靠北", "靠爸", "靠杯", "靠盃", "靠邀", "靠腰", "靠么", "靠夭", "靠參",
        "哭北", "哭爸", "哭杯",
        "你他媽", "我他媽",
        "看三小", "看三洨", "殺三小", "殺三洨", "沙三小", "莎三小",
        "你老子",
        "賽拎娘", "駛拎娘", "幹拎娘", "操拎娘", "肏拎娘",
        "賽拎老母", "駛拎老母",
        "賽你娘", "駛你娘",
        "賽你媽", "駛你媽",
        "拎北", "拎爸", "林北", "林爸",
        "欠駛",
        "賽三小", "駛三小",
        "哭夭", "哭枵",
        "機掰人", "雞掰人",
        "膣屄",
        "卒仔",
        "射精", "顏射", "內射", "外射", "體內射精", "體外射精",
        "射了一臉", "射在臉上", "射在身上", "射在嘴裡",
    ];

    private static readonly string[] SafeWordNeighbors =
    [
        "操場", "操作", "賽馬", "比賽", "駕駛", "草地", "靠近", "滾動",
        "鳥類", "雞蛋", "馬路", "媽媽", "爸爸", "森林", "垃圾分類", "廢物利用",
    ];

    private static readonly HashSet<string> ExcludedSafeUnits = new(StringComparer.Ordinal)
    {
        // This is mechanically possible as ProfaneVerb(賽)+Kinship(馬), but the lexicon
        // intentionally treats it as a safe word for horse-racing contexts.
        "賽馬",
    };

    private static readonly string[] ContextFrames =
    [
        "{0}，走開，別再吵了。",
        "你剛剛那句話真的很{0}",
        "我本來在看{1}，但你這個{0}很煩。",
        "{0}。",
        "路人甲回：「{0}」然後把手機收起來。",
        "客服紀錄先寫{1}，下一行卻是「{0}」。",
        "他在{1}旁邊直接打{0}，沒有多留空格。",
        "前面還在聊{1}後面突然冒出{0}這句。",
        "公告說{1}正常，留言區卻補了一句{0}。",
        "隊友連續輸入{0}、{1}、再補一個表情。",
        "文章中段提到{1}，接著插入「{0}」才換段。",
        "截圖左邊是{1}，右邊有人寫：{0}！",
        "主持人剛講完{1}，台下就有人喊{0}。",
        "他把句子拆成兩段：先說{1}；再說{0}。",
        "留言沒有空白，前綴是{1}後綴是{0}結束。",
        "短訊內容只有兩句，第一句{0}，第二句提到{1}。",
        "他說「先別管{1}」，但下一秒罵出{0}。",
        "整段文字像流水帳，從{1}寫到{0}再寫到會議。",
        "有人把{0}放在句首，後面接著談{1}。",
        "這不是{1}的問題，而是有人直接說{0}。",
    ];

    internal static IReadOnlyList<string> GenerateProfanityCorpus()
    {
        var sentences = new List<string>(TargetCorpusSize);
        var seen = new HashSet<string>();
        var units = GenerateProfanityUnits();

        var frameCount = ContextFrames.Length;
        for (var i = 0; sentences.Count < TargetCorpusSize; i++)
        {
            var frameIndex = i % frameCount;
            var unitRound = i / frameCount;
            var unitIndex = (unitRound + (frameIndex * 997)) % units.Count;
            var unit = units[unitIndex];
            var safe = SafeWordNeighbors[(unitRound + frameIndex) % SafeWordNeighbors.Length];

            Add(string.Format(ContextFrames[frameIndex], unit, safe));
        }

        return sentences;

        void Add(string sentence)
        {
            if (seen.Add(sentence))
            {
                sentences.Add(sentence);
            }
        }
    }

    private static IReadOnlyList<string> GenerateProfanityUnits()
    {
        var units = new List<string>();
        var seen = new HashSet<string>();

        AddRange(Compounds);
        AddRange(GenerateBoundaryGapCompounds());
        AddRange(GenerateBoundaryGapConstructions());
        AddRange(Slurs);

        foreach (var bodyPart in BodyParts)
        {
            Add(bodyPart);
            foreach (var verb in ProfaneVerbs)
            {
                Add(verb + bodyPart);
            }
        }

        foreach (var verb in ProfaneVerbs)
        {
            foreach (var pronoun in Pronouns)
            {
                Add(verb + pronoun);
                foreach (var kinship in KinshipTerms)
                {
                    Add(verb + pronoun + kinship);
                }
            }

            foreach (var kinship in KinshipTerms)
            {
                Add(verb + kinship);
            }
        }

        foreach (var verb in ProfaneVerbs)
        {
            foreach (var prefix in PejorativePrefixes)
            {
                foreach (var pronoun in Pronouns)
                {
                    Add(verb + prefix + pronoun);
                    foreach (var kinship in KinshipTerms)
                    {
                        Add(verb + prefix + pronoun + kinship);
                    }
                }
            }
        }

        foreach (var kinship in KinshipTerms)
        {
            foreach (var particle in Particles)
            {
                Add(kinship + particle);
                foreach (var pronoun in Pronouns)
                {
                    Add(pronoun + kinship + particle);
                }
            }
        }

        foreach (var prefix in PejorativePrefixes)
        {
            foreach (var slur in Slurs)
            {
                Add(prefix + slur);
            }

            foreach (var bodyPart in BodyParts)
            {
                Add(prefix + bodyPart);
            }

            foreach (var kinship in KinshipTerms)
            {
                Add(prefix + kinship);
            }
        }

        foreach (var expletive in MildExpletives)
        {
            Add("，" + expletive + "！");
            Add(" " + expletive + " ");
            Add("「" + expletive + "」");
        }

        foreach (var verb in ProfaneVerbs)
        {
            Add("，" + verb + "！");
            Add(" " + verb + " ");
            Add("「" + verb + "」");
        }

        return units;

        void AddRange(IEnumerable<string> values)
        {
            foreach (var value in values)
            {
                Add(value);
            }
        }

        void Add(string value)
        {
            if (!ExcludedSafeUnits.Contains(value) && seen.Add(value))
            {
                units.Add(value);
            }
        }
    }

    private static IEnumerable<string> GenerateBoundaryGapCompounds()
    {
        foreach (var compound in Compounds)
        {
            if (compound.Length >= 2)
            {
                yield return compound[0] + "~" + compound[1..];
            }
        }
    }

    private static IEnumerable<string> GenerateBoundaryGapConstructions()
    {
        var verbs = new[] { "幹", "操", "肏", "賽", "駛" };
        var pronouns = new[] { "你", "妳", "恁", "拎" };
        var kinships = new[] { "娘", "媽", "老母" };

        foreach (var verb in verbs)
        {
            foreach (var pronoun in pronouns)
            {
                yield return verb + "." + pronoun;
                foreach (var kinship in kinships)
                {
                    yield return verb + "." + pronoun + "." + kinship;
                    yield return verb + " " + pronoun + " " + kinship;
                }
            }
        }
    }
}
