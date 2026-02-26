namespace TaiwanUtilities.UnitTests;

using TaiwanUtilities;

using Xunit;

[Trait("Category", "Unit")]
public class ChineseCurrencyTest
{
    #region Parse

    [Theory]
    [InlineData("一百二十三元一角", "123.1")]
    [InlineData("壹佰貳拾參元壹角", "123.1")]
    [InlineData("一百元整", "100")]
    [InlineData("五元三角", "5.3")]
    [InlineData("一百二十三元一角二分", "123.12")]
    [InlineData("一百元一角二分三厘", "100.123")]
    [InlineData("壹佰貳拾參元壹角整", "123.1")]
    [InlineData("一元", "1")]
    [InlineData("一百二十三元零角零分", "123")]
    [InlineData("一萬元整", "10000")]
    [InlineData("壹佰元整", "100")]
    [InlineData("伍圓參角", "5.3")]
    public void 從貨幣格式轉換為正確十進位數值(string input, string expectedDecimal)
    {
        Assert.Equal<decimal>(
            expected: decimal.Parse(expectedDecimal),
            actual: ChineseCurrency.Parse(input));
    }

    [Theory]
    [InlineData("一百二十三")]
    [InlineData("五")]
    [InlineData("零")]
    public void 不含元的字串應拋出例外(string input)
    {
        Assert.Throws<FormatException>(() => ChineseCurrency.Parse(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void 空字串應拋出例外(string input)
    {
        Assert.Throws<ArgumentNullException>(() => ChineseCurrency.Parse(input));
    }

    [Theory]
    [InlineData("一百二十三")]
    [InlineData("五")]
    public void TryParse_不含元的字串應回傳false(string input)
    {
        Assert.False(ChineseCurrency.TryParse(input, out _));
    }

    [Theory]
    [InlineData("一百元整", "100")]
    [InlineData("五元三角", "5.3")]
    public void TryParse_正確格式應回傳true(string input, string expectedDecimal)
    {
        Assert.True(ChineseCurrency.TryParse(input, out var result));
        Assert.Equal(decimal.Parse(expectedDecimal), (decimal)result);
    }

    #endregion

    #region ToString

    [Theory]
    [InlineData(1234.56, "壹仟貳佰參拾肆元伍角陸分")]
    [InlineData(1234, "壹仟貳佰參拾肆元整")]
    [InlineData(0, "零元整")]
    [InlineData(1.01, "壹元壹分")]
    [InlineData(1.10, "壹元壹角整")]
    [InlineData(-1234.56, "負壹仟貳佰參拾肆元伍角陸分")]
    [InlineData(1234.5, "壹仟貳佰參拾肆元伍角整")]
    [InlineData(1234.567, "壹仟貳佰參拾肆元伍角陸分柒厘")]
    [InlineData(1234.5678, "壹仟貳佰參拾肆元伍角陸分柒厘捌毫")]
    [InlineData(1234.56789, "壹仟貳佰參拾肆元伍角陸分柒厘捌毫玖絲")]
    [InlineData(100000000, "壹億元整")]
    [InlineData(10.05, "壹拾元伍分")]
    [InlineData(0.50, "零元伍角整")]
    [InlineData(1000, "壹仟元整")]
    [InlineData(0.001, "零元壹厘")]
    [InlineData(0.0001, "零元壹毫")]
    [InlineData(0.00001, "零元壹絲")]
    public void 一般貨幣格式化(double feed, string expected)
    {
        var c = new ChineseCurrency((decimal)feed);
        Assert.Equal(expected, c.ToString());
    }

    [Theory]
    [InlineData(1234.56, "壹仟貳佰參拾伍元整")]
    [InlineData(1234, "壹仟貳佰參拾肆元整")]
    [InlineData(0, "零元整")]
    [InlineData(-1234.56, "負壹仟貳佰參拾伍元整")]
    [InlineData(100000000, "壹億元整")]
    [InlineData(10.05, "壹拾元整")]
    [InlineData(1000, "壹仟元整")]
    [InlineData(0.50, "壹元整")]
    [InlineData(0.49, "零元整")]
    public void 支票貨幣格式化(double feed, string expected)
    {
        var c = new ChineseCurrency((decimal)feed);
        Assert.Equal(expected, c.ToString(ChineseCurrencyFormats.Check));
    }

    [Theory]
    [InlineData(1234.56, "壹仟貳佰參拾肆元伍角陸分")]
    [InlineData(1234, "壹仟貳佰參拾肆元整")]
    public void ToString_tw_dollar_格式(double feed, string expected)
    {
        var c = new ChineseCurrency((decimal)feed);
        Assert.Equal(expected, c.ToString(ChineseCurrencyFormats.Dollar));
    }

    #endregion

    #region Round-trip

    [Theory]
    [InlineData("123.1")]
    [InlineData("100")]
    [InlineData("5.3")]
    [InlineData("123.12")]
    [InlineData("1")]
    [InlineData("10000")]
    [InlineData("99.99")]
    public void 貨幣格式RoundTrip(string decimalStr)
    {
        var value = decimal.Parse(decimalStr);
        var currency = new ChineseCurrency(value);
        var formatted = currency.ToString();
        var parsed = ChineseCurrency.Parse(formatted);
        Assert.Equal(value, (decimal)parsed);
    }

    #endregion

    #region Implicit Conversions

    [Fact]
    public void 隱式轉換_decimal到ChineseCurrency()
    {
        ChineseCurrency c = 123.45m;
        Assert.Equal(123.45m, (decimal)c);
    }

    [Fact]
    public void 隱式轉換_ChineseCurrency到decimal()
    {
        var c = new ChineseCurrency(123.45m);
        decimal d = c;
        Assert.Equal(123.45m, d);
    }

    [Fact]
    public void 隱式轉換_ChineseNumeric到ChineseCurrency()
    {
        var cn = new ChineseNumeric(100m);
        ChineseCurrency c = cn;
        Assert.Equal(100m, (decimal)c);
    }

    [Fact]
    public void 隱式轉換_ChineseCurrency到ChineseNumeric()
    {
        var c = new ChineseCurrency(100m);
        ChineseNumeric cn = c;
        Assert.Equal(100m, (decimal)cn);
    }

    #endregion

    #region Equality

    [Fact]
    public void 相等性測試()
    {
        var a = new ChineseCurrency(100m);
        var b = new ChineseCurrency(100m);
        var c = new ChineseCurrency(200m);

        Assert.True(a.Equals(b));
        Assert.False(a.Equals(c));
        Assert.True(a == b);
        Assert.True(a != c);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void 比較測試()
    {
        var small = new ChineseCurrency(10m);
        var large = new ChineseCurrency(100m);

        Assert.True(small < large);
        Assert.True(large > small);
        Assert.True(small.CompareTo(large) < 0);
    }

    #endregion
}
