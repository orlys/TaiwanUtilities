namespace TaiwanUtilities;
using System;
using System.Text.RegularExpressions;

partial class TaiwanIdValidator
{

    /// <summary>
    /// 驗證是否為有效的電子發票手機條碼
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static bool IsElectronicInvoiceMobileBarcode(string input) => MatchCore(GetEimBarcodePattern(), input);

#if NET7_0_OR_GREATER

    [GeneratedRegex(@"^\/[0-9A-Z.\-+]{7}$", RegexOptions.Singleline)]
    private static partial Regex GetEimBarcodePattern();

#else

    private static readonly Lazy<Regex> s_eimBarcodePatternCache = new (() => new (@"^\/[0-9A-Z.\-+]{7}$", RegexOptions.Compiled | RegexOptions.Singleline));
    private static Regex GetEimBarcodePattern() => s_eimBarcodePatternCache .Value;

#endif

}

