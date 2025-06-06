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

        if (Token.Tokenize(str, throwError) is not { } tokens)
        {
            return false;
        }

        var sumRoot = 0m;
        var digitStack = new Stack<decimal>(4);
        var hugeStack = new Stack<decimal>(7);

        var lastTinyMultiplerCharacter = default(Character);
        var lastLargeMultiplierCharacter = default(Character);
        var lastLargeMultiplierToken = default(Token);

        var previousUnit = 3;
        var previousIsZero = false;

        foreach (var token in tokens)
        {
            // 零~九
            if (token.IsDigit)
            {
                if (previousUnit < 3 &&
                    digitStack.TryPop(out var digit))
                {
                    if (lastTinyMultiplerCharacter is { } ltm)
                    {
                        lastTinyMultiplerCharacter = ltm.Previous;
                    }

                    if (previousIsZero)
                    {
                        previousIsZero = false;
                        digitStack.Push(token);
                    }
                    else
                    {
                        digitStack.Push((digit * Character.Ten) + token);
                    }
                }
                else
                {
                    if (lastTinyMultiplerCharacter is { } ltm && token.IsZero)
                    {
                        lastTinyMultiplerCharacter = ltm.Previous;
                        previousIsZero = true;
                        digitStack.Push(token);
                    }
                    else
                    {
                        digitStack.Push(token);
                        previousIsZero = false;
                        if (token.IsZero)
                        {
                            previousUnit++;
                        }
                    }
                }
                --previousUnit;
                continue;
            }

            // 千百十
            if (token.IsTinyMultipler)
            {
                var log10 = (int)Log10((int)token.Character.Value) - 1;
                if (previousUnit < log10)
                {
                    if (throwError)
                    {
                        throw InvalidToken.SegmentOverflow(token, token.Index);
                    }
                    else
                    {
                        return false;
                    }
                }

                if (lastTinyMultiplerCharacter is { } ltm &&
                    ltm.Value < token)
                {
                    if (throwError)
                    {
                        throw InvalidToken.InvalidUnitPosition(token, token.Index);
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    lastTinyMultiplerCharacter = token.Character;
                }


                if (digitStack.TryPop(out var digit) && !previousIsZero)
                {
                    sumRoot += token * digit;
                }
                else
                {
                    sumRoot += token;
                }
                previousIsZero = false;
                previousUnit = 3;
                continue;
            }


            if (token.IsLargeMultiplier)
            {
                lastLargeMultiplierCharacter = token.Character;
                lastLargeMultiplierToken = token;
                if (!previousIsZero && digitStack.TryPop(out var digit))
                {
                    hugeStack.Push(token * (sumRoot + digit));
                }
                else
                {
                    if (sumRoot is 0m)
                    {
                        hugeStack.Push(token);
                    }
                    else
                    {
                        hugeStack.Push(token * sumRoot);
                    }
                }
                sumRoot = 0;
                previousUnit = 3;
                previousIsZero = false;
                lastTinyMultiplerCharacter = default(Character);
                continue;
            }
        }

        if (digitStack.TryPop(out var digit2))
        {
            if (lastTinyMultiplerCharacter is Character ltm)
            {
                var prev = ltm.Previous;
                if (prev.IsUnknown)
                {
                    sumRoot += digit2;
                }
                else
                {
                    sumRoot += digit2 * prev;
                }

            }
            else
            {
                if (lastLargeMultiplierCharacter is Character lhm)
                {
                    if (lastLargeMultiplierToken is { Previous: { IsZero: true } })
                    {
                        if (lastLargeMultiplierToken.Next is { IsZero: true })
                        {
                            // 1000000000
                            sumRoot += digit2 * lhm.Previous;
                        }
                        else
                        {
                            // 1000000001
                            sumRoot += digit2 * (lhm / Character.Ten);
                        }
                    }
                    else if (lastLargeMultiplierToken.Next is { IsZero: true } nextToken)
                    {
                        // 找到下個 large multiplier token
                        FindNext:
                        if (nextToken?.Next is { IsLargeMultiplier: true } n)
                        {
                            nextToken = n;
                        }
                        else
                        {
                            if (nextToken is not null)
                            {
                                nextToken = nextToken.Next;
                                goto FindNext;
                            }
                            else
                            {
                                nextToken = null;
                            }
                        }


                        // 1000000000
                        sumRoot += digit2 * (nextToken ?? 1m);
                    }
                    else
                    {

                        sumRoot += digit2 * (lhm / Character.Ten);
                    }

                }
                else
                {
                    sumRoot += digit2;
                }
            }
        }

        while (hugeStack.TryPop(out var huge))
        {
            sumRoot += huge;
        }

        // assign
        result = sumRoot;
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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException" />
    /// <exception cref="ArgumentOutOfRangeException" />
    /// <exception cref="FormatException" />
    public static ChineseNumeric Parse(string str)
    {
        return Parse(str, null);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="str"></param>
    /// <param name="formatProvider"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException" />
    /// <exception cref="ArgumentOutOfRangeException" />
    /// <exception cref="FormatException" />
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

