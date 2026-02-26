namespace TaiwanUtilities;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

/// <summary>
/// 表示中文貨幣金額的值型別，支援解析與格式化中文貨幣表示法。
/// <list type="bullet">
///   <item><term>Parse</term><description>解析含「元」的中文貨幣字串（如「壹佰貳拾參元壹角整」）</description></item>
///   <item><term>ToString()</term><description>一般貨幣格式（tw$），自動偵測精度</description></item>
///   <item><term>ToString("TW$")</term><description>支票模式，遵循中央銀行規範，角分不計</description></item>
/// </list>
/// </summary>
[DebuggerDisplay("{ToString(),nc}")]
public readonly struct ChineseCurrency :
    IFormattable,
    IEquatable<ChineseCurrency>,
    IComparable<ChineseCurrency>,
    IComparable
{
    private readonly decimal _value;

    public ChineseCurrency(decimal value)
    {
        _value = value;
    }

    public static implicit operator decimal(ChineseCurrency c) => c._value;
    public static implicit operator ChineseCurrency(decimal v) => new(v);
    public static implicit operator ChineseNumeric(ChineseCurrency c) => new(c._value);
    public static implicit operator ChineseCurrency(ChineseNumeric cn) => new((decimal)cn);

    #region Parse

    /// <summary>
    /// 解析中文貨幣字串。字串必須包含「元」或「圓」。
    /// </summary>
    /// <exception cref="ArgumentNullException" />
    /// <exception cref="FormatException" />
    public static ChineseCurrency Parse(string str)
    {
        if (ParseCore(str.AsSpan(), out var result, throwError: true))
        {
            return result;
        }
        throw new FormatException($"Input string '{str}' is not a valid Chinese currency format.");
    }

    public static bool TryParse(string str, out ChineseCurrency value)
    {
        return ParseCore(str.AsSpan(), out value, throwError: false);
    }

    public static ChineseCurrency Parse(ReadOnlySpan<char> str)
    {
        if (ParseCore(str, out var result, throwError: true))
        {
            return result;
        }
        throw new FormatException("Input string is not a valid Chinese currency format.");
    }

    public static bool TryParse(ReadOnlySpan<char> str, out ChineseCurrency value)
    {
        return ParseCore(str, out value, throwError: false);
    }

    private static bool ParseCore(ReadOnlySpan<char> str, out ChineseCurrency result, bool throwError)
    {
        result = default;

        if (ChineseNumeric.Token.Tokenize(str, throwError) is not { } tokens)
        {
            return false;
        }

        // 處理負號前綴
        var isNegative = false;
        if (tokens.Count > 0 && tokens[0].IsNegative)
        {
            isNegative = true;
            tokens.RemoveAt(0);
            if (tokens.Count == 0)
            {
                if (throwError)
                {
                    throw new FormatException("Negative sign must be followed by a number");
                }
                return false;
            }
        }

        // 尋找貨幣單位「元/圓」
        var currencyUnitIndex = -1;
        for (var i = 0; i < tokens.Count; i++)
        {
            if (tokens[i].IsCurrencyUnit)
            {
                currencyUnitIndex = i;
                break;
            }
        }

        if (currencyUnitIndex < 0)
        {
            if (throwError)
            {
                throw new FormatException("Input string does not contain a currency unit (元/圓).");
            }
            return false;
        }

        // 貨幣模式：以「元」分割整數與小數部分
        var integerTokens = currencyUnitIndex > 0 ? tokens.GetRange(0, currencyUnitIndex) : null;
        decimal integerPart = 0m;

        if (integerTokens is { Count: > 0 })
        {
            if (!ChineseNumeric.ParseIntegerTokens(integerTokens, out var integerResult, throwError))
            {
                return false;
            }
            integerPart = (decimal)integerResult;
        }

        // 取「元」之後的 token，濾掉「整」
        var afterYuan = tokens.GetRange(currencyUnitIndex + 1, tokens.Count - currencyUnitIndex - 1);
        afterYuan.RemoveAll(t => t.IsCurrencyTerminator);

        decimal decimalPart = 0m;
        if (afterYuan.Count > 0)
        {
            if (!ParseCurrencyDecimalWithMultipliers(afterYuan, out decimalPart, throwError))
            {
                return false;
            }
        }

        result = isNegative ? -(integerPart + decimalPart) : integerPart + decimalPart;
        return true;
    }

    private static bool ParseCurrencyDecimalWithMultipliers(List<ChineseNumeric.Token> tokens, out decimal result, bool throwError)
    {
        result = 0m;
        var lastMultiplierValue = 1m;

        var i = 0;
        while (i < tokens.Count)
        {
            var token = tokens[i];

            if (token.IsZero)
            {
                // 零角零分 → skip digit+multiplier pair
                if (i + 1 < tokens.Count && (tokens[i + 1].IsCurrencySubUnit || tokens[i + 1].IsDecimalMultiplier))
                {
                    i += 2;
                    continue;
                }
                i++;
                continue;
            }

            if (token.IsDigit)
            {
                if (i + 1 < tokens.Count)
                {
                    var multiplier = tokens[i + 1];

                    if (!multiplier.IsCurrencySubUnit && !multiplier.IsDecimalMultiplier)
                    {
                        if (throwError)
                        {
                            throw new FormatException($"Invalid character '{multiplier.Value}' at index {multiplier.Index}, reason: Unknown character");
                        }
                        return false;
                    }

                    // 貨幣模式統一除以10：角(1→0.1), 分(0.1→0.01), 厘(0.01→0.001)...
                    var multiplierValue = multiplier.Character.Value / 10m;

                    if (multiplierValue >= lastMultiplierValue)
                    {
                        if (throwError)
                        {
                            throw new FormatException($"Invalid character '{multiplier.Value}' at index {multiplier.Index}, reason: Invalid unit position");
                        }
                        return false;
                    }
                    lastMultiplierValue = multiplierValue;
                    result += token.Character.Value * multiplierValue;
                    i += 2;
                }
                else
                {
                    if (throwError)
                    {
                        throw new FormatException($"Invalid character '{token.Value}' at index {token.Index}, reason: Unknown character");
                    }
                    return false;
                }
            }
            else
            {
                if (throwError)
                {
                    throw new FormatException($"Invalid character '{token.Value}' at index {token.Index}, reason: Unknown character");
                }
                return false;
            }
        }

        return true;
    }

    #endregion

    #region ToString

    private static readonly string[] s_currencySubUnits = ["角", "分", "厘", "毫", "絲", "忽", "微", "纖", "沙", "塵"];

    /// <summary>
    /// 以一般貨幣格式（tw$）輸出。
    /// </summary>
    public override string ToString()
    {
        return FormatCurrency(_value, checkMode: false);
    }

    /// <summary>
    /// <list type="bullet">
    ///   <item><term>TWD</term><description>支票大寫（遵循中央銀行規範，角分不計）</description></item>
    ///   <item><term>twd</term><description>一般貨幣大寫（精度自動偵測）</description></item>
    /// </list>
    /// </summary>
    /// <seealso cref="ChineseCurrencyFormats"/>
    public string ToString(string format)
    {
        return ToString(format, null);
    }

    public string ToString(string format, IFormatProvider formatProvider)
    {
        return format switch
        {
            ChineseCurrencyFormats.Check => FormatCurrency(_value, checkMode: true),
            ChineseCurrencyFormats.Dollar or null or "" => FormatCurrency(_value, checkMode: false),
            _ => _value.ToString(format, formatProvider),
        };
    }

    internal static string FormatCurrency(decimal value, bool checkMode)
    {
        var profile = ChineseNumeric.FormatterProfile.TraditionalUppercase;

        var sb = new StringBuilder();

        var isNegative = value < 0;
        if (isNegative)
        {
            value = -value;
            sb.Append(profile.NegativeSign);
        }

        if (checkMode)
        {
            // 支票模式：四捨五入到元
            value = Math.Round(value, 0, MidpointRounding.AwayFromZero);
        }

        var integerPart = Math.Truncate(value);
        var fractionalPart = value - integerPart;

        // 整數部分
        if (integerPart == 0)
        {
            sb.Append(profile.Digits[0]);
        }
        else
        {
            var integerStr = new ChineseNumeric.Builder().ToString(integerPart, profile);

            // 貨幣格式要求「壹拾」而非「拾」
            if (integerStr.StartsWith(profile.GroupTinyUnits[1], StringComparison.Ordinal))
            {
                integerStr = profile.Digits[1] + integerStr;
            }

            sb.Append(integerStr);
        }

        sb.Append("元");

        if (checkMode || fractionalPart == 0)
        {
            sb.Append("整");
            return sb.ToString();
        }

        // 小數部分：逐位取出，自動偵測精度
        var remaining = fractionalPart;
        var lastUnitIndex = -1;
        for (var i = 0; i < s_currencySubUnits.Length; i++)
        {
            remaining *= 10;
            var digit = (int)Math.Truncate(remaining);
            remaining -= digit;

            if (digit > 0)
            {
                sb.Append(profile.Digits[digit]);
                sb.Append(s_currencySubUnits[i]);
                lastUnitIndex = i;
            }

            if (remaining == 0)
            {
                break;
            }
        }

        // 角後無分時補「整」（最後輸出的單位是「角」）
        if (lastUnitIndex == 0)
        {
            sb.Append("整");
        }

        return sb.ToString();
    }

    #endregion

    #region Equality & Comparison

    public override int GetHashCode()
    {
        return HashCode.Combine(typeof(ChineseCurrency), _value);
    }

    public override bool Equals([NotNullWhen(true)] object obj)
    {
        return obj is ChineseCurrency c && Equals(c);
    }

    public bool Equals(ChineseCurrency other)
    {
        return _value == other._value;
    }

    public int CompareTo(ChineseCurrency other)
    {
        return _value.CompareTo(other._value);
    }

    public int CompareTo(object obj)
    {
        Guard.ThrowIfNull(obj);

        if (obj is not ChineseCurrency c)
        {
            throw new ArgumentException($"{nameof(obj)} is not a {nameof(ChineseCurrency)}", nameof(obj));
        }

        return CompareTo(c);
    }

    public static bool operator ==(ChineseCurrency left, ChineseCurrency right) => left.Equals(right);
    public static bool operator !=(ChineseCurrency left, ChineseCurrency right) => !left.Equals(right);
    public static bool operator <(ChineseCurrency left, ChineseCurrency right) => left._value < right._value;
    public static bool operator >(ChineseCurrency left, ChineseCurrency right) => left._value > right._value;
    public static bool operator <=(ChineseCurrency left, ChineseCurrency right) => left._value <= right._value;
    public static bool operator >=(ChineseCurrency left, ChineseCurrency right) => left._value >= right._value;

    #endregion
}
