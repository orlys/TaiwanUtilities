namespace TaiwanUtilities;
#if NET7_0_OR_GREATER
using System.Numerics;
#endif

partial struct ChineseNumeric
#if NET7_0_OR_GREATER
    : IMinMaxValue<ChineseNumeric>
#endif
{
    public static ChineseNumeric Zero { get; } = new (0m);

    public static ChineseNumeric MaxValue { get; } = new(decimal.MaxValue);
    public static ChineseNumeric MinValue => Zero;
}
