namespace TaiwanUtilities;
using System;
using System.Diagnostics.CodeAnalysis;
partial struct ChineseNumeric :
    IEquatable<ChineseNumeric>,
    IComparable<ChineseNumeric>,
    IComparable
{
    public override int GetHashCode()
    {
        return HashCode.Combine(typeof(ChineseNumeric), GetRawValue());
    }

    public override bool Equals([NotNullWhen(true)] object obj)
    {
        return obj is ChineseNumeric cn ? Equals(cn) : base.Equals(obj);
    }



    public bool Equals(ChineseNumeric other)
    {
        return GetRawValue() == other.GetRawValue();
    }

    public int CompareTo(ChineseNumeric other)
    {
        return GetRawValue().CompareTo(other.GetRawValue());
    }

    public int CompareTo(object obj)
    {
        Guard.ThrowIfNull(obj);

        if (obj is not ChineseNumeric cn)
        {
            throw new ArgumentException($"{nameof(obj)} is not a {nameof(ChineseNumeric)}", nameof(obj));
        }

        return CompareTo(cn);
    }

}
