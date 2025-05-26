
namespace TaiwanUtilities;

using System;
using System.Runtime.CompilerServices;

public readonly partial struct ChineseNumeric
{
    private readonly decimal _value;
     

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private decimal GetRawValue()
    {
        return _value;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <exception cref="ArgumentOutOfRangeException" />
    public ChineseNumeric(decimal value)
    {
        Guard.ThrowIfNegative(value);
        EnsureWholeNumber(value);
        _value = value;
    }

    /// <summary>
    /// 確定沒有小數點
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    private static void EnsureWholeNumber(decimal value)
    {
        if (value != Math.Floor(value))
        {
            // 當有小數點時，拋出例外
            throw new ArgumentOutOfRangeException(nameof(value), "The value must be a whole number.");
        }
    }

    public static string ToString(decimal value, string format)
    {
        return new ChineseNumeric(value).ToString(format);
    }
}
