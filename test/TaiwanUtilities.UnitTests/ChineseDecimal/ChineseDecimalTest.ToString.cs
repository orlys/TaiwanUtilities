namespace TaiwanUtilities.UnitTests;

using TaiwanUtilities;

using Xunit;

[Trait("Category", "Unit")]
partial class ChineseNumericTest
{
    [Theory]
    [InlineData(1234, "壹仟貳佰參拾肆")]
    [InlineData(1230, "壹仟貳佰參拾")]
    [InlineData(1003, "壹仟零參")]
    [InlineData(101003, "拾萬壹仟零參")]
    [InlineData(1001003, "壹佰萬壹仟零參")]
    [InlineData(1000003, "壹佰萬零參")]
    [InlineData(0, "零")]
    [InlineData(8901, "捌仟玖佰零壹")]
    [InlineData(1_0123_5678, "壹億零壹佰貳拾參萬伍仟陸佰柒拾捌")]
    [InlineData(1_0234_5678, "壹億零貳佰參拾肆萬伍仟陸佰柒拾捌")]
    [InlineData(1, "壹")]
    [InlineData(10, "拾")]
    [InlineData(11, "拾壹")]
    [InlineData(21, "貳拾壹")]
    [InlineData(101, "壹佰零壹")]
    [InlineData(201, "貳佰零壹")]
    [InlineData(100, "壹佰")]
    [InlineData(200, "貳佰")]
    [InlineData(2001, "貳仟零壹")]
    [InlineData(10_0234, "拾萬零貳佰參拾肆")]
    [InlineData(1_0001, "壹萬零壹")]
    [InlineData(1_0000_9999, "壹億零玖仟玖佰玖拾玖")]
    [InlineData(1000, "壹仟")]
    [InlineData(1001, "壹仟零壹")]
    [InlineData(1010, "壹仟零壹拾")]
    [InlineData(1011, "壹仟零壹拾壹")]
    [InlineData(1100, "壹仟壹佰")]
    [InlineData(1101, "壹仟壹佰零壹")]
    [InlineData(1110, "壹仟壹佰壹拾")]
    [InlineData(1111, "壹仟壹佰壹拾壹")]
    [InlineData(1000_0100, "壹仟萬零壹佰")]
    public void 測試從十進位數值轉換為中文(decimal feed, string expected)
    {
        var actual = new ChineseNumeric(feed).ToString("TW");
        Assert.Equal(expected, actual);
    }


}