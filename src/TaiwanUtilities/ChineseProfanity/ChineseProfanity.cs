namespace TaiwanUtilities;

using System;
using System.Text;
using TaiwanUtilities.Internals;

/// <summary>
/// 中文髒話
/// </summary>
public static class ChineseProfanity
{
    /// <summary>
    /// 審查是否包含髒話
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public static bool Censor(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var matches = ProfanityAnalyzer.Analyze(text.AsSpan());
        return matches.Count > 0;
    }

    /// <summary>
    /// 將文字中的髒話以 <see cref="ReplacementCharacters.WhiteCircle"/> 替換
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public static string? Replace(string? text)
    {
        return Replace(text, ReplacementCharacters.WhiteCircle);
    }

    /// <summary>
    /// 將文字中的髒話以指定字元替換
    /// </summary>
    /// <param name="text"></param>
    /// <param name="replaceWith">要替換髒話的字元，預設為 <see cref="ReplacementCharacters.WhiteCircle"/></param>
    /// <returns></returns>
    public static string? Replace(string? text, char replaceWith = ReplacementCharacters.WhiteCircle)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var matches = ProfanityAnalyzer.Analyze(text.AsSpan());
        if (matches.Count == 0)
            return text;

        var sb = new StringBuilder(text);

        // 從後往前替換，避免 index 位移
        for (var i = matches.Count - 1; i >= 0; i--)
        {
            var (index, length) = matches[i];
            sb.Remove(index, length);
            sb.Insert(index, new string(replaceWith, length));
        }

        return sb.ToString();
    }
}
