namespace TaiwanUtilities;
using System;

#if NET7_0_OR_GREATER
using System.Numerics;
#endif

partial struct RocDateTime :
#if NET7_0_OR_GREATER
    IComparisonOperators<RocDateTime, RocDateTime, bool>,
    //IComparisonOperators<RocDateTime, DateTime, bool>,
    //IComparisonOperators<RocDateTime, DateTimeOffset, bool>,
    IEqualityOperators<RocDateTime, RocDateTime, bool>,
    //IEqualityOperators<RocDateTime, DateTime, bool>,
    //IEqualityOperators<RocDateTime, DateTimeOffset, bool>,
#endif
    IComparable<RocDateTime>,
    IEquatable<RocDateTime>,
    IComparable
{
    #region Comparable

    public int CompareTo(object obj)
    {
        return obj switch
        {
            RocDateTime rdt => CompareTo(rdt),
            DateTime dt => CompareTo(dt),
            DateTimeOffset dto => CompareTo(dto),
            _ => throw new ArgumentException($"{nameof(obj)} is not a {nameof(RocDateTime)}, {nameof(DateTime)} or {nameof(DateTimeOffset)}", nameof(obj))
        };
    }

    public int CompareTo(RocDateTime other)
    {
        return GetRawValue().CompareTo(other.GetRawValue());
    }

    public override bool Equals(object obj)
    {
        return obj switch
        {
            RocDateTime rdt => Equals(rdt),
            DateTime dt => Equals(dt),
            DateTimeOffset dto => Equals(dto),
            _ => base.Equals(obj)
        };
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(GetRawValue());
    }

    public bool Equals(RocDateTime other)
    {
        return GetHashCode() == other.GetHashCode();
    }

    #endregion



    public static bool operator >(RocDateTime left, RocDateTime right) => left.GetRawValue() > right.GetRawValue();
    public static bool operator <(RocDateTime left, RocDateTime right) => left.GetRawValue() < right.GetRawValue();
    public static bool operator <=(RocDateTime left, RocDateTime right) => left.GetRawValue() <= right.GetRawValue();
    public static bool operator >=(RocDateTime left, RocDateTime right) => left.GetRawValue() >= right.GetRawValue();

    //public static bool operator >(RocDateTime left, DateTime right) => left.GetRawValue() > right;
    //public static bool operator <(RocDateTime left, DateTime right) => left.GetRawValue() < right;
    //public static bool operator >=(RocDateTime left, DateTime right) => left.GetRawValue() >= right;
    //public static bool operator <=(RocDateTime left, DateTime right) => left.GetRawValue() <= right;

    //public static bool operator >(RocDateTime left, DateTimeOffset right) => left.GetRawValue() > right;
    //public static bool operator <(RocDateTime left, DateTimeOffset right) => left.GetRawValue() < right;
    //public static bool operator <=(RocDateTime left, DateTimeOffset right) => left.GetRawValue() <= right;
    //public static bool operator >=(RocDateTime left, DateTimeOffset right) => left.GetRawValue() >= right;

    public static bool operator ==(RocDateTime left, RocDateTime right) => left.GetRawValue() == right.GetRawValue();
    public static bool operator !=(RocDateTime left, RocDateTime right) => left.GetRawValue() != right.GetRawValue();

    //public static bool operator ==(RocDateTime left, DateTime right) => left.GetRawValue() == right;
    //public static bool operator !=(RocDateTime left, DateTime right) => left.GetRawValue() != right;

    //public static bool operator ==(RocDateTime left, DateTimeOffset right) => left.GetRawValue() == right;
    //public static bool operator !=(RocDateTime left, DateTimeOffset right) => left.GetRawValue() != right;
}
