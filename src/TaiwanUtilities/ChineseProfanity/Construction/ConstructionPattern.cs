namespace TaiwanUtilities.Internals;

using System;
using System.Collections.Generic;

internal abstract class ConstructionPattern
{
    // Maximum gap between consecutive tokens in a pattern (in characters).
    // This prevents distant tokens from being matched across unrelated text.
    protected const int MAX_TOKEN_GAP = 2;

    internal abstract (int start, int length)? TryMatch(
        List<ProfanityAnalyzer.AnalyzedToken> tokens, int startIndex,
        ReadOnlySpan<char> originalText);

    protected static bool HasCategory(ProfanityAnalyzer.AnalyzedToken token, WordCategory category)
    {
        return !token.IsSafe && (token.Category & category) != 0;
    }

    internal static bool IsBoundary(ReadOnlySpan<char> text, int index)
    {
        if (index < 0 || index >= text.Length)
        {
            return true;
        }

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
        return next.Index - prevEnd <= MAX_TOKEN_GAP;
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
        {
            return true;
        }

        if (gap > MAX_TOKEN_GAP)
        {
            return false;
        }
        // Check that all characters in the gap are whitespace/punctuation
        for (var j = prevEnd; j < next.Index; j++)
        {
            if (!IsBoundary(text, j))
            {
                return false;
            }
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
        {
            return null;
        }

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
            else
            {
                break;
            }
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
        {
            return null;
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
            {
                return null;
            }
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
        {
            return null;
        }

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
        {
            return null;
        }

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
        {
            return null;
        }

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
        {
            return null;
        }

        if (!HasCategory(tokens[i], WordCategory.Slur))
        {
            return null;
        }

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
        {
            return null;
        }

        var startPos = tokens[i].Index;

        // optional: [ProfaneVerb]
        if (HasCategory(tokens[i], WordCategory.ProfaneVerb))
        {
            var prevToken = tokens[i];
            i++;
            if (i >= tokens.Count || !IsAdjacent(prevToken, tokens[i]))
            {
                return null;
            }
        }

        if (i >= tokens.Count || !HasCategory(tokens[i], WordCategory.BodyPart))
        {
            return null;
        }

        var endPos = tokens[i].Index + tokens[i].Length;
        return (startPos, endPos - startPos);
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
        {
            return null;
        }

        var token = tokens[i];
        if (token.IsSafe)
        {
            return null;
        }

        if (!HasCategory(token, WordCategory.ProfaneVerb) &&
            !HasCategory(token, WordCategory.MildExpletive))
        {
            return null;
        }

        // Must be surrounded by boundaries (punctuation/whitespace/start/end)
        // to avoid matching characters embedded in normal compound words (e.g. "熱身操")
        if (!IsBoundary(originalText, token.Index - 1) ||
            !IsBoundary(originalText, token.Index + token.Length))
        {
            return null;
        }

        return (token.Index, token.Length);
    }
}
