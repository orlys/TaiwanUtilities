namespace TaiwanUtilities.UnitTests;

using System;

using TaiwanUtilities;

using Xunit;

[Trait("Category", "Unit")]
partial class ChineseNumericTest
{
    [Theory]
    [InlineData("三千零七十三", "3073")]
    [InlineData("一一百一一", "1111")]
    [InlineData("一千二百零四", "1204")]
    [InlineData("一千零一十", "1010")]
    [InlineData("一千零一十一", "1011")]
    [InlineData("一千零一", "1001")]
    [InlineData("一千一百一十一", "1111")]
    [InlineData("千一百一十一", "1111")]
    [InlineData("千百十", "1110")]
    [InlineData("一一一一", "1111")]
    [InlineData("一一一", "111")]
    [InlineData("十一", "11")]
    [InlineData("千", "1000")]
    [InlineData("千十", "1010")]
    [InlineData("一千百十", "1110")]
    [InlineData("一一一十", "1110")]
    [InlineData("一千一十", "1010")]
    [InlineData("一千百一", "1110")]
    [InlineData("一一百", "1100")]
    [InlineData("零", "0")]
    [InlineData("零一", "1")]
    [InlineData("零零一二", "12")]
    [InlineData("零零一十二", "12")]
    [InlineData("零零一", "1")]
    [InlineData("零零零一", "1")]
    [InlineData("零零零零", "0")]
    [InlineData("零十", "0")]
    [InlineData("零零十", "0")]
    [InlineData("零零零十", "0")]
    [InlineData("零零十二", "2")]
    [InlineData("一千七百五", "1750")]
    [InlineData("一千二百三十四", "1234")]
    [InlineData("一千二百四", "1240")]
    [InlineData("一一一一一一", "111111")]
    [InlineData("六百九十七", "697")]
    [InlineData("三三", "33")]
    [InlineData("一十一", "11")]
    [InlineData("八百八", "880")]
    [InlineData("六百五", "650")]
    [InlineData("四十四", "44")]
    [InlineData("六九七", "697")]
    [InlineData("一千二百三十零", "1230")]
    [InlineData("一千二百三十", "1230")]
    [InlineData("一千四", "1400")]
    [InlineData("一一", "11")]
    [InlineData("七百", "700")]
    [InlineData("二億", "200000000")]
    [InlineData("一千六百五十萬", "16500000")]
    [InlineData("一千六百五十萬三千零七十三", "16503073")]
    [InlineData("一千零萬", "10000000")]
    [InlineData("七千零萬一二百五二", "70001252")]
    [InlineData("三萬五", "35000")]
    [InlineData("兆百億", "1010000000000")]
    [InlineData("百萬", "1000000")]
    [InlineData("億百萬", "101000000")]
    [InlineData("一億", "100000000")]
    [InlineData("一千億", "100000000000")]
    [InlineData("億", "100000000")]
    [InlineData("五千七百億", "570000000000")]
    [InlineData("五億七千萬", "570000000")]
    [InlineData("七千七百萬", "77000000")]
    [InlineData("八千六百萬", "86000000")]
    [InlineData("一萬一千二", "11200")]
    [InlineData("一萬一千二佰", "11200")]
    [InlineData("一萬一千零二", "11002")]
    [InlineData("八千萬零五十", "80000050")]
    [InlineData("一百億四千萬二千", "10040002000")]
    [InlineData("貳拾", "20")]
    [InlineData("二一萬", "210000")]
    [InlineData("一十一萬", "110000")]
    [InlineData("千零一", "1001")]
    [InlineData("千零十", "1010")]
    [InlineData("千百", "1100")]
    [InlineData("壹壹肆伍壹肆", "114514")]
    public static void 從中文轉換為正確十進位數值(string input, string expectedDecimal)
    {
        Assert.Equal<decimal>(
            expected: decimal.Parse(expectedDecimal),
            actual: ChineseNumeric.Parse(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(" \t")]
    public static void 空字串(string emptyString)
    {
        Assert.Throws<ArgumentNullException>(delegate { ChineseNumeric.Parse(emptyString); });
    }


    [Theory]
    [InlineData("其他")]
    [InlineData("三點")]
    [InlineData("三又")]
    [InlineData("一零一百")]
    [InlineData("一一一百")]
    [InlineData("一一一千")]
    [InlineData("一百千")]
    public static void 解析時發生錯誤(string badFormat)
    {
        Assert.Throws<FormatException>(delegate { ChineseNumeric.Parse(badFormat); });
    }

    [Theory]
    [InlineData("一二三四五點五四三二一", "12345.54321")]
    [InlineData("一二三四五點五四三二一零", "12345.543210")]
    [InlineData("一萬二千三百四十五點五四三二一", "12345.54321")]
    [InlineData("五點三二一", "5.321")]
    public static void 從中文轉換為正確且包含小數之十進位數值(string input, string expectedDecimal)
    {
        Assert.Equal<decimal>(
            expected: decimal.Parse(expectedDecimal),
            actual: ChineseNumeric.Parse(input));
    }

    [Theory]
    [InlineData("五又三分二釐一毫", "5.321")]
    [InlineData("一百又五分", "100.5")]
    [InlineData("一萬二千三百四十五又五分四釐三毫二絲一忽", "12345.54321")]
    [InlineData("三分", "0.3")]
    [InlineData("二釐", "0.02")]
    [InlineData("一毫", "0.001")]
    [InlineData("五又三分", "5.3")]
    [InlineData("十又一分五釐", "10.15")]
    [InlineData("三又七釐", "3.07")]
    [InlineData("二又四毫", "2.004")]
    [InlineData("三絲", "0.0003")]
    [InlineData("七忽", "0.00007")]
    [InlineData("五又三分二釐一毫四絲五忽", "5.32145")]
    [InlineData("一又一忽", "1.00001")]
    public static void 從又分釐毫格式轉換為正確十進位數值(string input, string expectedDecimal)
    {
        Assert.Equal<decimal>(
            expected: decimal.Parse(expectedDecimal),
            actual: ChineseNumeric.Parse(input));
    }

    [Theory]
    [InlineData("負三十一", "-31")]
    [InlineData("負一百二十三", "-123")]
    [InlineData("負零", "0")]
    [InlineData("負一萬", "-10000")]
    [InlineData("負零點伍", "-0.5")]
    [InlineData("負五又三分二釐一毫", "-5.321")]
    [InlineData("负四十二", "-42")]
    public static void 負數解析(string input, string expectedDecimal)
    {
        Assert.Equal<decimal>(
            expected: decimal.Parse(expectedDecimal),
            actual: ChineseNumeric.Parse(input));
    }

    [Fact]
    public static void 負號後無數字_應拋出例外()
    {
        Assert.Throws<FormatException>(() => ChineseNumeric.Parse("負"));
    }
}