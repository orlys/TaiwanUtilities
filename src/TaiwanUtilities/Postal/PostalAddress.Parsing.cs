namespace TaiwanUtilities;

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

/// <summary>
/// 地址
/// </summary> 
partial class PostalAddress
#if NET7_0_OR_GREATER
    : IParsable<PostalAddress>,
    ISpanParsable<PostalAddress>
#endif
{
    // 林道資訊
    // ref: https://data.gov.tw/dataset/106313
    // ref: https://zh.wikipedia.org/zh-tw/%E4%B8%AD%E8%8F%AF%E6%B0%91%E5%9C%8B%E5%B0%88%E7%94%A8%E5%85%AC%E8%B7%AF

    // 結構
    // ref: https://schema.gov.tw/lists/161

#if NET7_0_OR_GREATER
    // https://github.com/dotnet/runtime/issues/104212
    [GeneratedRegex(
        pattern: @"^(?<AREA>(((?<COUNTY>[\u4E00-\u9FFF]{2}市)(?<TOWN>[\u4E00-\u9FFF]{1,3}區)(?<VILLAGE>[\u4E00-\u9FFF]{2,3}里)?)|((?<COUNTY>[\u4E00-\u9FFF]{2,3}縣)((?<TOWN>[\u4E00-\u9FFF]{1,3}[市鎮](?<VILLAGE>[\u4E00-\u9FFF]{2,4}里)?)|(?<TOWN>[\u4E00-\u9FFF]{2,3}鄉)(?<VILLAGE>[\u4E00-\u9FFF]{2,3}村)?)))(?<NEIGHBOR>(?<=\<VILLAGE>)[0-9\uFF10-\uFF19零〇一二三四五六七八九十百]{1,5}?鄰?)?)((?<ROAD>((?#林道規則)([桃竹中投嘉高屏花東宜]|嘉南|中苗)專([1\uFF11][0-9\uFF10-\uFF19]|[1-9\uFF11-\uFF19]|[一二三四五六七八九十]?)((?<OF>[\-─－‧·之])([1\uFF11][0-9\uFF10-\uFF19]|[1-9\uFF11-\uFF19]|[一二三四五六七八九十]?))?線)|((?#路與街道規則)([^\u5DF7\u5F04\u8855\u8856\u8857\u8DEF\s\d\uFF10-\uFF19]|[\u4E00-\u5DF6\u5DF8-\u5F03\u5F05-\u8854\u8857-\u9FFF]){1,12}?([路街]|林道|大道)(?<SECTION>([^\u5DF7\u5F04\u8855\u8856\s\d\uFF10-\uFF19]|[\u4E00-\u5DF6\u5DF8-\u5F03\u5F05-\u8854\u8857-\u9FFF]){0,4}?([1\uFF11][0-9\uFF10-\uFF19]|[1-9\uFF11-\uFF19]|[一二三四五六七八九十]?)段)?)?)|(?<LOCATION>(?#地區名稱規則)([^\u5DF7\u5F04\u8855\u8856\s\d\uFF10-\uFF19]|[\u4E00-\u5DF6\u5DF8-\u5F03\u5F05-\u8854\u8857-\u9FFF]){1,6}?[^\u5DF7\u5F04\u8855\u8856\s\d\uFF10-\uFF19]))?(?<LOCATION>(?#地區名稱規則)(?<!\<LOCATION>)([^\u5DF7\u5F04\u8855\u8856\s\d\uFF10-\uFF19]|[\u4E00-\u5DF6\u5DF8-\u5F03\u5F05-\u8854\u8857-\u9FFF]){1,6}?[^\u5DF7\u5F04\u8855\u8856\s\d\uFF10-\uFF19])?(?<LANE>((([1-9\uFF11-\uFF19][0-9\uFF10-\uFF19]{0,3}|[零〇一二三四五六七八九十百千]{1,7}?)((?<OF>[\-─－‧·之])([1-9\uFF11-\uFF19][0-9\uFF10-\uFF19]{0,2}|[零〇一二三四五六七八九十百]{1,5}?)){0,3}?|([^\u5DF7\u5F04\u8855\u8856\u8857\u8DEF\u9053\s\d\uFF10-\uFF19a-zA-Z]|[\u4E00-\u5DF6\u5DF8-\u5F03\u5F05-\u8854\u8858-\u8DEE\u8DF0-\u9FFF]){1,6}?)巷))?(?<ALLEY>(?<=\<LANE>)(([1-9\uFF11-\uFF19][0-9\uFF10-\uFF19]{0,2}|[零〇一二三四五六七八九十百]{1,5}?)((?<OF>[\-─－‧·之])([1-9\uFF11-\uFF19][0-9\uFF10-\uFF19]{0,2}|[零〇一二三四五六七八九十百]{1,5}?)){0,3}?|([^\u5DF7\u5F04\u8855\u8856\u8857\u8DEF\u9053\s\d\uFF10-\uFF19a-zA-Z]|[\u4E00-\u5DF6\u5DF8-\u5F03\u5F05-\u8854\u8858-\u8DEE\u8DF0-\u9FFF]){2,5}?)弄)?(?<SUB_ALLEY>(?<=\<ALLEY>)([1-9\uFF11-\uFF19][0-9\uFF10-\uFF19]{0,2}|[零〇一二三四五六七八九十百]{1,5}?)((?<OF>[\-─－‧·之])([1-9\uFF11-\uFF19][0-9\uFF10-\uFF19]{0,2}|[零〇一二三四五六七八九十百]{1,5}?)){0,3}?[衖衕])?(?<LOCATION>(?#地區名稱規則)(?<!\<LOCATION>)([^\u5DF7\u5F04\u8855\u8856\s\d\uFF10-\uFF19]|[\u4E00-\u5DF6\u5DF8-\u5F03\u5F05-\u8854\u8857-\u9FFF]){1,6}?[^\u5DF7\u5F04\u8855\u8856\s\d\uFF10-\uFF19])?(?<NUMBER>(?<NUMBER_TYPE>[臨])?([1-9\uFF11-\uFF19][0-9\uFF10-\uFF19]{0,3}|[零〇一二三四五六七八九十百千]{1,7}?)(?<SUB_NUMBER>(?<OF>[\-─－‧·之])[1-9\uFF11-\uFF19][0-9\uFF10-\uFF19]{0,3}|[零〇一二三四五六七八九十百千]{1,7}?)?號?)?(?<FLOOR>(?<=號)(地下([1-9\uFF11-\uFF19][0-9\uFF10-\uFF19]{0,1}|[一二三四五六七八九十]{1,3}?)|([1-9\uFF11-\uFF19][0-9\uFF10-\uFF19]{0,2}|[零〇一二三四五六七八九十百]{1,5}?))樓)?(?<ROOM>(?<=\<FLOOR>|號)([\-─－‧·之]?(([^\u5DF7\u5F04\u8855\u8856\s\d\uFF10-\uFF19]|[\u4E00-\u5DF6\u5DF8-\u5F03\u5F05-\u8854\u8857-\u9FFFa-zA-Z\d])+?)))?$",
        options: RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace,
        matchTimeoutMilliseconds: 1000)]
    private static partial Regex GetPattern();
#else
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private static readonly Lazy<Regex> s_patternCache = new(() => new(
        pattern: @"^(?<AREA>(((?<COUNTY>[\u4E00-\u9FFF]{2}市)(?<TOWN>[\u4E00-\u9FFF]{1,3}區)(?<VILLAGE>[\u4E00-\u9FFF]{2,3}里)?)|((?<COUNTY>[\u4E00-\u9FFF]{2,3}縣)((?<TOWN>[\u4E00-\u9FFF]{1,3}[市鎮](?<VILLAGE>[\u4E00-\u9FFF]{2,4}里)?)|(?<TOWN>[\u4E00-\u9FFF]{2,3}鄉)(?<VILLAGE>[\u4E00-\u9FFF]{2,3}村)?)))(?<NEIGHBOR>(?<=\<VILLAGE>)[0-9\uFF10-\uFF19零〇一二三四五六七八九十百]{1,5}?鄰?)?)((?<ROAD>((?#林道規則)([桃竹中投嘉高屏花東宜]|嘉南|中苗)專([1\uFF11][0-9\uFF10-\uFF19]|[1-9\uFF11-\uFF19]|[一二三四五六七八九十]?)((?<OF>[\-─－‧·之])([1\uFF11][0-9\uFF10-\uFF19]|[1-9\uFF11-\uFF19]|[一二三四五六七八九十]?))?線)|((?#路與街道規則)([^\u5DF7\u5F04\u8855\u8856\u8857\u8DEF\s\d\uFF10-\uFF19]|[\u4E00-\u5DF6\u5DF8-\u5F03\u5F05-\u8854\u8857-\u9FFF]){1,12}?([路街]|林道|大道)(?<SECTION>([^\u5DF7\u5F04\u8855\u8856\s\d\uFF10-\uFF19]|[\u4E00-\u5DF6\u5DF8-\u5F03\u5F05-\u8854\u8857-\u9FFF]){0,4}?([1\uFF11][0-9\uFF10-\uFF19]|[1-9\uFF11-\uFF19]|[一二三四五六七八九十]?)段)?)?)|(?<LOCATION>(?#地區名稱規則)([^\u5DF7\u5F04\u8855\u8856\s\d\uFF10-\uFF19]|[\u4E00-\u5DF6\u5DF8-\u5F03\u5F05-\u8854\u8857-\u9FFF]){1,6}?[^\u5DF7\u5F04\u8855\u8856\s\d\uFF10-\uFF19]))?(?<LOCATION>(?#地區名稱規則)(?<!\<LOCATION>)([^\u5DF7\u5F04\u8855\u8856\s\d\uFF10-\uFF19]|[\u4E00-\u5DF6\u5DF8-\u5F03\u5F05-\u8854\u8857-\u9FFF]){1,6}?[^\u5DF7\u5F04\u8855\u8856\s\d\uFF10-\uFF19])?(?<LANE>((([1-9\uFF11-\uFF19][0-9\uFF10-\uFF19]{0,3}|[零〇一二三四五六七八九十百千]{1,7}?)((?<OF>[\-─－‧·之])([1-9\uFF11-\uFF19][0-9\uFF10-\uFF19]{0,2}|[零〇一二三四五六七八九十百]{1,5}?)){0,3}?|([^\u5DF7\u5F04\u8855\u8856\u8857\u8DEF\u9053\s\d\uFF10-\uFF19a-zA-Z]|[\u4E00-\u5DF6\u5DF8-\u5F03\u5F05-\u8854\u8858-\u8DEE\u8DF0-\u9FFF]){1,6}?)巷))?(?<ALLEY>(?<=\<LANE>)(([1-9\uFF11-\uFF19][0-9\uFF10-\uFF19]{0,2}|[零〇一二三四五六七八九十百]{1,5}?)((?<OF>[\-─－‧·之])([1-9\uFF11-\uFF19][0-9\uFF10-\uFF19]{0,2}|[零〇一二三四五六七八九十百]{1,5}?)){0,3}?|([^\u5DF7\u5F04\u8855\u8856\u8857\u8DEF\u9053\s\d\uFF10-\uFF19a-zA-Z]|[\u4E00-\u5DF6\u5DF8-\u5F03\u5F05-\u8854\u8858-\u8DEE\u8DF0-\u9FFF]){2,5}?)弄)?(?<SUB_ALLEY>(?<=\<ALLEY>)([1-9\uFF11-\uFF19][0-9\uFF10-\uFF19]{0,2}|[零〇一二三四五六七八九十百]{1,5}?)((?<OF>[\-─－‧·之])([1-9\uFF11-\uFF19][0-9\uFF10-\uFF19]{0,2}|[零〇一二三四五六七八九十百]{1,5}?)){0,3}?[衖衕])?(?<LOCATION>(?#地區名稱規則)(?<!\<LOCATION>)([^\u5DF7\u5F04\u8855\u8856\s\d\uFF10-\uFF19]|[\u4E00-\u5DF6\u5DF8-\u5F03\u5F05-\u8854\u8857-\u9FFF]){1,6}?[^\u5DF7\u5F04\u8855\u8856\s\d\uFF10-\uFF19])?(?<NUMBER>(?<NUMBER_TYPE>[臨])?([1-9\uFF11-\uFF19][0-9\uFF10-\uFF19]{0,3}|[零〇一二三四五六七八九十百千]{1,7}?)(?<SUB_NUMBER>(?<OF>[\-─－‧·之])[1-9\uFF11-\uFF19][0-9\uFF10-\uFF19]{0,3}|[零〇一二三四五六七八九十百千]{1,7}?)?號?)?(?<FLOOR>(?<=號)(地下([1-9\uFF11-\uFF19][0-9\uFF10-\uFF19]{0,1}|[一二三四五六七八九十]{1,3}?)|([1-9\uFF11-\uFF19][0-9\uFF10-\uFF19]{0,2}|[零〇一二三四五六七八九十百]{1,5}?))樓)?(?<ROOM>(?<=\<FLOOR>|號)([\-─－‧·之]?(([^\u5DF7\u5F04\u8855\u8856\s\d\uFF10-\uFF19]|[\u4E00-\u5DF6\u5DF8-\u5F03\u5F05-\u8854\u8857-\u9FFFa-zA-Z\d])+?)))?$",  
        options: RegexOptions.ExplicitCapture | RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace, 
        matchTimeout: TimeSpan.FromSeconds(1)));
    private static Regex GetPattern() => s_patternCache.Value;
#endif



    public static bool TryParse(
        string str,
        [NotNullWhen(true)] out PostalAddress postalAddress)
    {
        return ParseCore(str.AsSpan(), false, out postalAddress);
    }

    public static PostalAddress Parse(string s)
    {
        _ = ParseCore(s.AsSpan(), false, out var result);
        return result;
    }

    private static bool ParseCore(
        in ReadOnlySpan<char> s,
        bool throwError,
        out PostalAddress postalAddress,
        [CallerArgumentExpression(nameof(s))] string paramName = default)
    {
        postalAddress = default;
        if (s.IsEmpty || s.IsWhiteSpace())
        {
            if (throwError)
            {
                throw new ArgumentNullException(paramName);
            }
            return false;
        }

        var str = Regex.Replace(s.ToString(), @"\s+", string.Empty);

        if (GetPattern()
            .Match(str)
            is { Success: true } m)
        {
            var county = m.Groups["COUNTY"].Value;
            var town = m.Groups["TOWN"].Value;
            var village = NullIfEmpty(m.Groups["VILLAGE"].Value);
            var neighbor = NullIfEmpty(NormalizeDigits(m.Groups["NEIGHBOR"].Value, "hw").PadLeft(3, '0'));

            var road = NormalizeDigits(NormalizeOfSymbol(m.Groups["ROAD"].Value), "tw");
            var lane = NormalizeDigits(NormalizeOfSymbol(m.Groups["LANE"].Value), "fw");
            var alley = NormalizeDigits(NormalizeOfSymbol(m.Groups["ALLEY"].Value), "fw");
            var subAlley = NormalizeDigits(NormalizeOfSymbol(m.Groups["SUB_ALLEY"].Value), "fw");
            var number = NormalizeDigits(NormalizeOfSymbol(m.Groups["NUMBER"].Value), "hw");
            var floor = NormalizeDigits(NormalizeOfSymbol(m.Groups["FLOOR"].Value), "hw");
            var room = NormalizeDigits(NormalizeOfSymbol(m.Groups["ROOM"].Value), "hw");

            var isTemporary = CheckIsTemporary(number);

            postalAddress = new PostalAddress(
               county,
               town,
               village: village,
               neighbor: neighbor,
               area: string.Concat(county, town, village, neighbor),
               road: NullIfEmpty(road),
               lane: NullIfEmpty(lane),
               alley: NullIfEmpty(alley),
               subAlley: NullIfEmpty(subAlley),
               number: NullIfEmpty(number),
               floor: NullIfEmpty(floor),
               room: NullIfEmpty(room),
               address: NullIfEmpty(string.Concat(road, lane, alley, subAlley, number, floor, room)),
               isTemporary);

            return true;
        }

        if (throwError)
        {
            throw new FormatException("Invalid address format.");
        }

        return false;
    }



    public static PostalAddress Parse(ReadOnlySpan<char> s, IFormatProvider provider)
    {
        _ = ParseCore(s, false, out var result);
        return result;
    }
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider provider, [MaybeNullWhen(false)] out PostalAddress result)
    {
        return ParseCore(s, false, out result);
    }

    public static PostalAddress Parse(string s, IFormatProvider provider)
    {
        _ = ParseCore(s.AsSpan(), false, out var result);
        return result;
    }
    public static bool TryParse(string s, IFormatProvider provider, [MaybeNullWhen(false)] out PostalAddress result)
    {
        return ParseCore(s.AsSpan(), false, out result);
    }


    private static string NormalizeOfSymbol(
        string s)
    {
        return Regex.Replace(s, @"[~\-‧·]", "之");
    }

    private static string NormalizeDigits(
        string s,
        string format)
    {
        return Regex.Replace(
            input: s,
            pattern: @"[0-9\uFF10-\uFF19零〇一二三四五六七八九十百千]+",
            evaluator: (m) => ChineseNumeric.Parse(m.Value).ToString(format));
    }

    private static bool CheckIsTemporary(string s)
    {
        // 臨時性門牌
        return s.Contains("臨");
    }
}