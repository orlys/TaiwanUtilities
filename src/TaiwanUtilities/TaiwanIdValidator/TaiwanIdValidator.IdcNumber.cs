//namespace TaiwanUtilities;
//using System;
//using System.Runtime.CompilerServices;
//using System.Text.RegularExpressions;

//partial class TaiwanIdValidator
//{

//    /// <summary>
//    /// 驗證是否為有效的身分證識別碼
//    /// </summary>
//    /// <param name="input"></param>
//    /// <returns></returns>
//    public static bool IsIdentityCardNumber(string input, bool applyOldRules = false)
//    {
//        return
//            MatchCore(GetIdcNumberPattern(), input) &&
//            ValidateCore(input, applyOldRules);

//        [MethodImpl(MethodImplOptions.AggressiveInlining | (MethodImplOptions)512)]
//        static bool ValidateCore(string input, bool applyOldRules)
//        {
//            throw new NotImplementedException();
//        }
//    }

//#if NET7_0_OR_GREATER

//    [GeneratedRegex(@"^ $", RegexOptions.Singleline)]
//    private static partial Regex GetIdcNumberPattern();

//#else

//    private static readonly Lazy<Regex> s_idcNumberPatternCache = new (() => new (@"^ $", RegexOptions.Compiled | RegexOptions.Singleline));
//    private static Regex GetIdcNumberPattern() => s_idcNumberPatternCache.Value;

//#endif

//}

