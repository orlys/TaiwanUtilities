// transpile from https://github.com/enylin/taiwan-id-validator/commit/6a673c608e5ec2287a58457a6dc2317f7a03f338
// license: MIT

namespace TaiwanUtilities;
using System;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

partial class TaiwanIdValidator
{
    /// <summary>
    /// 驗證是否為有效的營利事業統一編號
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static bool IsBusinessAdministrationNumber(string input, bool applyOldRules = false)
    {
        return 
            MatchCore(GetBaNumberPattern(), input) &&
            Validate(input, applyOldRules);

        [MethodImpl(MethodImplOptions.AggressiveInlining | (MethodImplOptions)512)]
        static bool Validate(string input, bool applyOldRules)
        {
            var checksum =
                Fusion(input[0], 1) +
                Fusion(input[1], 2) +
                Fusion(input[2], 1) +
                Fusion(input[3], 2) +
                Fusion(input[4], 1) +
                Fusion(input[5], 2) +
                Fusion(input[6], 4) +
                Fusion(input[7], 1);

            var divisor = applyOldRules ? 10 : 5;

            if(checksum % divisor is 0)
            {
                return true;
            }

            if(input[6] is '7' &&
               (checksum + 1) % divisor is 0)
            {
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | (MethodImplOptions)512)]
        static int Fusion(char c, byte @base)
        {
            var n = (byte)(c - '0') * @base;
            return (n % 10) + (byte)Math.Floor(n / 10d);
        }
    }

#if NET7_0_OR_GREATER

    [GeneratedRegex(@"^[0-9]{8}$", RegexOptions.Singleline)]
    private static partial Regex GetBaNumberPattern();

#else

    private static readonly Lazy<Regex> s_baNumberPatternCache = new (() => new (@"^[0-9]{8}$", RegexOptions.Compiled | RegexOptions.Singleline));
    private static Regex GetBaNumberPattern() => s_baNumberPatternCache.Value;

#endif

}

