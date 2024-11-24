namespace TaiwanUtilities;
using System;

#if NET7_0_OR_GREATER
using System.Numerics;
#endif


partial struct RocDateTime
#if NET7_0_OR_GREATER
    : IAdditionOperators<RocDateTime, TimeSpan, RocDateTime>,
    ISubtractionOperators<RocDateTime, TimeSpan, RocDateTime>,
    ISubtractionOperators<RocDateTime, RocDateTime, TimeSpan>
#endif
{

    public static RocDateTime operator +(RocDateTime time, TimeSpan span) => time.Add(span);
    public static RocDateTime operator -(RocDateTime time, TimeSpan span) => time.Add(-span);

    /// <summary>
    /// 計算時間差異
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static TimeSpan operator -(RocDateTime left, RocDateTime right) => left.GetRawValue() - right.GetRawValue();

}

