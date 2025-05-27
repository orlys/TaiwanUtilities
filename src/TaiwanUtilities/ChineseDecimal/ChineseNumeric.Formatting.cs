namespace TaiwanUtilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

using TaiwanUtilities.Internal;

[DebuggerDisplay("{ToString(),nc}")]
partial struct ChineseNumeric : IFormattable
{

    private static readonly IFormatProvider s_formatProvider = new FormatProvider();

    public override string ToString()
    {
        return ToString(string.Empty, s_formatProvider);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="format"> 字串格式
    /// <list type="bullet">
    ///   <item><term>tw</term><description>繁體小寫(例: 一百二十三)</description></item>
    ///   <item><term>TW</term><description>繁體大寫(例: 壹佰貳拾參)</description></item>
    ///   <item><term>cn</term><description>簡體小寫(例: 一百二十三)</description></item>
    ///   <item><term>CN</term><description>簡體大寫(例: 壹佰贰拾参)</description></item>
    ///   <item><term>HW</term><description>半形數字(例: 123)</description></item>
    ///   <item><term>FW</term><description>全形數字(例: １２３)</description></item>
    ///   <item><description>遵照 <seealso langword="decimal"/> 的格式設定</description></item>
    /// </list>
    /// </param>
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
            return Constants.CultureInfo;
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
                    "FW" or "fw" => Format(cn, FormatterProfile.FullwidthWestern),
                    "HW" or "hw" => Format(cn, FormatterProfile.HalfwidthWestern),
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
            return profile.Mode.HasFlag(FormatterFlags.CrawlStack)
                ? CrawlStack(input, profile)
                : DirectTranslate(input, profile);
        }

        private static string DirectTranslate(ChineseNumeric input, FormatterProfile profile)
        {
            var cn = input.GetRawValue();

            var str = cn.ToString("G");

            if (!profile.Mode.HasFlag(FormatterFlags.Mapping))
            {
                return str;
            }

            var sb = new StringBuilder(str.Length);

            foreach (var c in str)
            {
                sb.Append(profile.Digits[c - '0']);
            }

            return sb.ToString();
        }

        //private static string CrawlStack(ChineseNumeric input, FormatterProfile profile)
        //{
        //    var cn = input.GetRawValue();

        //    // 寫得有夠醜，但我暫時沒想法，笑死
        //    var segments = new Stack<(decimal currentSectionValue, string segmentString, bool directPop)>();

        //    var groupUnits = profile.GroupUnits;

        //    var ic = 0;

        //    ProcessGroup:
        //    var floor = Math.Floor(cn / 10000m);
        //    if (floor > 0)
        //    {
        //        // currentSectionValue: 0 ~ 9999
        //        var currentSectionValue = (cn % 10000m);


        //        segments.Push(HandleTinySegment(false, currentSectionValue, profile));

        //        if (currentSectionValue >= 100 && currentSectionValue % 10 == 0)
        //        {
        //            segments.Push((0, profile.Digits[0], true));
        //            ic++;
        //        }
        //        cn = floor;
        //        goto ProcessGroup;
        //    }
        //    else
        //    {
        //        segments.Push(HandleTinySegment(true, cn, profile));
        //    }

        //    var sb = new StringBuilder();
        //    var isPreviousZero = false;
        //    while (segments.TryPop(out var pair))
        //    {
        //        if (pair.directPop)
        //        {
        //            if (!isPreviousZero &&
        //                segments.Count is 0 &&
        //                pair.currentSectionValue is 0)
        //            {

        //                isPreviousZero = true;
        //                ic--;
        //                continue;
        //            }

        //            isPreviousZero = true;
        //            sb.Append(pair.segmentString);
        //            ic--;
        //            continue;
        //        }


        //        if (pair.currentSectionValue > 0)
        //        {
        //            isPreviousZero = false;
        //            sb.Append(pair.segmentString);
        //            var group = groupUnits[segments.Count - ic];
        //            sb.Append(group);
        //            continue;
        //        }


        //        if (!isPreviousZero)
        //        {
        //            if (segments.Count is 0)
        //            {
        //                sb.Append(pair.segmentString);
        //            }
        //        }
        //        isPreviousZero = true;
        //    }

        //    return sb.ToString();


        //}



        private static string CrawlStack(ChineseNumeric input, FormatterProfile profile)
        {
            return new Builder().ToString(input.GetRawValue(), profile);
        }
    }

    private class Builder
    {
        private readonly record struct TinySection(decimal Value, string Content, bool IsLead)
        {
            /// <summary>
            /// 是否為補償段落，補償段落是指當 Value 為 1000 的倍數時，會在前面補上零。
            /// </summary>
            public bool Compensation => Value >= 1000 && Value % 10 == 0;

        }


        public string ToString(decimal cn, FormatterProfile profile)
        {
            var sb = new StringBuilder();

            var sections = new Queue<TinySection>();
            PrepareSections(cn, profile, sections);

            // 大單位群組位置
            var groupPosition = -1;

            // 用來記錄上個處理過的段落是否為 0 
            var lastIsZero = false;
            var compensated = false;
            while (sections.TryDequeue(out var section))
            {
                var currentSectionValue = section.Value;
                var currentIsZero = currentSectionValue is 0;
                groupPosition++;

                if (currentIsZero)
                {
                    if (lastIsZero && !section.IsLead)
                    {
                        // 如果上個段落也是 0，則不需要處理
                        continue;
                    }
                    else if (section.IsLead)
                    {
                        // 如果是最後一個段落，則需要處理
                        sb.Append(profile.Digits[0]);
                        lastIsZero = true;
                        continue;
                    }
                    else if (groupPosition is 0)
                    {
                        lastIsZero = true;
                        continue;
                    }
                    else if (compensated)
                    {
                        lastIsZero = true;
                        compensated = false;
                        continue;
                    }
                }

                if (section.Compensation && !lastIsZero && groupPosition is not 0)
                {
                    // 如果是補償段落，則需要在前面補上零
                    if (compensated)
                    {
                        compensated = false;
                    }
                    else
                    {

                        sb.Insert(0, profile.Digits[0]);
                    }
                }

                if (section.Content.StartsWith(profile.Digits[0]) &&
                    section.Content.Length > 1)
                {
                    compensated = true;
                }


                if (groupPosition is not 0 &&
                   section.Value is 0)
                {
                    sb.Insert(0, profile.Digits[0]);
                    compensated = true;
                }
                else
                {

                    var group = profile.GroupUnits[groupPosition];

                    sb.Insert(0, $"{section.Content}{group}");
                }




                lastIsZero = currentIsZero;
            }

            var result = sb.ToString();

            return result;
        }

        static void PrepareSections(decimal cn, FormatterProfile profile, Queue<TinySection> sections)
        {
            ProcessGroup:
            if (Math.Floor(cn / 10000m) is var floor &&
                floor > 0)
            {
                // currentSectionValue: 0 ~ 9999
                var value = (cn % 10000m);
                sections.Enqueue(ProcessTinySection(false, value, profile));
                cn = floor;
                goto ProcessGroup;
            }
            else
            {
                // floor == 0
                sections.Enqueue(ProcessTinySection(true, cn, profile));
            }
        }

        /// <summary>
        /// 處理小於萬的數字段落，
        /// </summary>
        /// <param name="isLead"></param>
        /// <param name="v"></param>
        /// <param name="profile"></param>
        /// <returns></returns>
        static TinySection ProcessTinySection(bool isLead, decimal v, FormatterProfile profile)
        {
            var digits = profile.Digits;
            var groupTinyUnits = profile.GroupTinyUnits;

            if (!isLead && v is 0)
            {
                return new(v, digits[0], isLead);
            }

            var input = v;
            var segments = new Stack<string>();
            var tiny = 0;
            Scan:
            var floor = (int)Math.Floor(input / 10m);
            if (floor > 0)
            {
                // currentSectionValue: 0 ~ 9 
                var value = (input % 10);
                if (value > 0)
                {
                    segments.Push(groupTinyUnits[tiny]);
                    segments.Push(digits[(int)value]);
                }
                else
                {
                    segments.Push(digits[(int)value]);
                }
                tiny++;
                input = floor;
                goto Scan;
            }
            else
            {
                // 是最後一個段落
                if (isLead)
                {
                    if (tiny == 1)
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

                // 不是最後一個段落
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

            return new(v, sb.ToString(), isLead);

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


    [Flags]
    private enum FormatterFlags
    {
        Mapping = 1,
        CrawlStack = 2,
        DirectTranslate = 4,
    }

    private class FormatterProfile
    {

        /// <summary>
        /// 繁體大寫
        /// </summary>
        public static FormatterProfile TraditionalUppercase { get; } = new(FormatterFlags.CrawlStack, "零", "壹", "貳", "參", "肆", "伍", "陸", "柒", "捌", "玖", "拾", "佰", "仟", "萬", "億", "兆", "京", "垓",  "秭","穰");

        /// <summary>
        /// 繁體小寫
        /// </summary>
        public static FormatterProfile TraditionalLowercase { get; } = new(FormatterFlags.CrawlStack, "〇", "一", "二", "三", "四", "五", "六", "七", "八", "九", "十", "百", "千", "萬", "億", "兆", "京", "垓", "秭", "穰");

        /// <summary>
        /// 簡體大寫
        /// </summary>
        public static FormatterProfile SimplifiedUppercase { get; } = new(FormatterFlags.CrawlStack, "零", "壹", "贰", "参", "肆", "伍", "陆", "柒", "捌", "玖", "拾", "佰", "仟", "万", "亿", "兆", "京", "垓", "秭", "穰");

        /// <summary>
        /// 簡體小寫
        /// </summary>
        public static FormatterProfile SimplifiedLowercase { get; } = new(FormatterFlags.CrawlStack, "〇", "一", "二", "三", "四", "五", "六", "七", "八", "九", "十", "百", "千", "万", "亿", "兆", "京", "垓", "秭", "穰");

        /// <summary>
        /// 全形數字
        /// </summary>
        public static FormatterProfile FullwidthWestern { get; } = new(FormatterFlags.DirectTranslate | FormatterFlags.Mapping, "０", "１", "２", "３", "４", "５", "６", "７", "８", "９", null, null, null, null, null, null, null, null, null, null);

        /// <summary>
        /// 半形數字
        /// </summary>
        public static FormatterProfile HalfwidthWestern { get; } = new(FormatterFlags.DirectTranslate, "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", null, null, null, null, null, null, null, null, null, null);




        internal IReadOnlyList<string> Digits { get; }
        internal IReadOnlyList<string> GroupTinyUnits { get; }
        internal IReadOnlyList<string> GroupUnits { get; }
        public FormatterFlags Mode { get; }

        public FormatterProfile(
            FormatterFlags mode,
            string zero,
            string one,
            string two,
            string three,
            string four,
            string five,
            string six,
            string seven,
            string eight,
            string nine,
            string ten,
            string hundred,
            string thousand,
            string tenThousand,
            string hundredMillion,
            string trillion,
            string tenQuadrillion,
            string hundredQuintillion,
            string septillion,
            string tenOctillion)
        {
            Mode = mode;

            Digits = [
                zero,
                one,
                two,
                three,
                four,
                five,
                six,
                seven,
                eight,
                nine
            ];

            GroupTinyUnits = [
                null!,
                ten,
                hundred,
                thousand
            ];

            GroupUnits = [
                null!,
                tenThousand,
                hundredMillion,
                trillion,
                tenQuadrillion,
                hundredQuintillion,
                septillion,
                tenOctillion
            ];
        }

    }

}

