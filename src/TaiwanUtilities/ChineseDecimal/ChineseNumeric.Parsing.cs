namespace TaiwanUtilities;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;


partial struct ChineseNumeric
#if NET7_0_OR_GREATER
    : IParsable<ChineseNumeric>,
    ISpanParsable<ChineseNumeric>
#endif
{
    private enum ParsingErrorKind
    {
        UnknownCharacter,
        InvalidUnitPosition,
        SegmentOverflow
    }

    private static bool ParseCore(ReadOnlySpan<char> str, out ChineseNumeric result, bool throwError)
    {
        result = Zero;
        // 檢查空字串
        if (str.IsWhiteSpace())
        {
            if (throwError)
            {
                throw new ArgumentNullException(nameof(str));
            }
            else
            {
                return false;
            }
        }

        // 檢查是否包含無效字元
        for (var i = 0; i < str.Length; i++)
        {
            var c = str[i];
            if (!Token.ContainsToken(c))
            {
                if (throwError)
                {
                    throw new FormatException($"Invalid character '{c}' at index {i}, reason: Unknown Character")
                    {
                        Data =
                        {
                            [nameof(ParsingErrorKind)] = ParsingErrorKind.UnknownCharacter,
                        }
                    };
                }
                else
                {
                    return false;
                }

            }
        }

        var characters = Tokenizer.Tokenize(str);

        var aggregateRoot = 0m;
        var digitStack = new Stack<decimal>(4);
        var hugeStack = new Stack<decimal>(7);
        var lastTinyMultipler = default(Token);
        var lastHugeMultipler = default(Token);
        var previousUnit = 3;
        var previousIsZero = false;

#if NET_5_0_OR_GREATER
        foreach (var c in CollectionsMarshal.AsSpan(characters))
#else
        foreach (var c in characters)
#endif
        {

            if (c.IsDigit)
            {
                if (previousUnit < 3 &&
                    digitStack.TryPop(out var digit))
                {
                    if (lastTinyMultipler is Token ltm)
                    {
                        lastTinyMultipler = ltm.PreviousToken;
                    }

                    if (previousIsZero)
                    {
                        previousIsZero = false;
                        digitStack.Push(c);
                    }
                    else
                    {
                        digitStack.Push((digit * Token.Ten) + c);
                    }
                }
                else
                {
                    if (lastTinyMultipler is Token ltm && c.IsZero)
                    {
                        lastTinyMultipler = ltm.PreviousToken;
                        previousIsZero = true;
                        digitStack.Push(c);
                    }
                    else
                    {
                        digitStack.Push(c);
                        previousIsZero = false;
                    }
                }
                --previousUnit;
                continue;
            }

            if (c.IsGroupTinyMultipler)
            {

                var log10 = (int)Log10((int)c.Token.Value) - 1;
                if (previousUnit < log10)
                {
                    if (throwError)
                    {
                        throw new FormatException($"Invalid character '{c}' at index {c.Index}, reason: Segment Overflow")
                        {
                            Data =
                            {
                                [nameof(ParsingErrorKind)] = ParsingErrorKind.SegmentOverflow,
                                [nameof(Character)] = c
                            }
                        };
                    }
                    else
                    {
                        return false;
                    }
                }

                if (lastTinyMultipler is Token ltm && ltm.Value < c)
                {
                    if (throwError)
                    {
                        throw new FormatException($"Invalid character '{c}' at index {c.Index}, reason: Invalid Unit Position")
                        {
                            Data =
                            {
                                [nameof(ParsingErrorKind)] = ParsingErrorKind.InvalidUnitPosition,
                                [nameof(Character)] = c
                            }
                        };
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    lastTinyMultipler = c.Token;
                }


                if (digitStack.TryPop(out var digit) && !previousIsZero)
                {
                    aggregateRoot += c * digit;
                }
                else
                {
                    aggregateRoot += c;
                }
                previousIsZero = false;
                previousUnit = 3;
                continue;
            }


            if (c.IsGroupMultipler)
            {
                lastHugeMultipler = c.Token;
                if (!previousIsZero && digitStack.TryPop(out var digit))
                {
                    hugeStack.Push(c * (aggregateRoot + digit));
                }
                else
                {
                    if (aggregateRoot is 0m)
                    {
                        hugeStack.Push(c);
                    }
                    else
                    {
                        hugeStack.Push(c * aggregateRoot);
                    }
                }
                aggregateRoot = 0;
                previousUnit = 3;
                previousIsZero = false;
                lastTinyMultipler = default(Token);
                continue;
            }
        }

        if (digitStack.TryPop(out var digit2))
        {
            if (lastTinyMultipler is Token ltm)
            {
                var prev = ltm.PreviousToken;
                if (prev.IsUnknown)
                {
                    aggregateRoot += digit2;
                }
                else
                {
                    aggregateRoot += digit2 * prev;
                }

            }
            else
            {
                if (lastHugeMultipler is Token lhm)
                {

                    aggregateRoot += digit2 * (lhm / Token.Ten);
                }
                else
                {
                    aggregateRoot += digit2;
                }
            }
        }

        while (hugeStack.TryPop(out var huge))
        {
            aggregateRoot += huge;
        }

        // assign
        result = aggregateRoot;
        return true;



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static decimal Log10(decimal value)
        {
            // LICENSE: MIT
            // SEE: https://github.com/raminrahimzada/CSharp-Helper-Classes/issues/10
            // SOURCE: https://github.com/raminrahimzada/CSharp-Helper-Classes/blob/master/Math/DecimalMath/DecimalMath.cs


            const decimal E_Inv = 0.3678794411714423215955237701614608674458111310317678m;
            const decimal Log10_Inv = 0.434294481903251827651128918916605082294397005803666566114m;
            const decimal E = 2.7182818284590452353602874713526624977572470936999595749m;
             
            Guard.ThrowIfLessThanOrEqual(value, decimal.Zero);

            var count = 0;
            while (value >= decimal.One)
            {
                value *= E_Inv;
                count++;
            }
            while (value <= E_Inv)
            {
                value *= E;
                count--;
            }

            if (--value is decimal.Zero)
            {
                return count;
            }

            var result = decimal.Zero;
            var iteration = 0;
            var y = decimal.One;
            var cacheResult = result - decimal.One;
            while (cacheResult != result &&
                iteration < 100)
            {
                iteration++;
                cacheResult = result;
                y *= -value;
                result += y / iteration;
            }

            return (count - result) * Log10_Inv;
        }
    }


    public static ChineseNumeric Parse(string str)
    {
        return Parse(str, null);
    }
    public static ChineseNumeric Parse(string str, IFormatProvider formatProvider)
    {
        _ = ParseCore(str.AsSpan(), out var result, true);
        return result;
    }



    public static bool TryParse(string str, out ChineseNumeric value)
    {
        return TryParse(str, null, out value);
    }
    public static bool TryParse(string str, IFormatProvider formatProvider, out ChineseNumeric value)
    {
        return ParseCore(str.AsSpan(), out value, false);
    }

    public static ChineseNumeric Parse(ReadOnlySpan<char> str)
    {
        return Parse(str, null);
    }
    public static ChineseNumeric Parse(ReadOnlySpan<char> str, IFormatProvider formatProvider)
    {
        _ = ParseCore(str, out var result, true);
        return result;
    }

    public static bool TryParse(ReadOnlySpan<char> str, out ChineseNumeric value)
    {
        return TryParse(str, null, out value);
    }
    public static bool TryParse(ReadOnlySpan<char> str, IFormatProvider formatProvider, out ChineseNumeric value)
    {
        return ParseCore(str, out value, false);
    }
}


#if NETSTANDARD2_0
[ExcludeFromCodeCoverage]
[DebuggerNonUserCode]
[StackTraceHidden]
internal static class StackExtensions
{
    public static bool TryPop<T>(this Stack<T> stack, out T value)
    {
        if (stack is { Count: > 0 })
        {
            value = stack.Pop();
            return true;
        }
        else
        {
            value = default;
            return false;
        }
    }
}
#endif