
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
    public ChineseNumeric(decimal value)
    {
        _value = value;
    }

    public static string ToString(decimal value, string format)
    {
        return new ChineseNumeric(value).ToString(format);
    }
}
