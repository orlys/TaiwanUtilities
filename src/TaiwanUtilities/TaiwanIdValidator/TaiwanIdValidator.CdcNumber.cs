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

