namespace TaiwanUtilities;

/// <summary>
/// 替換用字元候選清單
/// </summary>
public static class ReplacementCharacters
{
    /// <summary>
    /// 白色圓圈 <see langword="○"/>
    /// </summary>
    private const char WHITE_CIRCLE = '○';
    public static char WhiteCircle => WHITE_CIRCLE;

    /// <summary>
    /// 半形星號 <see langword="*"/>
    /// </summary>
    private const char HALF_WIDTH_ASTERISK = '*';
    public static char HalfWidthAsterisk => HALF_WIDTH_ASTERISK;

    /// <summary>
    ///  全形星號 <see langword="＊"/>
    /// </summary>
    private const char FULL_WIDTH_ASTERISK = '＊';
    public static char FullWidthAsterisk => FULL_WIDTH_ASTERISK;
}