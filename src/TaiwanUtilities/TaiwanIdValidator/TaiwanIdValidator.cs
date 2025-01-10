namespace TaiwanUtilities;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

/// <summary>
/// 台灣各式識別碼驗證器
/// </summary>
public static partial class TaiwanIdValidator
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool MatchCore(Regex pattern, string input)
    {
        return
            !string.IsNullOrWhiteSpace(input) &&
            pattern.IsMatch(input.ToUpperInvariant());
    }
}