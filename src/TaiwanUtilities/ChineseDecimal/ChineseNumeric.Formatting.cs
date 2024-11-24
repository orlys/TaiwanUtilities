namespace TaiwanUtilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

partial struct ChineseNumeric : IFormattable
{

    private static readonly IFormatProvider s_formatProvider = new FormatProvider();

    public override string ToString()
    {
        return ToString(string.Empty, s_formatProvider);
    }

    /// <summary>
    /// s
    /// </summary>
    /// <param name="format">
    /// <list type="bullet">
    ///   <item><term>tw</term> <description>繁體小寫中文數字 (例: 一百二十三)</description></item>
    ///   <item><term>TW</term> <description>繁體大寫中文數字 (例: 壹佰貳拾參)</description></item>
    ///   <item><term>cn</term> <description>簡體小寫中文數字 (例: 一百二十三)</description></item>
    ///   <item><term>CN</term> <description>簡體大寫中文數字 (例: 壹佰贰拾参)</description></item>
    /// </list>
    /// 
    /// </param>
    /// <returns></returns>
    /// 
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

        public string Format(string format, object arg, IFormatProvider formatProvider)
        {
            if (arg is ChineseNumeric cn)
            {
                return format switch
                {
                    "TW" or "zh-TW" => Format(cn, FormatterProfile.TraditionalUppercase),
                    "tw" or "zh-tw" => Format(cn, FormatterProfile.TraditionalLowercase),
                    "CN" or "zh-CN" => Format(cn, FormatterProfile.SimplifiedUppercase),
                    "cn" or "zh-cn" => Format(cn, FormatterProfile.SimplifiedLowercase),
                    _ => cn.ToString(format, CultureInfo.CurrentCulture),
                };
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

        private static string Format(ChineseNumeric input, FormatterProfile profile)
        {
            var cn = input.GetRawValue();

            // 寫得有夠醜，但我暫時沒想法，笑死
            var segments = new Stack<(decimal Val, string SegStr)>();

            var groupUnits = profile.GroupUnits;

            ProcessGroup:
            var floor = Math.Floor(cn / 10000m);
            if (floor > 0)
            {
                // value: 0 ~ 9999
                var value = (cn % 10000m);
                segments.Push(HandleTinySegment(false, value, profile));
                cn = floor;
                goto ProcessGroup;
            }
            else
            {
                segments.Push(HandleTinySegment(true, cn, profile));
            }

            var sb = new StringBuilder();
            var isPreviousZero = false;
            while (segments.TryPop(out var pair))
            {
                if (pair.Val > 0)
                {
                    isPreviousZero = false;
                    sb.Append(pair.SegStr);
                    var group = groupUnits[segments.Count];
                    sb.Append(group);
                }
                else
                {
                    if (!isPreviousZero)
                    {
                        sb.Append(pair.SegStr);
                    }
                    isPreviousZero = true;
                }
            }

            return sb.ToString();


            static (decimal value, string segmentStr) HandleTinySegment(bool isLead, decimal v, FormatterProfile profile)
            {
                var digits = profile.Digits;
                var groupTinyUnits = profile.GroupTinyUnits;

                if (!isLead && v == 0)
                {
                    return (v, digits[0]);
                }

                var input = v;
                var segments = new Stack<string>();
                var tiny = 0;
                ProcessTiny:
                var floor = (int)Math.Floor(input / 10m);
                if (floor > 0)
                {
                    // value: 0 ~ 9 
                    var value = (input % 10);
                    if (value > 0)
                    {
                        segments.Push(groupTinyUnits[tiny++]);
                        segments.Push(digits[(int)value]);
                    }
                    else
                    {
                        segments.Push(digits[(int)value]);
                        tiny++;
                    }
                    input = floor;
                    goto ProcessTiny;
                }
                else
                {
                    if (isLead)
                    {
                        if (tiny == 1)
                        //if (tiny > 0)
                        {
                            segments.Push(groupTinyUnits[tiny]);
                            if (input > 1m)
                            {
                                // 不為 1 跟 0
                                segments.Push(digits[(int)input]);
                            }
                        }
                        else
                        {
                            segments.Push(groupTinyUnits[tiny]);
                            segments.Push(digits[(int)input]);
                        }
                    }
                    else
                    {
                        if (tiny == 0)
                        {
                            segments.Push(digits[(int)input]);
                        }
                        else
                        {
                            segments.Push(groupTinyUnits[tiny]);
                            segments.Push(digits[(int)input]);
                        }

                        if (tiny < 3)
                        {
                            // 最前面補 0 
                            segments.Push(digits[0]);
                        }
                    }
                }

                // todo: Pooling
                var sb = new StringBuilder();

                var lastIsZero = false;
                while (segments.TryPop(out var segment))
                {
                    if (segment == digits[0])
                    {
                        if (!lastIsZero)
                        {
                            lastIsZero = true;
                            sb.Append(segment);
                        }
                    }
                    else
                    {
                        lastIsZero = false;
                        sb.Append(segment);
                    }
                }

                if (EndsWith(sb, digits[0]))
                {
                    // 當不為個位數時才移除最後的零
                    if (sb.Length > 1)
                    {
                        RemoveLast(sb);
                    }
                }

                return (v, sb.ToString());

                static bool EndsWith(StringBuilder sb, string str)
                {
                    if (sb.Length >= str.Length)
                    {
                        var flag = true;
                        for (int offset = sb.Length - str.Length, i = 0; offset < sb.Length; offset++, i++)
                        {
                            if (sb[offset] != str[i])
                            {
                                flag = false;
                                break;
                            }
                        }
                        return flag;
                    }
                    return false;
                }


                static StringBuilder RemoveLast(StringBuilder sb)
                {
                    if (sb.Length > 0)
                    {
                        return sb.Remove(sb.Length - 1, 1);
                    }
                    return sb;
                }
            }
        }

    }

    private class FormatterProfile
    {
        /// <summary>
        /// 繁體中文數字大寫
        /// </summary>
        public static FormatterProfile TraditionalUppercase { get; } = new('零', '壹', '貳', '參', '肆', '伍', '陸', '柒', '捌', '玖', '拾', '佰', '仟', '萬', '億', '兆', '京', '垓', '穰', '秭');

        /// <summary>
        /// 繁體中文數字小寫
        /// </summary>
        public static FormatterProfile TraditionalLowercase { get; } = new('零', '一', '二', '三', '四', '五', '六', '七', '八', '九', '十', '百', '千', '萬', '億', '兆', '京', '垓', '穰', '秭');

        /// <summary>
        /// 簡體中文數字大寫
        /// </summary>
        public static FormatterProfile SimplifiedUppercase { get; } = new('零', '壹', '贰', '参', '肆', '伍', '陆', '柒', '捌', '玖', '拾', '佰', '仟', '万', '亿', '兆', '京', '垓', '秭', '穰');

        /// <summary>
        /// 簡體中文數字小寫
        /// </summary>
        public static FormatterProfile SimplifiedLowercase { get; } = new('零', '一', '二', '三', '四', '五', '六', '七', '八', '九', '十', '百', '千', '万', '亿', '兆', '京', '垓', '秭', '穰');


        internal IReadOnlyList<string> Digits { get; }
        internal IReadOnlyList<string> GroupTinyUnits { get; }
        internal IReadOnlyList<string> GroupUnits { get; }

        public FormatterProfile(
            char zero,
            char one,
            char two,
            char three,
            char four,
            char five,
            char six,
            char seven,
            char eight,
            char nine,
            char ten,
            char hundred,
            char thousand,
            char tenThousand,
            char hundredMillion,
            char trillion,
            char tenQuadrillion,
            char hundredQuintillion,
            char septillion,
            char tenOctillion)
        {
            Digits = [
                zero.ToString(),
                one.ToString(),
                two.ToString(),
                three.ToString(),
                four.ToString(),
                five.ToString(),
                six.ToString(),
                seven.ToString(),
                eight.ToString(),
                nine.ToString()
            ];

            GroupTinyUnits = [
                null!,
                ten.ToString(),
                hundred.ToString(),
                thousand.ToString()
            ];

            GroupUnits = [
                null!,
                tenThousand.ToString(),
                hundredMillion.ToString(),
                trillion.ToString(),
                tenQuadrillion.ToString(),
                hundredQuintillion.ToString(),
                septillion.ToString(),
                tenOctillion.ToString()
            ];
        }

    }

}

