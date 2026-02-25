namespace TaiwanUtilities.UnitTests;

using TaiwanUtilities;

using Xunit;

/// <summary>
/// 地址正規化測試
/// </summary>
public class AddressNormalizationTests
{
    [Theory]
    [InlineData("台北市", "臺北市")]
    [InlineData("台灣", "臺灣")]
    [InlineData("台中市", "臺中市")]
    public void TestTaiwanCharacterNormalization(string input, string expected)
    {
        Assert.Equal(expected, AddressTokenizer.Normalize(input));
    }

    [Theory]
    [InlineData("１２３", "123")]
    [InlineData("４５６７８９０", "4567890")]
    [InlineData("市府路１號", "市府路1號")]
    public void TestFullWidthNumberNormalization(string input, string expected)
    {
        Assert.Equal(expected, AddressTokenizer.Normalize(input));
    }

    [Theory]
    [InlineData("三號", "3號")]
    [InlineData("十一號", "11號")]
    [InlineData("十八號", "18號")]
    [InlineData("三十八號", "38號")]
    [InlineData("九十九號", "99號")]
    // 注意：單獨的"十"和"二十"等不會轉換，因為可能是路名的一部分
    public void TestChineseNumberNormalization(string input, string expected)
    {
        Assert.Equal(expected, AddressTokenizer.Normalize(input));
    }

    [Theory]
    [InlineData("八德路", "八德路")]  // 路名中的數字不轉換
    [InlineData("三元街", "三元街")]
    [InlineData("四維路", "四維路")]
    [InlineData("五權街", "五權街")]
    public void TestRoadNamePreservation(string input, string expected)
    {
        Assert.Equal(expected, AddressTokenizer.Normalize(input));
    }

    [Theory]
    [InlineData("信義路一段", "信義路1段")]
    [InlineData("中山路二段", "中山路2段")]
    [InlineData("敬業一路", "敬業1路")]
    [InlineData("愛富三街", "愛富3街")]
    public void TestRoadSectionNumberConversion(string input, string expected)
    {
        Assert.Equal(expected, AddressTokenizer.Normalize(input));
    }

    [Theory]
    [InlineData("台北市，信義區，市府路1號", "臺北市信義區市府路1號")]
    [InlineData("台北市　信義區　市府路 1 號", "臺北市信義區市府路1號")]
    [InlineData("台北市, 信義區, 市府路 1 號", "臺北市信義區市府路1號")]
    public void TestSeparatorRemoval(string input, string expected)
    {
        Assert.Equal(expected, AddressTokenizer.Normalize(input));
    }

    [Theory]
    [InlineData("市府路1-1號", "市府路1之1號")]
    [InlineData("市府路5~10號", "市府路5之10號")]
    public void TestSubNumberNormalization(string input, string expected)
    {
        Assert.Equal(expected, AddressTokenizer.Normalize(input));
    }
}
