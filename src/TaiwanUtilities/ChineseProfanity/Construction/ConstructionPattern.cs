namespace TaiwanUtilities.Internals;

using System;
using System.Collections.Generic;

internal abstract class ConstructionPattern
{
    // Maximum gap between consecutive tokens in a pattern (in characters).
    // This prevents distant tokens from being matched across unrelated text.
    protected const int MaxTokenGap = 2;

    internal abstract (int start, int length)? TryMatch(
        List<ProfanityAnalyzer.AnalyzedToken> tokens, int startIndex,
        ReadOnlySpan<char> originalText);

    protected static bool HasCategory(ProfanityAnalyzer.AnalyzedToken token, WordCategory category)
    {
        return !token.IsSafe && (token.Category & category) != 0;
    }

    protected static bool IsBoundary(ReadOnlySpan<char> text, int index)
    {
        if (index < 0 || index >= text.Length)
            return true;
        var ch = text[index];
        return char.IsWhiteSpace(ch) || IsPunctuationChar(ch);
    }

    private static bool IsPunctuationChar(char ch)
    {
        return (ch >= '\u3000' && ch <= '\u303F') ||
               (ch >= '\uFF00' && ch <= '\uFF60') ||
               (ch >= '!' && ch <= '/') ||
               (ch >= ':' && ch <= '@') ||
               (ch >= '[' && ch <= '`') ||
               (ch >= '{' && ch <= '~');
    }

    /// <summary>
    /// Check if two consecutive tokens are close enough in the original text.
    /// </summary>
    protected static bool IsAdjacent(ProfanityAnalyzer.AnalyzedToken prev, ProfanityAnalyzer.AnalyzedToken next)
    {
        var prevEnd = prev.Index + prev.Length;
        return next.Index - prevEnd <= MaxTokenGap;
    }

    /// <summary>
    /// Strict adjacency: tokens must be directly adjacent (gap = 0) or separated
    /// only by whitespace/punctuation. No unknown CJK characters allowed between them.
    /// </summary>
    protected static bool IsStrictlyAdjacent(
        ProfanityAnalyzer.AnalyzedToken prev, ProfanityAnalyzer.AnalyzedToken next,
        ReadOnlySpan<char> text)
    {
        var prevEnd = prev.Index + prev.Length;
        var gap = next.Index - prevEnd;
        if (gap <= 0)
            return true;
        if (gap > MaxTokenGap)
            return false;
        // Check that all characters in the gap are whitespace/punctuation
        for (var j = prevEnd; j < next.Index; j++)
        {
            if (!IsBoundary(text, j))
                return false;
        }
        return true;
    }
}

/// <summary>
/// [ProfaneVerb] [Pronoun]? [Kinship]? [Particle]?
/// Requires at least one of [Pronoun] or [Kinship] after the verb.
/// "幹你娘" = full match, "幹你" = verb+pronoun, "幹你香蕉" = catches "幹你"
/// </summary>
internal sealed class VerbKinshipPattern : ConstructionPattern
{
    internal override (int start, int length)? TryMatch(
        List<ProfanityAnalyzer.AnalyzedToken> tokens, int startIndex,
        ReadOnlySpan<char> originalText)
    {
        var i = startIndex;
        if (i >= tokens.Count || !HasCategory(tokens[i], WordCategory.ProfaneVerb))
            return null;

        var startPos = tokens[i].Index;
        var endPos = tokens[i].Index + tokens[i].Length;
        var prevToken = tokens[i];
        i++;

        // optional: [PejorativePrefix]? (死、破) e.g. 幹死你娘
        if (i < tokens.Count && HasCategory(tokens[i], WordCategory.PejorativePrefix) && IsAdjacent(prevToken, tokens[i]))
        {
            endPos = tokens[i].Index + tokens[i].Length;
            prevToken = tokens[i];
            i++;
        }

        // optional: [Pronoun]{1,2}
        var hasPronoun = false;
        for (var p = 0; p < 2 && i < tokens.Count; p++)
        {
            if (HasCategory(tokens[i], WordCategory.Pronoun) && IsAdjacent(prevToken, tokens[i]))
            {
                hasPronoun = true;
                endPos = tokens[i].Index + tokens[i].Length;
                prevToken = tokens[i];
                i++;
            }
            else break;
        }

        // optional: [Kinship] (but at least one of Pronoun/Kinship must be present)
        var hasKinship = false;
        if (i < tokens.Count && HasCategory(tokens[i], WordCategory.Kinship) && IsAdjacent(prevToken, tokens[i]))
        {
            hasKinship = true;
            endPos = tokens[i].Index + tokens[i].Length;
            prevToken = tokens[i];
            i++;
        }

        // Must have at least Pronoun or Kinship
        if (!hasPronoun && !hasKinship)
            return null;

        // optional: [Particle]
        if (i < tokens.Count && HasCategory(tokens[i], WordCategory.Particle) && IsAdjacent(prevToken, tokens[i]))
        {
            endPos = tokens[i].Index + tokens[i].Length;
        }

        return (startPos, endPos - startPos);
    }
}

/// <summary>
/// [Pronoun]? [Kinship] [Particle]
/// </summary>
internal sealed class ExclamatoryPattern : ConstructionPattern
{
    internal override (int start, int length)? TryMatch(
        List<ProfanityAnalyzer.AnalyzedToken> tokens, int startIndex,
        ReadOnlySpan<char> originalText)
    {
        var i = startIndex;

        var startPos = tokens[i].Index;
        var endPos = startPos;
        ProfanityAnalyzer.AnalyzedToken prevToken;

        // optional: [Pronoun]
        if (HasCategory(tokens[i], WordCategory.Pronoun))
        {
            endPos = tokens[i].Index + tokens[i].Length;
            prevToken = tokens[i];
            i++;

            // required: [Kinship] — must be adjacent to pronoun
            if (i >= tokens.Count || !HasCategory(tokens[i], WordCategory.Kinship) || !IsAdjacent(prevToken, tokens[i]))
                return null;
        }
        else if (HasCategory(tokens[i], WordCategory.Kinship))
        {
            // Kinship without pronoun (e.g. "媽的")
        }
        else
        {
            return null;
        }

        endPos = tokens[i].Index + tokens[i].Length;
        prevToken = tokens[i];
        i++;

        // required: [Particle] — must be strictly adjacent (no CJK gap)
        // This prevents "馬桶的" from matching as [Kinship(馬)][Particle(的)]
        if (i >= tokens.Count || !HasCategory(tokens[i], WordCategory.Particle) ||
            !IsStrictlyAdjacent(prevToken, tokens[i], originalText))
            return null;

        endPos = tokens[i].Index + tokens[i].Length;

        return (startPos, endPos - startPos);
    }
}

/// <summary>
/// [PejorativePrefix]+ [Slur/BodyPart/Kinship]
/// </summary>
internal sealed class PrefixedSlurPattern : ConstructionPattern
{
    internal override (int start, int length)? TryMatch(
        List<ProfanityAnalyzer.AnalyzedToken> tokens, int startIndex,
        ReadOnlySpan<char> originalText)
    {
        var i = startIndex;
        if (i >= tokens.Count || !HasCategory(tokens[i], WordCategory.PejorativePrefix))
            return null;

        var startPos = tokens[i].Index;
        var endPos = tokens[i].Index + tokens[i].Length;
        var prevToken = tokens[i];
        i++;

        // additional optional prefixes
        while (i < tokens.Count && HasCategory(tokens[i], WordCategory.PejorativePrefix) && IsAdjacent(prevToken, tokens[i]))
        {
            endPos = tokens[i].Index + tokens[i].Length;
            prevToken = tokens[i];
            i++;
        }

        // required: [Slur] or [BodyPart] or [Kinship]
        if (i >= tokens.Count || !IsAdjacent(prevToken, tokens[i]))
            return null;

        if (HasCategory(tokens[i], WordCategory.Slur) ||
            HasCategory(tokens[i], WordCategory.BodyPart) ||
            HasCategory(tokens[i], WordCategory.Kinship))
        {
            endPos = tokens[i].Index + tokens[i].Length;
            return (startPos, endPos - startPos);
        }

        return null;
    }
}

/// <summary>
/// [Slur]
/// </summary>
internal sealed class StandaloneSlurPattern : ConstructionPattern
{
    internal override (int start, int length)? TryMatch(
        List<ProfanityAnalyzer.AnalyzedToken> tokens, int startIndex,
        ReadOnlySpan<char> originalText)
    {
        var i = startIndex;
        if (i >= tokens.Count || tokens[i].IsSafe)
            return null;

        if (!HasCategory(tokens[i], WordCategory.Slur))
            return null;

        return (tokens[i].Index, tokens[i].Length);
    }
}

/// <summary>
/// [ProfaneVerb]? [BodyPart]
/// </summary>
internal sealed class BodyPartPattern : ConstructionPattern
{
    internal override (int start, int length)? TryMatch(
        List<ProfanityAnalyzer.AnalyzedToken> tokens, int startIndex,
        ReadOnlySpan<char> originalText)
    {
        var i = startIndex;
        if (i >= tokens.Count)
            return null;

        var startPos = tokens[i].Index;

        // optional: [ProfaneVerb]
        if (HasCategory(tokens[i], WordCategory.ProfaneVerb))
        {
            var prevToken = tokens[i];
            i++;
            if (i >= tokens.Count || !IsAdjacent(prevToken, tokens[i]))
                return null;
        }

        if (i >= tokens.Count || !HasCategory(tokens[i], WordCategory.BodyPart))
            return null;

        var endPos = tokens[i].Index + tokens[i].Length;
        return (startPos, endPos - startPos);
    }
}

/// <summary>
/// Compound verb phrases — fixed multi-character expressions.
/// </summary>
internal sealed class CompoundVerbPattern : ConstructionPattern
{
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
            "看三小", "看三洨", "殺三小", "殺三洨", "沙三小", "莎三小",
            "你老子",

            // 閩南話特有複合表達
            "賽拎娘", "駛拎娘", "幹拎娘", "操拎娘", "肏拎娘",
            "賽拎老母", "駛拎老母",
            "賽你娘", "駛你娘",
            "賽你媽", "駛你媽",
            "拎北", "拎爸", "林北", "林爸",  // 閩南話「你爸」= 老子、我
            "欠駛",
            "賽三小", "駛三小",

            // 射 的性相關固定詞組（「射在」不加，避免誤殺「射在靶上」）
            "射精", "顏射", "內射", "外射", "體內射精", "體外射精",
            "射了一臉", "射在臉上", "射在身上", "射在嘴裡",
        };
        foreach (var w in words)
        {
            trie.Add(w);
        }
        return trie;
    }

    internal override (int start, int length)? TryMatch(
        List<ProfanityAnalyzer.AnalyzedToken> tokens, int startIndex,
        ReadOnlySpan<char> originalText)
    {
        return null;
    }

    internal static int TryMatchCompound(ReadOnlySpan<char> text, int index)
    {
        return s_compounds.Value.LongestMatch(text, index);
    }
}

/// <summary>
/// Isolated expletive surrounded by punctuation/whitespace/boundaries.
/// </summary>
internal sealed class IsolatedExpletivePattern : ConstructionPattern
{
    internal override (int start, int length)? TryMatch(
        List<ProfanityAnalyzer.AnalyzedToken> tokens, int startIndex,
        ReadOnlySpan<char> originalText)
    {
        var i = startIndex;
        if (i >= tokens.Count)
            return null;

        var token = tokens[i];
        if (token.IsSafe)
            return null;

        if (!HasCategory(token, WordCategory.ProfaneVerb) &&
            !HasCategory(token, WordCategory.MildExpletive))
            return null;

        // Must be surrounded by boundaries (punctuation/whitespace/start/end)
        // to avoid matching characters embedded in normal compound words (e.g. "熱身操")
        if (!IsBoundary(originalText, token.Index - 1) ||
            !IsBoundary(originalText, token.Index + token.Length))
            return null;

        return (token.Index, token.Length);
    }
}
