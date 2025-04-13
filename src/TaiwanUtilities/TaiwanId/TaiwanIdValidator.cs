// transpile from https://github.com/enylin/taiwan-id-validator/commit/6a673c608e5ec2287a58457a6dc2317f7a03f338
// license: MIT

namespace TaiwanUtilities;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

/// <summary>
/// 台灣各式識別碼驗證器
/// </summary>
public static partial class TaiwanIdValidator
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool MatchCore(Regex pattern, string input, bool caseSensitive = false)
    {
        return
            !string.IsNullOrWhiteSpace(input) &&
            pattern.IsMatch(caseSensitive ? input : input.ToUpperInvariant());
    }
}