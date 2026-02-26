// transpile from https://github.com/enylin/taiwan-id-validator/commit/6a673c608e5ec2287a58457a6dc2317f7a03f338
// license: MIT

namespace TaiwanUtilities;

using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

/// <summary>
/// 台灣各式識別碼驗證器
/// </summary>
internal static class ValidatorUtils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool MatchCore(Regex pattern, string input, bool caseSensitive = false)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        if (caseSensitive)
        {
            return pattern.IsMatch(input);
        }

        // 避免 ToUpperInvariant() 的字串分配：
        // 驗證器的 regex pattern 皆使用 [A-Z] 明確匹配大寫，
        // 這裡將小寫字元轉為大寫後再匹配
#if NET8_0_OR_GREATER
        return pattern.IsMatch(string.Create(input.Length, input, static (span, src) =>
        {
            for (var i = 0; i < src.Length; i++)
            {
                span[i] = char.ToUpperInvariant(src[i]);
            }
        }));
#else
        return pattern.IsMatch(input.ToUpperInvariant());
#endif
    }
}