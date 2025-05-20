namespace TaiwanUtilities;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[ExcludeFromCodeCoverage]
[DebuggerNonUserCode]
[StackTraceHidden]
internal static class Guard
{
    public static void ThrowIfLessThanOrEqual<T>(
        T value,
        T other,
        [CallerArgumentExpression(nameof(value))] string? name = null)
        where T : struct, IComparable<T>
    {
#if NET8_0_OR_GREATER
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, other, name);
        return;
#else

        if (value.CompareTo(other) <= 0)
        {
            throw new ArgumentOutOfRangeException(name, $"The argument must be greater than {other}.");
        }
#endif
    }

    public static void ThrowIfNegative<T>(T value,
        [CallerArgumentExpression(nameof(value))] string? name = null)
#if NET7_0_OR_GREATER
        where T : INumberBase<T>
#else
        where T : struct, IComparable<T>
#endif
    {

#if NET7_0_OR_GREATER
        ArgumentOutOfRangeException.ThrowIfNegative(value, name);
        return;
#else
        if (value.CompareTo(default(T)) < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "The argument must be a non-negative number.");
        }
#endif
    }


    public static void ThrowIfZero<T>(T value,
        [CallerArgumentExpression(nameof(value))] string? name = null)
#if NET7_0_OR_GREATER
        where T : INumberBase<T>
#else
        where T : struct, IComparable<T>
#endif
    {

#if NET7_0_OR_GREATER
        ArgumentOutOfRangeException.ThrowIfZero(value, name);
        return;
#else
        var v = default(T);
        if(Unsafe.As<T, int>(ref v) is 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "The argument must be a non-zero number.");
        }
#endif
    }


    public static void ThrowIfNull<T>(
        [NotNull] T? value,
        [CallerArgumentExpression(nameof(value))] string? name = null)
        where T : class
    {
        if (value is null)
        {
            throw new ArgumentNullException(name);
        }
    }

    public static void ThrowIfNull(
        [NotNull] string? value,
        [CallerArgumentExpression(nameof(value))] string? name = null)
    {
        if (value is null)
        {
            throw new ArgumentNullException(name);
        }

    }


    public static void ThrowIfNullOrEmpty(
        [NotNull] string? value,
        [CallerArgumentExpression(nameof(value))] string? name = null)
    {
        if (value is null)
        {
            throw new ArgumentNullException(name);
        }

        if (value.Length is 0)
        {
            throw new ArgumentException("Argument cannot be empty.", name);
        }
    }


    public static void ThrowIfNullOrWhiteSpace(
       [NotNull] string? value,
       [CallerArgumentExpression(nameof(value))] string? name = null)
    {
#if NET8_0_OR_GREATER
        ArgumentException.ThrowIfNullOrWhiteSpace(value, name);
        return;
#else
        if (value is null)
        {
            throw new ArgumentNullException(name);
        }

        if (value.Length is 0)
        {
            throw new ArgumentException("Argument cannot be empty.", name);
        }

        foreach (var ch in value)
        {
            if (!char.IsWhiteSpace(ch))
            {
                return;
            }
        }

        throw new ArgumentException("Argument cannot be whitespace.", name);
#endif
    }
}