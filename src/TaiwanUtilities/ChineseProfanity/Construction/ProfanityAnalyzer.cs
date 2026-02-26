namespace TaiwanUtilities.Internals;

using System;
using System.Collections.Generic;

internal static class ProfanityAnalyzer
{
    internal readonly struct AnalyzedToken
    {
        internal int Index { get; init; }
        internal int Length { get; init; }
        internal WordCategory Category { get; init; }
        internal bool IsSafe { get; init; }
    }

    private static readonly ConstructionPattern[] s_patterns =
    [
        new VerbKinshipPattern(),
        new ExclamatoryPattern(),
        new PrefixedSlurPattern(),
        new StandaloneSlurPattern(),
        new BodyPartPattern(),
        new IsolatedExpletivePattern(),
    ];

    /// <summary>
    /// 掃描文字，回傳所有髒話匹配的範圍。
    /// </summary>
    internal static List<(int index, int length)> Analyze(ReadOnlySpan<char> text)
    {
        var matches = new List<(int index, int length)>();

        // Phase 0: compound expression matching (fixed phrases)
        var compoundCovered = new HashSet<int>();
        for (var i = 0; i < text.Length; i++)
        {
            if (compoundCovered.Contains(i))
            {
                continue;
            }

            // Try safe word first
            var safeLen = SafeWordDictionary.TryMatchSafeWord(text, i);
            if (safeLen > 0)
            {
                // Mark safe range — skip compound matching here
                for (var s = i; s < i + safeLen; s++)
                {
                    compoundCovered.Add(s);
                }

                i += safeLen - 1;
                continue;
            }

            var compLen = CompoundVerbPattern.TryMatchCompound(text, i);
            if (compLen > 0)
            {
                matches.Add((i, compLen));
                for (var c = i; c < i + compLen; c++)
                {
                    compoundCovered.Add(c);
                }

                i += compLen - 1;
            }
        }

        // Phase 1-2: Tokenize (safe word filtering + lexicon classification)
        var tokens = Tokenize(text, compoundCovered);

        // Phase 3: Apply construction patterns
        var patternMatches = ApplyPatterns(tokens, text);

        // Merge compound and pattern matches, avoiding overlaps
        foreach (var m in patternMatches)
        {
            if (!Overlaps(matches, m))
            {
                matches.Add(m);
            }
        }

        // Sort by position
        matches.Sort((a, b) => a.index.CompareTo(b.index));

        return matches;
    }

    private static List<AnalyzedToken> Tokenize(ReadOnlySpan<char> text, HashSet<int> covered)
    {
        var tokens = new List<AnalyzedToken>();

        for (var i = 0; i < text.Length; i++)
        {
            // Skip already covered positions (compound matches)
            if (covered.Contains(i))
            {
                continue;
            }

            // Skip whitespace and punctuation
            var ch = text[i];
            if (char.IsWhiteSpace(ch) || IsPunctuation(ch))
            {
                continue;
            }

            // Layer 1: Safe word check
            var safeLen = SafeWordDictionary.TryMatchSafeWord(text, i);
            if (safeLen > 0)
            {
                tokens.Add(new AnalyzedToken
                {
                    Index = i,
                    Length = safeLen,
                    Category = WordCategory.None,
                    IsSafe = true,
                });
                i += safeLen - 1;
                continue;
            }

            // Layer 2: Lexicon classification
            var (category, length) = ProfanityLexicon.Classify(text, i);
            if (category != WordCategory.None && length > 0)
            {
                tokens.Add(new AnalyzedToken
                {
                    Index = i,
                    Length = length,
                    Category = category,
                    IsSafe = false,
                });
                i += length - 1;
                continue;
            }

            // Unknown character — skip (including unrecognized ASCII)
        }

        return tokens;
    }

    private static List<(int index, int length)> ApplyPatterns(List<AnalyzedToken> tokens, ReadOnlySpan<char> originalText)
    {
        var matches = new List<(int index, int length)>();

        for (var i = 0; i < tokens.Count; i++)
        {
            if (tokens[i].IsSafe)
            {
                continue;
            }

            (int start, int length)? bestMatch = null;

            foreach (var pattern in s_patterns)
            {
                var match = pattern.TryMatch(tokens, i, originalText);
                if (match.HasValue)
                {
                    if (!bestMatch.HasValue || match.Value.length > bestMatch.Value.length)
                    {
                        bestMatch = match;
                    }
                }
            }

            if (bestMatch.HasValue)
            {
                matches.Add(bestMatch.Value);

                // Advance past matched tokens
                var endPos = bestMatch.Value.start + bestMatch.Value.length;
                while (i + 1 < tokens.Count && tokens[i + 1].Index < endPos)
                {
                    i++;
                }
            }
        }

        return matches;
    }

    private static bool Overlaps(List<(int index, int length)> existing, (int index, int length) candidate)
    {
        var cStart = candidate.index;
        var cEnd = cStart + candidate.length;

        for (var i = 0; i < existing.Count; i++)
        {
            var eStart = existing[i].index;
            var eEnd = eStart + existing[i].length;

            if (cStart < eEnd && cEnd > eStart)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPunctuation(char ch)
    {
        // CJK punctuation range + fullwidth forms + common ASCII punctuation
        return (ch >= '\u3000' && ch <= '\u303F') ||
               (ch >= '\uFF00' && ch <= '\uFF60') ||
               (ch >= '!' && ch <= '/') ||
               (ch >= ':' && ch <= '@') ||
               (ch >= '[' && ch <= '`') ||
               (ch >= '{' && ch <= '~');
    }

    private static bool IsAsciiAlphanumeric(char ch)
    {
        return (ch >= '0' && ch <= '9') ||
               (ch >= 'a' && ch <= 'z') ||
               (ch >= 'A' && ch <= 'Z');
    }
}