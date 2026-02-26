namespace TaiwanUtilities;

/// <summary>
/// 提供 <see cref="RocDateTime"/> 格式化所使用的格式字串常數。
/// </summary>
public static class RocDateTimeFormats
{
    /// <summary>
    /// 民國年（例：民國一百一十四年）。
    /// </summary>
    public const string EraYear = "民國年";

    /// <summary>
    /// 年（例：一百一十四年）。
    /// </summary>
    public const string Year = "年";

    /// <summary>
    /// 月（例：二月）。
    /// </summary>
    public const string Month = "月";

    /// <summary>
    /// 日（例：二十七日）。
    /// </summary>
    public const string Day = "日";

    /// <summary>
    /// 時（例：十四時）。
    /// </summary>
    public const string Hour = "時";

    /// <summary>
    /// 分（例：三十分）。
    /// </summary>
    public const string Minute = "分";

    /// <summary>
    /// 秒（例：五秒）。
    /// </summary>
    public const string Second = "秒";

    /// <summary>
    /// 簡短日期（例：114/02/27）。
    /// </summary>
    public const string ShortDate = "d";

    /// <summary>
    /// 完整日期（例：114年2月27日）。
    /// </summary>
    public const string LongDate = "D";

    /// <summary>
    /// 簡短時間（例：14:30:00）。
    /// </summary>
    public const string ShortTime = "t";

    /// <summary>
    /// 完整時間（例：14時30分0秒）。
    /// </summary>
    public const string LongTime = "T";

    /// <summary>
    /// 完整日期/簡短時間（例：114/02/27 14:30:00）。
    /// </summary>
    public const string ShortFull = "f";

    /// <summary>
    /// 完整日期/完整時間（例：114年2月27日14時30分0秒）。
    /// </summary>
    public const string LongFull = "F";

    /// <summary>
    /// 簡短通用模式（無時間時僅日期，有時間時日期+時間）。
    /// </summary>
    public const string ShortGeneral = "g";

    /// <summary>
    /// 完整通用模式（無時間時僅日期，有時間時日期+時間）。
    /// </summary>
    public const string LongGeneral = "G";

    /// <summary>
    /// 民國日期（例：民國一百一十四年二月二十七日）。
    /// </summary>
    public const string EraDate = "民國日期";

    /// <summary>
    /// 日期（例：一百一十四年二月二十七日）。
    /// </summary>
    public const string Date = "日期";

    /// <summary>
    /// 時間（例：十四時三十分零秒）。
    /// </summary>
    public const string Time = "時間";

    /// <summary>
    /// 年月簡短模式（例：114/02）。
    /// </summary>
    public const string ShortYearMonth = "m";

    /// <summary>
    /// 年月完整模式（例：114年02月）。
    /// </summary>
    public const string LongYearMonth = "M";
}
