namespace TaiwanUtilities;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

using TaiwanUtilities.Internals;

/// <summary>
/// 郵遞區號
/// </summary>
#if NET8_0_OR_GREATER
[Experimental(nameof(ZipCode), UrlFormat = "https://github.com/orlys/TaiwanUtilities/blob/master/docs/experimental.md#{0}")]
#else
[Obsolete("This API is experimental and subject to change.", false)]
#endif
public static partial class ZipCode
{

    private sealed class Row
    {
        /// <summary>
        /// 五碼郵遞區號
        /// </summary>
        [JsonPropertyName("郵遞區號")]
        public string? Code { get; set; }

        /// <summary>
        /// 三碼郵遞區號
        /// </summary>
        [JsonIgnore]
        public string ShortCode => Code.AsSpan(0, 3).ToString();

        /// <summary>
        /// 區域
        /// </summary>
        [JsonIgnore]
        public string Region => string.Concat(County, Town);

        [JsonIgnore]
        public string Address => string.Concat(County, Town, Road);

        [JsonPropertyName("縣市名稱")]
        public string? County { get; set; }

        [JsonPropertyName("鄉鎮市區")]
        public string? Town { get; set; }

        [JsonPropertyName("原始路名")]
        public string? Road { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks><see href="https://www.post.gov.tw/post/internet/Postal/sz_a_b_ta1.jsp"/></remarks>
        [JsonPropertyName("投遞範圍")]
        public string? Range { get; set; }
    }

    private static readonly Lazy<TrieDictionary<string>> s_data = new(static delegate
    {
        using var stream = MethodBase.GetCurrentMethod()
           .DeclaringType
           .Assembly
           .GetManifestResourceStream("TaiwanUtilities.Postal.zipcode.json");
          
        var trie = new TrieDictionary<string>();

        foreach (var row in JsonSerializer
          .Deserialize<Row[]>(stream, new JsonSerializerOptions()))
        { 
            trie[row.Region] =  row.ShortCode;
        }

        return trie;
    });

    private static string Normalize(string s)
    {
        return s
            .Replace("台", "臺");
    }

#if NET7_0_OR_GREATER
    // https://github.com/dotnet/runtime/issues/104212
    [GeneratedRegex(@"^(?<AREA>(((?<COUNTY>[\u4E00-\u9FFF]{2}市)(?<TOWN>[\u4E00-\u9FFF]{1,3}區)(?<VILLAGE>[\u4E00-\u9FFF]{2,3}里)?)|((?<COUNTY>[\u4E00-\u9FFF]{2,3}縣)((?<TOWN>[\u4E00-\u9FFF]{1,3}[市鎮](?<VILLAGE>[\u4E00-\u9FFF]{2,4}里)?)|(?<TOWN>[\u4E00-\u9FFF]{2,3}鄉)(?<VILLAGE>[\u4E00-\u9FFF]{2,3}村)?)))(?<NEIGHBOR>(?<=\<VILLAGE>)[0-9\uFF10-\uFF19零〇一二三四五六七八九十百]{1,5}?鄰)?)", RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace, 1000)]
    private static partial Regex GetPattern();
#else
    private static readonly Lazy<Regex> s_patternCache = new(() => new(@"^(?<AREA>(((?<COUNTY>[\u4E00-\u9FFF]{2}市)(?<TOWN>[\u4E00-\u9FFF]{1,3}區)(?<VILLAGE>[\u4E00-\u9FFF]{2,3}里)?)|((?<COUNTY>[\u4E00-\u9FFF]{2,3}縣)((?<TOWN>[\u4E00-\u9FFF]{1,3}[市鎮](?<VILLAGE>[\u4E00-\u9FFF]{2,4}里)?)|(?<TOWN>[\u4E00-\u9FFF]{2,3}鄉)(?<VILLAGE>[\u4E00-\u9FFF]{2,3}村)?)))(?<NEIGHBOR>(?<=\<VILLAGE>)[0-9\uFF10-\uFF19零〇一二三四五六七八九十百]{1,5}?鄰)?)",  RegexOptions.ExplicitCapture | RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace, TimeSpan.FromSeconds(1)));
    private static Regex GetPattern() => s_patternCache.Value;
#endif


    public static string? Find(string region)
    {
        if(string.IsNullOrEmpty(region))
        {
            return null;
        }

        if (GetPattern().Match(region) is not { Success: true } m)
        {
            return null;
        }

        var county = m.Groups["COUNTY"].Value;
        var town = m.Groups["TOWN"].Value;
        var str = Normalize(county + town);


        var shortZipCode = s_data.Value
            .GetValueOrDefault(str);

        return shortZipCode;
    }

    //public static string Find(PostalAddress address)
    //{
    //    return Find(address, Width.Five);
    //}

    //public static string Find(PostalAddress address, Width width)
    //{
    //    return default;
    //}

    //public enum Width
    //{
    //    /// <summary>
    //    /// 三碼
    //    /// </summary>
    //    Three = 3,

    //    /// <summary>
    //    /// 五碼
    //    /// </summary>
    //    Five = 5,

    //    ///// <summary>
    //    ///// 六碼
    //    ///// </summary>
    //    //Six = 6,
    //}

}

