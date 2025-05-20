namespace TaiwanUtilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

[DebuggerDisplay("{ToString(),nc}")]
partial struct RocDateTime : IFormattable
{

    public override string ToString()
    {
        return ToString(null, s_formatProvider);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="format"></param>
    /// <example>ch(yyy)ch(MM)ch(dd) -> </example>
    /// <returns></returns>
    public string ToString(string format)
    {
        return ToString(format, s_formatProvider);
    }

    public string ToString(IFormatProvider formatProvider)
    {
        return ToString(null, formatProvider ?? s_formatProvider);
    }

    public string ToString(string format, IFormatProvider formatProvider)
    {
        formatProvider ??= s_formatProvider;

        if (formatProvider.GetFormat(typeof(Formatter)) is Formatter formatter)
        {
            return formatter.Format(format, this, formatProvider);
        }

        return GetRawValue().ToString(format, formatProvider);
    }

    private static readonly IFormatProvider s_formatProvider = new FormatProvider();

     


    private class FormatProvider : IFormatProvider
    {
        private Formatter _formatter;

        public FormatProvider()
        {
            _formatter = new Formatter();
        }
        public object GetFormat(Type formatType)
        {
            if (formatType == typeof(Formatter))
            {
                return _formatter;
            }
            return CultureInfo.GetCultureInfo("zh-TW");
        }
    }

    private class Formatter : ICustomFormatter
    {
        private List<FormatProcessor> _processors;

        private abstract class FormatProcessor
        {
            public abstract bool IsMatch(string format);
            public abstract string Process(Formatter formatter, string format, RocDateTime rdt);
        }

        private class YearProcessor : FormatProcessor
        {
            public override bool IsMatch(string format)
            {
                return format is "yyy";
            }

            public override string Process(Formatter formatter, string format, RocDateTime rdt)
            {
                return string.Format(s_taiwanCultureInfo, "{0:d3}", rdt.BeforeEra ? -rdt.Year : rdt.Year);
            }
        }

        private class MonthProcessor : FormatProcessor
        {
            public override bool IsMatch(string format)
            {
                return format is "MM";
            }

            public override string Process(Formatter formatter, string format, RocDateTime rdt)
            {
                return string.Format(s_taiwanCultureInfo, "{0:d2}", rdt.Month);
            }
        }

        private class DayProcessor : FormatProcessor
        {
            public override bool IsMatch(string format)
            {
                return format is "dd";
            }

            public override string Process(Formatter formatter, string format, RocDateTime rdt)
            {
                return string.Format(s_taiwanCultureInfo, "{0:d2}", rdt.Day);
            }
        }

        private class HourProcessor : FormatProcessor
        {
            public override bool IsMatch(string format)
            {
                return format is "hh";
            }

            public override string Process(Formatter formatter, string format, RocDateTime rdt)
            {
                return string.Format(s_taiwanCultureInfo, "{0:d2}", rdt.Hour);
            }
        }

        private class MinuteProcessor : FormatProcessor
        {
            public override bool IsMatch(string format)
            {
                return format is "mm";
            }

            public override string Process(Formatter formatter, string format, RocDateTime rdt)
            {
                return string.Format(s_taiwanCultureInfo, "{0:d2}", rdt.Minute);
            }
        }

        private class SecondProcessor : FormatProcessor
        {
            public override bool IsMatch(string format)
            {
                return format is "ss";
            }

            public override string Process(Formatter formatter, string format, RocDateTime rdt)
            {
                return string.Format(s_taiwanCultureInfo, "{0:d2}", rdt.Second);
            }
        }


        private static readonly CultureInfo s_taiwanCultureInfo = CultureInfo.GetCultureInfo("zh-TW");
        private class FullDateTimeProcessor : FormatProcessor
        {
            public override bool IsMatch(string format)
            {
                return string.IsNullOrWhiteSpace(format) || format is "G";
            }

            public override string Process(Formatter formatter, string format, RocDateTime rdt)
            {
                return formatter.Format("yyy/MM/dd hh:mm:ss", rdt, null);
            }
        }

        private class ChineseProcessor : FormatProcessor
        {
            private static readonly Regex s_pattern = new(@"((ch|CH)\((?<FMT>[^\)]+)\))");

            public override bool IsMatch(string format)
            {
                return s_pattern.IsMatch(format);
            }

            public override string Process(Formatter formatter, string format, RocDateTime rdt)
            {
                var sb = new StringBuilder(format);
                foreach (Match m in s_pattern.Matches(format))
                {
                    var fmt = m.Groups["FMT"].Value;
                    var stringValue = formatter.Format(fmt, rdt, s_taiwanCultureInfo);

                    if (m.Value.StartsWith("CH"))
                    {
                        sb.Replace(m.Value, GetChineseString(ChineseNumeric.Parse(stringValue), fmt, rdt));
                    }
                    else
                    {
                        sb.Replace(m.Value, GetChineseString(stringValue, fmt, rdt));
                    }
                }
                return sb.ToString();
            }

            private static string GetChineseString(object cn, string fmt, RocDateTime rdt)
            {
                if (fmt is "yyy")
                {
                    return string.Format("民國{0}{1}年", rdt.BeforeEra ? "前" : null, cn);
                }

                if (fmt is "MM")
                {
                    return string.Format("{0}月", cn);
                }

                if (fmt is "dd")
                {
                    return string.Format("{0}日", cn);
                }

                if (fmt is "hh")
                {
                    return string.Format("{0}時", cn);
                }

                if (fmt is "mm")
                {
                    return string.Format("{0}分", cn);
                }

                if (fmt is "ss")
                {
                    return string.Format("{0}秒", cn);
                }




                return cn.ToString();

            }
        }

        private class CompositedProcessor : FormatProcessor
        {

            public override bool IsMatch(string format)
            {
                return true;
            }


            public override string Process(Formatter formatter, string format, RocDateTime rdt)
            {
                var v = new StringBuilder(format)
                    .Replace("yyy", formatter.Format("yyy", rdt, null))
                    .Replace("MM", formatter.Format("MM", rdt, null))
                    .Replace("dd", formatter.Format("dd", rdt, null))
                    .Replace("hh", formatter.Format("hh", rdt, null))
                    .Replace("mm", formatter.Format("mm", rdt, null))
                    .Replace("ss", formatter.Format("ss", rdt, null));

                return v.ToString();
            }
        }



        public Formatter()
        {
            _processors = new List<FormatProcessor>
        {
            new YearProcessor(),
            new MonthProcessor(),
            new DayProcessor(),
            new HourProcessor(),
            new MinuteProcessor(),
            new SecondProcessor(),
            new FullDateTimeProcessor(),
            new ChineseProcessor(),
            new CompositedProcessor()
        };
        }


        public string Format(string format, object arg, IFormatProvider formatProvider)
        {
            if (arg is RocDateTime rdt)
            {
                foreach (var processor in _processors)
                {
                    if (processor.IsMatch(format))
                    {
                        return processor.Process(this, format, rdt);
                    }
                }

                throw new NotSupportedException();

                //return format switch
                //{
                //    "yyy" => string.Format("{0:d3}", rdt.BeforeEra ? -rdt.Year : rdt.Year),
                //    "MM" => string.Format("{0:d2}", rdt.Month),
                //    "dd" => string.Format("{0:d2}", rdt.Day),
                //    "hh" => string.Format("{0:d2}", rdt.Hour),
                //    "mm" => string.Format("{0:d2}", rdt.Minute),
                //    "ss" => string.Format("{0:d2}", rdt.Second),
                //    "ms" => string.Format("{0}", rdt.Millisecond),
                //    "us" => string.Format("{0}", rdt.Microsecond),
                //    null or "G" => string.Format("{0}-{1}-{2} {3}:{4}:{5}", rdt.Year, rdt.Month, rdt.Day, rdt.Hour, rdt.Minute, rdt.Second),

                //    var fmt => fmt
                //        .Replace("yyy", Format("yyy", rdt, formatProvider))
                //        .Replace("MM", Format("MM", rdt, formatProvider))
                //        .Replace("dd", Format("dd", rdt, formatProvider))
                //        .Replace("hh", Format("hh", rdt, formatProvider))
                //        .Replace("mm", Format("mm", rdt, formatProvider))
                //        .Replace("ss", Format("ss", rdt, formatProvider))
                //        .Replace("ms", Format("ms", rdt, formatProvider))
                //        .Replace("us", Format("us", rdt, formatProvider))
                //};

                //var formatBuilder = new StringBuilder();
                //DateTime.Now.ToString("")
                //if (format == "-DATE :TIME+")
                //{
                //    return string.Format("{0}-{1}-{2} {3}:{4}:{5}", rdt.Year, rdt.Month, rdt.Day, rdt.Hour, rdt.Minute, rdt.Second);
                //}
                //return string.Format("{0}", rdt.Year);
            }
            else
            {
                if (arg is IFormattable formattable)
                {
                    return formattable.ToString(format, formatProvider);
                }

                return arg.ToString();
            }
        }
    }
}
