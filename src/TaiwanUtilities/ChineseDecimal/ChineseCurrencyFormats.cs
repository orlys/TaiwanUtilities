namespace TaiwanUtilities;

/// <summary>
/// 提供 <see cref="ChineseCurrency"/> 格式化所使用的格式字串常數。
/// </summary>
public static class ChineseCurrencyFormats
{
    /// <summary>
    /// 一般貨幣格式（例：壹佰貳拾參元壹角整），精度自動偵測。
    /// </summary>
    public const string Dollar = "twd";

    /// <summary>
    /// 支票模式（例：壹佰貳拾參元整），遵循中央銀行規範，角分不計。
    /// </summary>
    public const string Check = "TWD";
}
