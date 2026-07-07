namespace TaiwanUtilities.Internals;

using System;

internal static class CompoundMatcher
{
    private const int MAX_BOUNDARY_GAP = 2;

    private static readonly Lazy<Trie> s_compounds = new(BuildCompounds);

    private static Trie BuildCompounds()
    {
        var trie = new Trie();
        var words = new[]
        {
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
            // 「他媽」副詞用法（無「的」），如「你他媽完全沒看懂」；固定搭配無誤殺同形
            "你他媽", "我他媽",
            "看三小", "看三洨", "殺三小", "殺三洨", "沙三小", "莎三小",
            "你老子",

            // 閩南話特有複合表達
            "賽拎娘", "駛拎娘", "幹拎娘", "操拎娘", "肏拎娘",
            "賽拎老母", "駛拎老母",
            "賽你娘", "駛你娘",
            "賽你媽", "駛你媽",
            "拎北", "拎爸", "林北", "林爸",
            "欠駛",
            "賽三小", "駛三小",
            // 哭夭/哭枵（khàu-iau，表抱怨/該死）；哭枵 為本字，哭夭 為通行寫法
            "哭夭", "哭枵",
            // 機掰/雞掰 衍生（完整遮罩；單獨機掰亦可由 BodyPart 命中）
            "機掰人", "雞掰人",
            // 膣屄（tsi-bai 本字，機掰的本字寫法）
            "膣屄",
            // 卒仔（tsut-á，膽小鬼）；與 卒業/士卒/小卒/過河卒 不衝突（需 卒+仔 相連）
            "卒仔",

            // 射 的性相關固定詞組（「射在」不加，避免誤殺「射在靶上」）
            "射精", "顏射", "內射", "外射", "體內射精", "體外射精",
            "射了一臉", "射在臉上", "射在身上", "射在嘴裡",
        };

        foreach (var word in words)
        {
            trie.Add(ProfanityNormalizer.NormalizeToString(word));
        }

        return trie;
    }

    internal static int TryMatchCompound(ReadOnlySpan<char> text, int index)
    {
        var trie = s_compounds.Value;
        var first = trie.GetChildNode(trie.Root, text[index]);
        if (first is null)
        {
            return 0;
        }

        var bestLength = 0;

        Walk(trie, text, index, first, index, 1, usedBoundaryGap: false, usedSentenceBoundaryGap: false, ref bestLength);
        return bestLength;
    }

    private static void Walk(
        Trie trie,
        ReadOnlySpan<char> text,
        int startIndex,
        CharTrieNode node,
        int position,
        int realCharCount,
        bool usedBoundaryGap,
        bool usedSentenceBoundaryGap,
        ref int bestLength)
    {
        if (node.IsTerminal &&
            realCharCount >= 2 &&
            !IsEmbeddedTwoCharacterGapMatch(text, position, realCharCount, usedBoundaryGap, usedSentenceBoundaryGap))
        {
            bestLength = Math.Max(bestLength, position - startIndex + 1);
        }

        var nextPosition = position + 1;
        for (var gap = 0; gap <= MAX_BOUNDARY_GAP && nextPosition < text.Length; gap++, nextPosition++)
        {
            var next = trie.GetChildNode(node, text[nextPosition]);
            if (next is not null)
            {
                Walk(
                    trie,
                    text,
                    startIndex,
                    next,
                    nextPosition,
                    realCharCount + 1,
                    usedBoundaryGap || gap > 0,
                    usedSentenceBoundaryGap || ContainsSentenceBoundary(text, position + 1, nextPosition),
                    ref bestLength);
            }

            if (!ConstructionPattern.IsBoundary(text, nextPosition))
            {
                break;
            }
        }
    }

    private static bool IsEmbeddedTwoCharacterGapMatch(
        ReadOnlySpan<char> text,
        int position,
        int realCharCount,
        bool usedBoundaryGap,
        bool usedSentenceBoundaryGap)
    {
        return usedBoundaryGap &&
               usedSentenceBoundaryGap &&
               realCharCount == 2 &&
               position + 1 < text.Length &&
               !ConstructionPattern.IsBoundary(text, position + 1);
    }

    private static bool ContainsSentenceBoundary(ReadOnlySpan<char> text, int start, int end)
    {
        for (var i = start; i < end; i++)
        {
            if (IsSentenceBoundary(text[i]))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsSentenceBoundary(char ch)
    {
        return ch == '。' ||
               ch == '．' ||
               ch == '！' ||
               ch == '？' ||
               ch == '!' ||
               ch == '?';
    }
}
