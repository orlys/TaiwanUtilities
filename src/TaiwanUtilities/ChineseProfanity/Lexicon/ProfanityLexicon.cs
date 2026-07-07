namespace TaiwanUtilities.Internals;

using System;
using System.Collections.Generic;

internal static class ProfanityLexicon
{
    private static readonly Lazy<TrieDictionary<WordCategory>> s_lexicon = new(Build);

    private static TrieDictionary<WordCategory> Build()
    {
        var dict = new TrieDictionary<WordCategory>();

        // 髒話動詞
        AddAll(dict, WordCategory.ProfaneVerb,
            "肏", "操", "幹", "姦", "草", "淦", "贛", "耖", "襙", "鄵", "糙", "駛", "賽");

        // 代詞（含閩南話代詞）
        AddAll(dict, WordCategory.Pronoun,
            "你", "妳", "祢", "汝", "他", "她", "它", "牠", "祂", "恁", "琳", "您", "林",
            "拎"); // 閩南話「你」，如「賽拎娘」

        // 親屬詞（多字詞優先）
        AddAll(dict, WordCategory.Kinship,
            "娘親", "親娘", "奶奶", "全家", "祖宗", "姑媽", "姑奶奶",
            "太祖", "開基祖", "阿公", "阿嬤", "啊公", "啊嬤",
            "老母", "老師", "老子",
            "媽", "娘", "爸", "爹", "馬", "妹", "姊", "姐");

        // 身體詞（多字詞優先）
        AddAll(dict, WordCategory.BodyPart,
            "雞巴", "機掰", "雞掰", "鮑魚", "屁眼", "龜頭",
            "屄", "逼", "屌", "鳥", "B");

        // 貶義前綴
        AddAll(dict, WordCategory.PejorativePrefix,
            "死", "臭", "破", "賤", "爛");

        // 貶義名詞（獨立成詞的罵人話）— 多字詞優先
        AddAll(dict, WordCategory.Slur,
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
            "淫蕩", "淫婦", "淫水", "淫紋", "淫窟",
            "青番", "生番", "番子", "番仔",
            "慫樣", "鳥蛋", "卵蛋", "滾蛋",
            "賤畜", "賤狗", "賤種", "賤民",
            // 破+X 固定搭配
            "破麻", "破狗", "破雞", "破妓", "破鞋", "破腦", "破格",
            "破屄", "破逼", "破鮑魚",
            // 爛+X 固定搭配
            "爛人", "爛貨", "爛屄", "爛逼", "爛鮑魚",
            // 死+X 固定搭配
            "死傻逼", "死傻屄", "死傻屌",
            // 臭+X 固定搭配
            "臭逼", "臭屄", "臭屌",
            // 傻B (B as euphemism for 屄)
            "傻B",
            // 數字諧音
            "87");

        // 語助詞
        AddAll(dict, WordCategory.Particle,
            "的", "勒", "啊", "了個");

        // 輕度感嘆
        AddAll(dict, WordCategory.MildExpletive,
            "靠", "滾");

        return dict;
    }

    private static void AddAll(TrieDictionary<WordCategory> dict, WordCategory category, params string[] words)
    {
        foreach (var word in words)
        {
            dict.TryAdd(ProfanityNormalizer.NormalizeToString(word), category);
        }
    }

    /// <summary>
    /// 嘗試在 text[index..] 位置識別一個已知詞彙（正向最大匹配）。
    /// 回傳 (類別, 長度)，找不到則回傳 (None, 0)。
    /// </summary>
    internal static (WordCategory category, int length) Classify(ReadOnlySpan<char> text, int index)
    {
        if (s_lexicon.Value.LongestMatch(text, index, out var category, out var length))
        {
            return (category, length);
        }

        return (WordCategory.None, 0);
    }
}
