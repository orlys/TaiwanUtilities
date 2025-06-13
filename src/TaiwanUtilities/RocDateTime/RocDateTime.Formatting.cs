namespace TaiwanUtilities;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

[DebuggerDisplay("{ToString(),nc}")]
partial struct RocDateTime : IFormattable
{

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private static readonly IFormatProvider s_formatProvider = new FormatProvider();

    public override string ToString()
    {
        return ToStringCore(null, s_formatProvider);
    }
    public string ToString(string format)
    {
        return ToStringCore(format, s_formatProvider);
    }
    public string ToString(IFormatProvider formatProvider)
    {
        return ToStringCore(null, s_formatProvider);
    }
    public string ToString(string format, IFormatProvider formatProvider)
    {
        return ToStringCore(format, s_formatProvider);
    }

    private string ToStringCore(string format, IFormatProvider formatProvider)
    {
        var str = (ICustomFormatter)formatProvider.GetFormat(typeof(ICustomFormatter));
        return str.Format(format, this, formatProvider);
    }


    private partial class FormatProvider : IFormatProvider, ICustomFormatter
    {
        public string Format(string format, object arg, IFormatProvider fp)
        {
            return arg switch
            {
                null => string.Empty,
                RocDateTime rdt => FormatCore(format, rdt, fp),
                IFormattable fmt => fmt.ToString(format, fp),
                string s => s,
                var o => o.ToString()
            };
        }


#if NET7_0_OR_GREATER
        // https://github.com/dotnet/runtime/issues/104212
        [GeneratedRegex(
            pattern: @"(?<FORMAT>(民國日期|date|DATE|time|TIME|full|FULL|民國年|yyy|MM|dd|hh|mm|ss|日期|時間|[年月日時分秒]|[GgTtFfMmDd]))",
            options: RegexOptions.ExplicitCapture | RegexOptions.Singleline,
            matchTimeoutMilliseconds: 1000)]
        private static partial Regex GetFormatPattern();
#else
    private static Regex BuildPattern()
    {
        return new (
            pattern: @"(?<FORMAT>(民國日期|date|DATE|time|TIME|full|FULL|民國年|yyy|MM|dd|hh|mm|ss|日期|時間|[年月日時分秒]|[GgTtFfMmDd]))",
            options: RegexOptions.ExplicitCapture | RegexOptions.Singleline | RegexOptions.Compiled ,
            matchTimeout: TimeSpan.FromMinutes(1000));
    }
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private static readonly Lazy<Regex> s_patternCache = new (BuildPattern);
    private static Regex GetFormatPattern() => s_patternCache.Value;
#endif


        static string FormatCore(string format, RocDateTime rdt, IFormatProvider fp)
        {
            format ??= DefaultFormat;

            return format switch
            {
                "民國年" => rdt.BeforeEra
                    ? $"民國前{ChineseNumeric.ToString(rdt.Year, "tw")}年"
                    : $"民國{ChineseNumeric.ToString(rdt.Year, "tw")}年",

                "年" => rdt.BeforeEra
                    ? $"民前{ChineseNumeric.ToString(rdt.Year, "tw")}年"
                    : $"{ChineseNumeric.ToString(rdt.Year, "tw")}年",

                "月" => $"{ChineseNumeric.ToString(rdt.Month, "tw")}月",
                "時" => $"{ChineseNumeric.ToString(rdt.Hour, "tw")}時",
                "日" => $"{ChineseNumeric.ToString(rdt.Day, "tw")}日",
                "分" => $"{ChineseNumeric.ToString(rdt.Minute, "tw")}分",
                "秒" => $"{ChineseNumeric.ToString(rdt.Second, "tw")}秒",

                "yyy" or "year" => rdt.BeforeEra
                    ? $"{BeforeEraSymbol}{rdt.Year:D3}"
                    : $"{rdt.Year:D3}",

                "MM" or "month" => rdt.Month.ToString("D2"),
                "dd" or "day" => rdt.Day.ToString("D2"),
                "hh" or "hour" => rdt.Hour.ToString("D2"),
                "mm" or "min" or "minute" => rdt.Minute.ToString("D2"),
                "ss" or "sec" or "second" => rdt.Second.ToString("D2"),

                // 年月模式
                "m" => $"{(rdt.BeforeEra ? BeforeEraSymbol : null)}{rdt.Year:D}/{rdt.Month:D2}",
                // 年月模式
                "M" => $"{(rdt.BeforeEra ? BeforeEraSymbol : null)}{rdt.Year:D}年{rdt.Month:D2}月",

                // 簡短日期模式
                "d" or "date" => $"{(rdt.BeforeEra ? BeforeEraSymbol : null)}{rdt.Year:D}/{rdt.Month:D2}/{rdt.Day:D2}",

                // 完整日期模式
                "D" or "DATE" => $"{(rdt.BeforeEra ? "民前" : null)}{rdt.Year:D}年{rdt.Month:D}月{rdt.Day:D}日",

                // 簡短時間模式
                "t" or "time" => $"{rdt.Hour:D2}:{rdt.Minute:D2}:{rdt.Second:D2}",

                // 完整時間模式
                "T" or "TIME" => $"{rdt.Hour:D}時{rdt.Minute:D}分{rdt.Second:D}秒",

                // 完整日期/時間模式 (簡短時間)
                "f" or "full" => string.Format(fp, "{0:date} {0:time}", rdt),

                // 完整日期/時間模式 (完整時間)
                "F" or "FULL" => string.Format(fp, "{0:DATE}{0:TIME}", rdt),

                // 簡短通用模式
                "g" => (rdt.Year, rdt.Month, rdt.Day, rdt.Hour, rdt.Minute, rdt.Second) switch
                {
                    (_, _, _, 0, 0, 0) => string.Format(fp, "{0:date}", rdt),
                    _ => string.Format(fp, "{0:date} {0:time}", rdt)
                },

                // 完整通用模式
                "G" => (rdt.Year, rdt.Month, rdt.Day, rdt.Hour, rdt.Minute, rdt.Second) switch
                {
                    (_, _, _, 0, 0, 0) => string.Format(fp, "{0:DATE}", rdt),
                    _ => string.Format(fp, "{0:DATE} {0:TIME}", rdt)
                },

                "民國日期" => string.Format(fp, "{0:民國年}{0:月}{0:日}", rdt),
                "日期" => string.Format(fp, "{0:年}{0:月}{0:日}", rdt),
                "時間" => string.Format(fp, "{0:時}{0:分}{0:秒}", rdt),

                //var formats when !string.IsNullOrWhiteSpace(formats) && GetFormatPattern().Replace(formats, x=>)
                //    .Matches(formats) is { Count: > 0 } m
                //=> string.Join(null, m.Cast<Match>().Select(m => FormatCore(m.Groups["FORMAT"].Value, rdt, fp))),

                var formats when !string.IsNullOrWhiteSpace(formats)
                    => GetFormatPattern().Replace(formats, x => FormatCore( x.Groups["FORMAT"].Value, rdt, fp)),


                _ => throw new NotSupportedException($"Format '{format}' is not supported.")
            };
        }

        object IFormatProvider.GetFormat(Type formatType)
        {
            if (typeof(ICustomFormatter) == formatType)
            {
                return this;
            }

            return default;
        }

    }
}
