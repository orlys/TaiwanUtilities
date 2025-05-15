namespace TaiwanUtilities.UnitTests;

using Xunit;

public partial class ChineseNumericTest
{
    [Fact]
    public static void 預設值()
    {
        Assert.Equal<decimal>(
            expected: 0m,
            actual: default(ChineseNumeric));
    }
}