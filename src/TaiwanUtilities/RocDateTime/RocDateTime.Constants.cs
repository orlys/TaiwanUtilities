namespace TaiwanUtilities;
using System;
using System.Diagnostics;

#if NET7_0_OR_GREATER
using System.Numerics;
using System.Runtime.Serialization;
#endif

partial struct RocDateTime
#if NET7_0_OR_GREATER
    : IMinMaxValue<RocDateTime>
#endif
{
    static RocDateTime()
    {
        TimeZoneOffset = TimeSpan.FromHours(+8);
        MaxValue = new DateTimeOffset(ticks: 918306719999999999L, TimeZoneOffset);
        MinValue = new DateTimeOffset(ticks: 287797536000000001L, TimeZoneOffset);
        Era = new DateTimeOffset(ticks: 603052128000000000L, TimeZoneOffset);
 
    }

    /// <summary>
    /// 設定時間提供者
    /// </summary>
    /// <param name="timeProvider"></param>
    /// <exception cref="ArgumentNullException" />
    public static void SetTimeProvider(TimeProvider timeProvider)
    {
        Guard.ThrowIfNull(timeProvider);
        s_timeProvider = timeProvider;
    }

    /// <summary>
    /// 取得時間提供者
    /// </summary>
    public static TimeProvider TimeProvider => s_timeProvider ??= TimeProvider.System;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private static volatile TimeProvider s_timeProvider;

    /// <summary>
    /// 民國年物件支援的最大值
    /// </summary>
    /// <remarks>
    /// 這個值是西元 2910/12/31 23:59:59.9999999
    /// </remarks>
    /// 
    public static RocDateTime MaxValue { get; }

    /// <summary>
    /// 民國年物件支援的最小值
    /// </summary>
    /// <remarks>
    /// 這個值是西元 0912/12/30 12:00:00 0000001
    /// </remarks> 
    public static RocDateTime MinValue { get; }

    /// <summary>
    /// 取得時區偏移量
    /// </summary>
    /// <remarks>
    /// 這個值是 8 小時
    /// </remarks> 
    public static TimeSpan TimeZoneOffset { get; }

    /// <summary>
    /// 民國元年
    /// </summary>
    /// <remarks>
    /// 這個值是西元 1912/01/01 00:00:00 0000000
    /// </remarks> 
    public static RocDateTime Era { get; }

    /// <summary>
    /// 表示現在時間
    /// </summary>
    public static RocDateTime Now => TimeProvider.GetUtcNow().Add(TimeZoneOffset);

    /// <summary>
    /// 表示今日
    /// </summary>
    
    public static RocDateTime Today => Now.Date;
}