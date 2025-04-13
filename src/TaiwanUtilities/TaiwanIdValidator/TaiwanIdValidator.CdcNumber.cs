// transpile from https://github.com/enylin/taiwan-id-validator/commit/6a673c608e5ec2287a58457a6dc2317f7a03f338
// license: MIT

namespace TaiwanUtilities;
using System;
using System.Text.RegularExpressions;

partial class TaiwanIdValidator
{
    /// <summary>
    /// 驗證是否為有效的自然人憑證號碼
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static bool IsCitizenDigitalCertificateNumber(string input) => MatchCore(GetCdcNumberPattern(), input);

#if NET7_0_OR_GREATER

    [GeneratedRegex(@"^[A-Z]{2}[0-9]{14}$", RegexOptions.Singleline)]
    private static partial Regex GetCdcNumberPattern();

#else

    private static readonly Lazy<Regex> s_cdcNumberPatternCache = new (() => new (@"^[A-Z]{2}[0-9]{14}$", RegexOptions.Compiled | RegexOptions.Singleline));
    private static Regex GetCdcNumberPattern() => s_cdcNumberPatternCache.Value;

#endif

}

