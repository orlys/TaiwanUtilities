// transpile from https://github.com/enylin/taiwan-id-validator/commit/6a673c608e5ec2287a58457a6dc2317f7a03f338
// license: MIT

namespace TaiwanUtilities;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

partial class TaiwanIdValidator
{

    /// <summary>
    /// 驗證是否為有效的身分證識別碼
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static bool IsIdentityCardNumber(string input, bool applyOldRules = false)
    {
        if (MatchCore(GetNationalIdPattern(), input, true))
        {
            return VerifyTaiwanIdIntermediateString(input);
        }

        if (applyOldRules && MatchCore(GetUiNumberOldFormatPattern(), input, true))
        {
            return VerifyTaiwanIdIntermediateString(input);
        }

        if (MatchCore(GetUiNumberNewFormatPattern(), input, true))
        {
            return VerifyTaiwanIdIntermediateString(input);
        } 

        return false;
    }

#if NET7_0_OR_GREATER

    // 國民身分證字號
    [GeneratedRegex(@"^[A-Z][12]\d{8}$", RegexOptions.Singleline, 1000)]
    private static partial Regex GetNationalIdPattern();


    // 統一證號舊式格式
    [GeneratedRegex(@"^[A-Z][A-D]\d{8}$", RegexOptions.Singleline, 1000)]
    private static partial Regex GetUiNumberOldFormatPattern();

    // 統一證號新式格式
    // 0-6: 外國人或無國籍人士
    // 7: 無國籍居民
    // 8: 港澳居民
    // 9: 中國大陸居民
    [GeneratedRegex(@"^[A-Z][89](?<REGION>[0-9])\d{7}$", RegexOptions.Singleline, 1000)]
    private static partial Regex GetUiNumberNewFormatPattern();


#else

    // 國民身分證字號
    private static readonly Lazy<Regex> s_nationalIdPatternCache = new (() => new (@"^[A-Z][12]\d{8}$", RegexOptions.Compiled | RegexOptions.Singleline, TimeSpan.FromSeconds(1)));
    private static Regex GetNationalIdPattern() => s_nationalIdPatternCache.Value;
    
    // 統一證號舊式格式
    private static Regex GetUiNumberOldFormatPattern() => s_uiNumberOldFormatPattern.Value;
    private static readonly Lazy<Regex> s_uiNumberOldFormatPattern = new(() => new( 
        pattern: @"^[A-Z][A-D]\d{8}$",
        options: RegexOptions.Compiled | RegexOptions.Singleline,
        matchTimeout: TimeSpan.FromSeconds(1)));

    // 統一證號新式格式
    private static Regex GetUiNumberNewFormatPattern() => s_uiNumberNewFormatPattern.Value;
    private static readonly Lazy<Regex> s_uiNumberNewFormatPattern = new(() => new(
        pattern: @"^[A-Z][89](?<REGION>[0-9])\d{7}$",
        options: RegexOptions.Compiled | RegexOptions.Singleline,
        matchTimeout: TimeSpan.FromSeconds(1)));

#endif

    /**
    *  A=10 台北市     J=18 新竹縣     S=26 高雄縣
    *  B=11 台中市     K=19 苗栗縣     T=27 屏東縣
    *  C=12 基隆市     L=20 台中縣     U=28 花蓮縣
    *  D=13 台南市     M=21 南投縣     V=29 台東縣
    *  E=14 高雄市     N=22 彰化縣     W=32 金門縣*
    *  F=15 台北縣     O=35 新竹市*    X=30 澎湖縣
    *  G=16 宜蘭縣     P=23 雲林縣     Y=31 陽明山
    *  H=17 桃園縣     Q=24 嘉義縣     Z=33 連江縣*
    *  I=34 嘉義市*    R=25 台南縣
    *
    *  Step 1: 英文字母按照上表轉換為數字之後，十位數 * 1 + 個位數 * 9 相加
    */

    private static readonly int[] TAIWAN_ID_LOCALE_CODE_LIST = [
        1,  // A -> 10 -> 1 * 1 + 9 * 0 = 1
        10, // B -> 11 -> 1 * 1 + 9 * 1 = 10
        19, // C -> 12 -> 1 * 1 + 9 * 2 = 19
        28, // D
        37, // E
        46, // F
        55, // G
        64, // H
        39, // I -> 34 -> 1 * 3 + 9 * 4 = 39
        73, // J
        82, // K
        2,  // L
        11, // M
        20, // N
        48, // O -> 35 -> 1 * 3 + 9 * 5 = 48
        29, // P
        38, // Q
        47, // R
        56, // S
        65, // T
        74, // U
        83, // V
        21, // W -> 32 -> 1 * 3 + 9 * 2 = 21
        3,  // X
        12, // Y
        30  // Z -> 33 -> 1 * 3 + 9 * 3 = 30
    ];

    private static readonly int[] RESIDENT_CERTIFICATE_NUMBER_LIST = [
        0, // A
        1, // B
        2, // C
        3, // D
        4, // E
        5, // F
        6, // G
        7, // H
        4, // I
        8, // J
        9, // K
        0, // L
        1, // M
        2, // N
        5, // O
        3, // P
        4, // Q
        5, // R
        6, // S
        7, // T
        8, // U
        9, // V
        2, // W
        0, // X
        1, // Y
        3  // Z
    ];

    // Step 2: 第 1 位數字 (只能為 1 or 2) 至第 8 位數字分別乘上 8, 7, 6, 5, 4, 3, 2, 1 後相加，再加上第 9 位數字
    private static readonly int[] ID_COEFFICIENTS = [1, 8, 7, 6, 5, 4, 3, 2, 1, 1];

    /// <summary>
    /// 驗證台灣身分證號碼或居留證號碼的中間字串
    /// </summary>
    /// <param name="input">要驗證的字串</param>
    /// <returns>如果 input 是有效的台灣身分證中間字串，則返回 true</returns>
    private static bool VerifyTaiwanIdIntermediateString(string input)
    {
        int GetCharOrder(string s, int i) => s[i] - 'A';

        var firstDigit = TAIWAN_ID_LOCALE_CODE_LIST[GetCharOrder(input, 0)];
        var secondDigit = 0;

        if (char.IsLetter(input[1])) // 舊版居留證編號
        {
            secondDigit = RESIDENT_CERTIFICATE_NUMBER_LIST[GetCharOrder(input, 1)];
        }
        else
        {
            secondDigit = int.Parse(input[1].ToString());
        }

        var idInDigits = new List<int> { firstDigit, secondDigit };

        for (var i = 2; i < input.Length; i++)
        {
            idInDigits.Add(int.Parse(input[i].ToString()));
        }

        var sum = 0;
        for (var i = 0; i < idInDigits.Count; i++)
        {
            sum += idInDigits[i] * ID_COEFFICIENTS[i];
        }

        // Step 3: 如果該數字為 10 的倍數，則為正確身分證字號
        return sum % 10 is 0;
    }
}

