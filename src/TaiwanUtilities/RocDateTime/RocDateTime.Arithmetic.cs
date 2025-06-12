namespace TaiwanUtilities;
using System;

#if NET7_0_OR_GREATER
using System.Numerics;
#endif


partial struct RocDateTime
#if NET7_0_OR_GREATER
    : IAdditionOperators<RocDateTime, TimeSpan, RocDateTime>,
    ISubtractionOperators<RocDateTime, TimeSpan, RocDateTime>,
    ISubtractionOperators<RocDateTime, RocDateTime, TimeSpan>,
    ISubtractionOperators<RocDateTime, DateTime, TimeSpan>,
    ISubtractionOperators<RocDateTime, DateTimeOffset, TimeSpan>
#endif
{

    public static RocDateTime operator +(RocDateTime time, TimeSpan span) => time.Add(span);
    public static RocDateTime operator -(RocDateTime time, TimeSpan span) => time.Add(-span);
    public static TimeSpan operator -(RocDateTime left, RocDateTime right) => left.GetRawValue() - right.GetRawValue();
    public static TimeSpan operator -(RocDateTime left, DateTime right) => left.GetRawValue() - right;
    public static TimeSpan operator -(RocDateTime left, DateTimeOffset right) => left.GetRawValue() - right;
}

