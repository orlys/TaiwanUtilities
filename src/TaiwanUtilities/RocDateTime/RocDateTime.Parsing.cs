namespace TaiwanUtilities;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;


partial struct RocDateTime
#if NET7_0_OR_GREATER
    : IParsable<RocDateTime>,
    ISpanParsable<RocDateTime>
#endif
{
#if NET7_0_OR_GREATER
    // https://github.com/dotnet/runtime/issues/104212
    [GeneratedRegex(@"^(?<DATE>(民[國国])?(?<BEFORE_ERA>[前\-\^])?(?<YEAR>[1-9１２３４５６７８９壹一貳贰二參参三肆四伍五陸陆六柒七捌八玖九][百佰]?[0-9０１２３４５６７８９零〇壹一貳贰二參参三肆四伍五陸陆六柒七捌八玖九][十拾]?[0-9０１２３４５６７８９零〇壹一貳贰二參参三肆四伍五陸陆六柒七捌八玖九]|[0-9０１２３４５６７８９零〇壹一貳贰二參参三肆四伍五陸陆六柒七捌八玖九]?[百佰]?[1-9１２３４５６７８９壹一貳贰二參参三肆四伍五陸陆六柒七捌八玖九][十拾]?[0-9０１２３４５６７８９零〇壹一貳贰二參参三肆四伍五陸陆六柒七捌八玖九]|[0０零〇]?[百佰]?[0０零〇]?[十拾]?[1-9１２３４５６７８９壹一貳贰二參参三肆四伍五陸陆六柒七捌八玖九])[年\/\-]?(?<MONTH>[1１壹一][十拾]?[0-2０１２零〇壹一貳贰二]|[0０零〇]?[十拾]?[1-9１２３４５６７８９壹一貳贰二參参三肆四伍五陸陆六柒七捌八玖九])[月\-\/]?(?<DAY>[3３參参三][十拾]?[0-1]|[1-2１２壹一貳贰二][十拾]?[0-9０１２３４５６７８９零〇壹一貳贰二參参三肆四伍五陸陆六柒七捌八玖九]|[0０零〇]?[十拾]?[1-9１２３４５６７８９壹一貳贰二參参三肆四伍五陸陆六柒七捌八玖九])([號号日])?)(?<TIME>(?<HOUR>[2２貳贰二][十拾]?[0-3０１２３零〇壹一貳贰二參参三]|[1１壹一][十拾]?[0-9０１２３４５６７８９零〇壹一貳贰二參参三肆四伍五陸陆六柒七捌八玖九]|[0０零〇]?[十拾]?[0-9０１２３４５６７８９零〇壹一貳贰二參参三肆四伍五陸陆六柒七捌八玖九])[時點点时\-\:]?((?<MINUTE>[1-5１２３４５壹一貳贰二參参三肆四伍五][十拾]?[0-9０１２３４５６７８９零〇壹一貳贰二參参三肆四伍五陸陆六柒七捌八玖九]|[0０零〇]?[十拾]?[0-9０１２３４５６７８９零〇壹一貳贰二參参三肆四伍五陸陆六柒七捌八玖九])[分\-\:]?)??((?<SECOND>[1-5１２３４５壹一貳贰二參参三肆四伍五][十拾]?[0-9０１２３４５６７８９零〇壹一貳贰二參参三肆四伍五陸陆六柒七捌八玖九]|[0０零〇]?[十拾]?[0-9０１２３４５６７８９零〇壹一貳贰二參参三肆四伍五陸陆六柒七捌八玖九])秒?)??)??$", RegexOptions.ExplicitCapture | RegexOptions.Compiled | RegexOptions.RightToLeft | RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex GetPattern(); 
#else
    private static readonly Lazy<Regex> s_patternCache = new (()=>new (@"^(?<DATE>(民[國国])?(?<BEFORE_ERA>[前\-\^])?(?<YEAR>[1-9１２３４５６７８９壹一貳贰二參参三肆四伍五陸陆六柒七捌八玖九][百佰]?[0-9０１２３４５６７８９零〇壹一貳贰二參参三肆四伍五陸陆六柒七捌八玖九][十拾]?[0-9０１２３４５６７８９零〇壹一貳贰二參参三肆四伍五陸陆六柒七捌八玖九]|[0-9０１２３４５６７８９零〇壹一貳贰二參参三肆四伍五陸陆六柒七捌八玖九]?[百佰]?[1-9１２３４５６７８９壹一貳贰二參参三肆四伍五陸陆六柒七捌八玖九][十拾]?[0-9０１２３４５６７８９零〇壹一貳贰二參参三肆四伍五陸陆六柒七捌八玖九]|[0０零〇]?[百佰]?[0０零〇]?[十拾]?[1-9１２３４５６７８９壹一貳贰二參参三肆四伍五陸陆六柒七捌八玖九])[年\/\-]?(?<MONTH>[1１壹一][十拾]?[0-2０１２零〇壹一貳贰二]|[0０零〇]?[十拾]?[1-9１２３４５６７８９壹一貳贰二參参三肆四伍五陸陆六柒七捌八玖九])[月\-\/]?(?<DAY>[3３參参三][十拾]?[0-1]|[1-2１２壹一貳贰二][十拾]?[0-9０１２３４５６７８９零〇壹一貳贰二參参三肆四伍五陸陆六柒七捌八玖九]|[0０零〇]?[十拾]?[1-9１２３４５６７８９壹一貳贰二參参三肆四伍五陸陆六柒七捌八玖九])([號号日])?)(?<TIME>(?<HOUR>[2２貳贰二][十拾]?[0-3０１２３零〇壹一貳贰二參参三]|[1１壹一][十拾]?[0-9０１２３４５６７８９零〇壹一貳贰二參参三肆四伍五陸陆六柒七捌八玖九]|[0０零〇]?[十拾]?[0-9０１２３４５６７８９零〇壹一貳贰二參参三肆四伍五陸陆六柒七捌八玖九])[時點点时\-\:]?((?<MINUTE>[1-5１２３４５壹一貳贰二參参三肆四伍五][十拾]?[0-9０１２３４５６７８９零〇壹一貳贰二參参三肆四伍五陸陆六柒七捌八玖九]|[0０零〇]?[十拾]?[0-9０１２３４５６７８９零〇壹一貳贰二參参三肆四伍五陸陆六柒七捌八玖九])[分\-\:]?)??((?<SECOND>[1-5１２３４５壹一貳贰二參参三肆四伍五][十拾]?[0-9０１２３４５６７８９零〇壹一貳贰二參参三肆四伍五陸陆六柒七捌八玖九]|[0０零〇]?[十拾]?[0-9０１２３４５６７８９零〇壹一貳贰二參参三肆四伍五陸陆六柒七捌八玖九])秒?)??)??$", RegexOptions.ExplicitCapture | RegexOptions.Compiled | RegexOptions.RightToLeft | RegexOptions.IgnoreCase | RegexOptions.Singleline));
    private static Regex GetPattern() => s_patternCache.Value;
#endif


    public static RocDateTime Parse(string s)
    {
        return Parse(s, null);
    }

    public static RocDateTime Parse(string s, IFormatProvider formatProvider)
    {
        Guard.ThrowIfNullOrWhiteSpace(s);
        _ = ParseCore(s.AsSpan(), true, out var rocDateTime);
        return rocDateTime;
    }

    public static bool TryParse(string s, out RocDateTime rocDateTime)
    {
        return TryParse(s, null, out rocDateTime);
    }
    public static bool TryParse(string s, IFormatProvider formatProvider, out RocDateTime rocDateTime)
    {
        return ParseCore(s.AsSpan(), false, out rocDateTime);
    }
     
    private readonly static ISet<char> s_validCharacters = FrozenSet.ToFrozenSet([
        ' ', '\t',
        '/','-',':','^',
        '年','月','日','時','分','秒','民','國','前','国','点','时','号',
        '0','1','2','3','4','5','6','7','8','9',
        '０','１','２','３','４','５','６','７','８','９',
        '〇','一','二','三','四','五','六','七','八','九','十','百',
        '零','壹','貳','參','肆','伍','陸','柒','捌','玖','拾','佰',
                 '贰','参',         '陆'
    ]);

    private static bool EnsureNoInvalidCharacter(ReadOnlySpan<char> s, out int index)
    {
        for (index = 0; index < s.Length; index++)
        {
            var current = s[index];
            if (!s_validCharacters.Contains(current))
            {
                return false;
            }
        }
        return true;
    }

    private static bool ParseCore(ReadOnlySpan<char> str, bool throwError, out RocDateTime rocDateTime)
    {
        rocDateTime = default;
        
        if (str.IsEmpty || str.IsWhiteSpace())
        {
            if (throwError)
            {
                throw new ArgumentException("String is null or empty", nameof(str));
            }
            return false;
        }


        if (!EnsureNoInvalidCharacter(str, out var index))
        {
            if (throwError)
            {
                // 無法識別的字元
                throw new FormatException($"Invalid character '{str[index]}' at index {index}");
            }
            return false;
        }
        
        // 移除空白
        var emptyRemoved = Regex.Replace(str.ToString(), @"\s", string.Empty);

        if (GetPattern().Match(emptyRemoved) is not { Success: true } m)
        {
            if (throwError)
            {
                // 無法識別的格式
                throw new FormatException("Invalid format");
            }
            return false;
        }


        var year = ParseNumeric(m, "YEAR", false, 0);
        var month = ParseNumeric(m, "MONTH", false, 1);
        var day = ParseNumeric(m, "DAY", false, 1);
        var hour = ParseNumeric(m, "HOUR", false, 0);
        var minute = ParseNumeric(m, "MINUTE", false, 0);
        var second = ParseNumeric(m, "SECOND", false, 0);

        var isBeforeEra = CanParse(m, "BEFORE_ERA");

        rocDateTime = new RocDateTime(isBeforeEra ? -year : year, month, day, hour, minute, second, 0);

        return true;

        static int ParseNumeric(Match match, string groupName, bool throwError, int defaultValue)
        {
            var str = match.Groups[groupName].Value;

            if (throwError)
            {
                return (int)ChineseNumeric.Parse(str);
            }

            if (ChineseNumeric.TryParse(str, out var result))
            {
                return (int)result;
            }
            return defaultValue;
        }

        static bool CanParse(Match m, string groupName)
        {
            return m.Groups[groupName].Success;
        }
    }

    public static RocDateTime Parse(ReadOnlySpan<char> s)
    {
        return Parse(s, null);
    }
    public static RocDateTime Parse(ReadOnlySpan<char> s, IFormatProvider provider)
    { 
        _ = ParseCore(s, true, out var rocDateTime);
        return rocDateTime;
    }

    public static bool TryParse(ReadOnlySpan<char> s, [MaybeNullWhen(false)] out RocDateTime result)
    {
        return TryParse(s, null, out result);
    }
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider provider, [MaybeNullWhen(false)] out RocDateTime result)
    {
        return ParseCore(s, false, out result);
    }





    //private static readonly Lazy<Regex> s_dateTimePattern = new Lazy<Regex>(() =>
    //{
    //    // lang = regex
    //    var pattern = """
    //            ^
    //            (?<DATE>
    //                (民[國国])?
    //                (?<BEFORE_ERA>[前\-\^])?
    //                (?<YEAR>
    //                    [1-9][百佰]?[0-9][十拾]?[0-9]|
    //                    [0-9]?[百佰]?[1-9][十拾]?[0-9]|
    //                    [0]?[百佰]?[0]?[十拾]?[1-9]
    //                )
    //                [年\/\-]?
    //                (?<MONTH>[1][十拾]?[0-2]|[0]?[十拾]?[1-9])
    //                [月\-\/]?
    //                (?<DAY>[3][十拾]?[0-1]|[1-2][十拾]?[0-9]|[0]?[十拾]?[1-9])
    //                ([號号日])?
    //            )
    //            (?<TIME>
    //                (?<HOUR>[2][十拾]?[0-3]|[1][十拾]?[0-9]|[0]?[十拾]?[0-9])
    //                [時點点时\-\:]?

    //                (
    //                    (?<MINUTE>[1-5][十拾]?[0-9]|[0]?[十拾]?[0-9])
    //                    [分\-\:]?
    //                )??

    //                (
    //                    (?<SECOND>[1-5][十拾]?[0-9]|[0]?[十拾]?[0-9])
    //                    秒?
    //                )??
    //            )??
    //            $
    //            """;

    //    var patternString = pattern
    //        .Replace(Environment.NewLine, null)
    //        .Replace(" ", null)
    //        .Replace("\t", null)
    //        .Replace("[0]", "[0０零〇]")
    //        .Replace("[1]", "[1１壹一]")
    //        .Replace("[2]", "[2２貳二]")
    //        .Replace("[3]", "[3３参三]")
    //        .Replace("[0-2]", "[0-2０１２零〇壹一貳二]")
    //        .Replace("[0-3]", "[0-3０１２３零〇壹一貳二参三]")
    //        .Replace("[0-9]", "[0-9０１２３４５６７８９零〇壹一貳二参三肆四伍五陸六柒七捌八玖九]")
    //        .Replace("[1-2]", "[1-2１２壹一貳二]")
    //        .Replace("[1-5]", "[1-5１２３４５壹一貳二参三肆四伍五]")
    //        .Replace("[1-9]", "[1-9１２３４５６７８９壹一貳二参三肆四伍五陸六柒七捌八玖九]")
    //        ;

    //    var dateTimePattern = new Regex(patternString,
    //        RegexOptions.ExplicitCapture | RegexOptions.Compiled | RegexOptions.RightToLeft | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    //    return dateTimePattern;
    //});
    //internal static Regex DateTimePattern => s_dateTimePattern.Value;


    ////// 日期+時間
    ////internal readonly static Regex DateTimePattern = new Regex(
    ////    @"^(?<DATE>(民[國国])?(?<BEFORE_ERA>[前\-\^])?(?<YEAR>[1-9][0-9]{2}|[0-9]?[1-9][0-9]|0{0,2}[1-9])[年\/\-]?(?<MONTH>1[0-2]|0?[1-9])[月\-\/]?(?<DAY>3[0-1]|[12][0-9]|0?[1-9])([號号日])?)(?<TIME>(?<HOUR>2[0-3]|1[0-9]|0?[0-9])[時點点时\-\:]?(?<MINUTE>[1-5][0-9]|0?[0-9])[分\-\:]?((?<SECOND>[1-5][0-9]|0?[0-9])秒?)??)??$",
    ////    //@"^(?<DATE>(民[國国])?(?<BEFORE_ERA>[前\-\^])?(?<YEAR>[1-9][0-9]{2}|[0-9]?[1-9][0-9]|0{0,2}[1-9])[年\/\-]?(?<MONTH>1[0-2]|0?[1-9])[月\-\/]?(?<DAY>3[0-1]|[12][0-9]|0?[1-9])([號号日])?)(?<TIME>(?<HOUR>2[0-3]|1[0-9]|0?[0-9])[時點点时\-\:]?(?<MINUTE>[1-5][0-9]|0?[0-9])[分\-\:]?(?<SECOND>[1-5][0-9]|0?[0-9])(秒?))??$",
    ////    RegexOptions.ExplicitCapture | RegexOptions.Compiled | RegexOptions.RightToLeft | RegexOptions.IgnoreCase | RegexOptions.Singleline);

}

