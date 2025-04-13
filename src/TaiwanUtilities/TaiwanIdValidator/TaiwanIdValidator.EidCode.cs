// transpile from https://github.com/enylin/taiwan-id-validator/commit/6a673c608e5ec2287a58457a6dc2317f7a03f338
// license: MIT

namespace TaiwanUtilities;
using System;
using System.Text.RegularExpressions;

partial class TaiwanIdValidator
{

    /// <summary>
    /// 驗證是否為有效的電子發票捐贈碼
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static bool IsElectronicInvoiceDonateCode(string input) => MatchCore(GetEidCodePattern(), input);

#if NET7_0_OR_GREATER

    [GeneratedRegex(@"^[0-9]{3,7}$", RegexOptions.Singleline)]
    private static partial Regex GetEidCodePattern();

#else

    private static readonly Lazy<Regex> s_eidCodePatternCache = new (() => new (@"^[0-9]{3,7}$", RegexOptions.Compiled | RegexOptions.Singleline));
    private static Regex GetEidCodePattern() => s_eidCodePatternCache.Value;

#endif

}

