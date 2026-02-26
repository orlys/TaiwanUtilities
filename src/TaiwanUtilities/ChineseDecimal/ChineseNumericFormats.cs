namespace TaiwanUtilities;

/// <summary>
/// 提供 <see cref="ChineseNumeric"/> 格式化所使用的格式字串常數。
/// </summary>
public static class ChineseNumericFormats
{
    /// <summary>
    /// 繁體小寫（例：一百二十三）。
    /// </summary>
    public const string TraditionalLowercase = "tw";

    /// <summary>
    /// 繁體大寫（例：壹佰貳拾參）。
    /// </summary>
    public const string TraditionalUppercase = "TW";

    /// <summary>
    /// 簡體小寫（例：一百二十三）。
    /// </summary>
    public const string SimplifiedLowercase = "cn";

    /// <summary>
    /// 簡體大寫（例：壹佰贰拾参）。
    /// </summary>
    public const string SimplifiedUppercase = "CN";

    /// <summary>
    /// 半形數字（例：123）。
    /// </summary>
    public const string HalfWidth = "HW";

    /// <summary>
    /// 全形數字（例：１２３）。
    /// </summary>
    public const string FullWidth = "FW";
}
