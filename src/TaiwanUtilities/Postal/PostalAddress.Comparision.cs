namespace TaiwanUtilities;
using System;
using System.Diagnostics.CodeAnalysis;
partial class PostalAddress :
    IEquatable<PostalAddress>,
    IComparable<PostalAddress>,
    IComparable
{
    public override int GetHashCode()
    {
        return HashCode.Combine(typeof(PostalAddress), GetRawValue());
    }

    public override bool Equals([NotNullWhen(true)] object obj)
    {
        return obj is PostalAddress cn ? Equals(cn) : base.Equals(obj);
    }

    public bool Equals(PostalAddress other)
    {
        return GetRawValue() == other.GetRawValue();
    }

    public int CompareTo(PostalAddress other)
    {
        return GetRawValue().CompareTo(other.GetRawValue());
    }

    public int CompareTo(object obj)
    {
        Guard.ThrowIfNull(obj);

        if (obj is not PostalAddress cn)
        {
            throw new ArgumentException($"{nameof(obj)} is not a {nameof(PostalAddress)}", nameof(obj));
        }

        return CompareTo(cn);
    }

}
