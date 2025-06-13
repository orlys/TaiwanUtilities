namespace TaiwanUtilities;
using System;
using System.Runtime.CompilerServices;

/// <summary>
/// 表示民國年日期時間結構。
/// </summary>
public readonly partial struct RocDateTime
{

    private readonly DateTimeOffset _value;
    private DateTimeOffset GetRawValue()
    {
        if (_value == default(DateTimeOffset))
        {
            return Era;
        }
        return _value;
    }

    #region Operations
    public RocDateTime AddTicks(long ticks) => GetRawValue().AddTicks(ticks); 
    public RocDateTime AddMilliseconds(double milliseconds) => GetRawValue().AddMilliseconds(milliseconds);
    public RocDateTime AddSeconds(double seconds) => GetRawValue().AddSeconds(seconds);
    public RocDateTime AddMinutes(double minutes) => GetRawValue().AddMinutes(minutes);
    public RocDateTime AddHours(double hours) => GetRawValue().AddHours(hours);
    public RocDateTime AddDays(double days) => GetRawValue().AddDays(days);
    public RocDateTime AddMonths(int months) => GetRawValue().AddMonths(months);
    public RocDateTime AddYears(int years) => GetRawValue().AddYears(years);
    public RocDateTime Add(TimeSpan span) => GetRawValue().Add(span);

    #endregion


    public DateTime ToDateTime() => GetRawValue().DateTime;
    public DateTimeOffset ToDateTimeOffset() => GetRawValue();
    public static RocDateTime From(DateTime dt) => new RocDateTime(dt);
    public static RocDateTime From(DateTimeOffset dto) => new RocDateTime(dto);


    #region Properties 



    /// <summary>
    /// 日期時間是否為民國元年前。
    /// </summary>
    public bool BeforeEra => GetRawValue() < Era.GetRawValue();
     

    /// <summary>
    /// 民國年。此值範圍為 1 至 999，若要判斷是否為民國前需使用 <see cref="BeforeEra"/> 屬性判斷。
    /// </summary>
    public int Year => YearConversion.EraToMinguo(GetRawValue().Year);
    public int Month => GetRawValue().Month;
    public int Day => GetRawValue().Day;
    public int Hour => GetRawValue().Hour;
    public int Minute => GetRawValue().Minute;
    public int Second => GetRawValue().Second;
    public int Millisecond => GetRawValue().Millisecond;

    public long Ticks => GetRawValue().Ticks;

    /// <summary>
    /// 表示今日。
    /// </summary>
    public RocDateTime Date => GetRawValue().Date;

    /// <summary>
    /// 表示一週中的星期名稱。
    /// </summary>
    public DayOfWeek DayOfWeek => GetRawValue().DayOfWeek;

    #endregion



    #region Constructors


    /// <summary>
    /// 透過年月日建立新的民國年物件實體。
    /// </summary>
    /// <param name="year">民國年。此值介於 -999 至 999。此值基於 1，故不支援 0。</param>
    /// <param name="month">月份。此值介於 1 至 12。</param>
    /// <param name="day">日期。此值介於 1 至 31。</param>
    /// <exception cref="ArgumentOutOfRangeException" /> 
    public RocDateTime(int year, int month, int day)
        : this(year, month, day, 0, 0, 0, 0)
    {
    }

    /// <summary>
    /// 透過年月日以及時分秒建立新的民國年物件實體。
    /// </summary>
    /// <param name="year">民國年。此值介於 -999 至 999。此值基於 1，故不支援 0。</param>
    /// <param name="month">月份。此值介於 1 至 12。</param>
    /// <param name="day">日期。此值介於 1 至 31。</param>
    /// <param name="hour">時刻。此值介於 0 至 23。</param>
    /// <param name="minute">分鐘。此值介於 0 至 59。</param>
    /// <param name="second">秒數。此值介於 0 至 59。</param> 
    /// <exception cref="ArgumentOutOfRangeException" />
    public RocDateTime(int year, int month, int day, int hour, int minute, int second)
        : this(year, month, day, hour, minute, second, 0)
    {
    }

    /// <summary>
    /// </summary>
    /// <param name="year">民國年。此值介於 -999 至 999。此值基於 1，故不支援 0。</param>
    /// <param name="month">月份。此值介於 1 至 12。</param>
    /// <param name="day">日期。此值介於 1 至 31。</param>
    /// <param name="hour">時刻。此值介於 0 至 23。</param>
    /// <param name="minute">分鐘。此值介於 0 至 59。</param>
    /// <param name="second">秒數。此值介於 0 至 59。</param>
    /// <param name="millisecond">毫秒數。此值介於 0 至 999。</param>
    /// <exception cref="ArgumentOutOfRangeException" />
    public RocDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond)
    {
        ThrowIfOutOfRange(-999, 999, year);
        ThrowIfOutOfRange(1, 12, month);
        ThrowIfOutOfRange(1, 31, day);
        ThrowIfOutOfRange(0, 23, hour);
        ThrowIfOutOfRange(0, 59, minute);
        ThrowIfOutOfRange(0, 59, second);
        ThrowIfOutOfRange(0, 999, millisecond);

       
        try
        {
            _value = new DateTimeOffset(YearConversion.MinguoToEra(year), month, day, hour, minute, second, millisecond, TimeZoneOffset);
        }
        catch (ArgumentOutOfRangeException)
        {
            throw;
        }


        static void ThrowIfOutOfRange(int min, int max, int value, [CallerArgumentExpression(nameof(value))] string paramName = default)
        {
            if (value < min || value > max)
            {
                throw new ArgumentOutOfRangeException(paramName, value, $"The {paramName} must be between '{min}' and '{max}'.");
            }
        }
    }
     

    /// <summary>
    /// 直接透過西元年建立新的民國年物件實體。
    /// </summary>
    /// <param name="dt">西元年。</param>
    /// <exception cref="ArgumentOutOfRangeException" />
    private RocDateTime(DateTime dt)
        : this(new DateTimeOffset(dt, TimeZoneOffset))
    {
    }


    /// <summary>
    /// 直接透過西元年建立新的民國年物件實體。
    /// </summary>
    /// <param name="dt">西元年。</param>
    /// <exception cref="ArgumentOutOfRangeException" />
    private RocDateTime(DateTimeOffset dt)
    {
        ThrowIfOutOfRange(s_rawMinValue, s_rawMinValue, dt);
        
        _value = dt;

        static void ThrowIfOutOfRange(DateTimeOffset min, DateTimeOffset max, DateTimeOffset value, [CallerArgumentExpression(nameof(value))] string paramName = default)
        {
            if (value < min || value > max)
            {
                throw new ArgumentOutOfRangeException(paramName, value, $"The {paramName} must be between '{min}' and '{max}'.");
            }
        }
    }

    #endregion



    private static class YearConversion
    {

        private const int EraYearOffset = 1911;

        public static int EraToMinguo(int eraYear)
        {
            var value = eraYear > EraYearOffset
                ? eraYear - EraYearOffset
                : 1 + EraYearOffset - eraYear;
            return value;
        }
        public static int MinguoToEra(int minGuoYear)
        {
            // 民國前一年為 1911, 民國一(元)年為 1912
            var value = minGuoYear + EraYearOffset + (minGuoYear < 1 ? 1 : 0);
            return value;
        }


    }
}