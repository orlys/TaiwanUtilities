namespace TaiwanUtilities.UnitTests;

using TaiwanUtilities;

using Xunit;

/// <summary>
/// 地址分詞測試
/// </summary>
public class AddressTokenizationTests
{
    [Fact]
    public void TestBasicAddressTokenization()
    {
        var addr = new AddressTokenizer("臺北市大安區市府路1號");

        Assert.Equal(4, addr.Tokens.Count);
        Assert.Equal(new[] { "", "", "臺北", "市" }, addr.Tokens[0]);
        Assert.Equal(new[] { "", "", "大安", "區" }, addr.Tokens[1]);
        Assert.Equal(new[] { "", "", "市府", "路" }, addr.Tokens[2]);
        Assert.Equal(new[] { "1", "", "", "號" }, addr.Tokens[3]);
    }

    [Fact]
    public void TestAddressWithSubNumber()
    {
        var addr = new AddressTokenizer("臺北市大安區市府路1之1號");

        Assert.Equal(4, addr.Tokens.Count);
        Assert.Equal("1", addr.Tokens[3][AddressTokenizer.NO]);
        Assert.Equal("之1", addr.Tokens[3][AddressTokenizer.SUBNO]);
    }

    [Fact]
    public void TestAddressWithAlley()
    {
        var addr = new AddressTokenizer("臺北市大安區市府路1巷2號");

        Assert.Equal(5, addr.Tokens.Count);
        Assert.Equal(new[] { "", "", "市府", "路" }, addr.Tokens[2]);
        Assert.Equal(new[] { "1", "", "", "巷" }, addr.Tokens[3]);
        Assert.Equal(new[] { "2", "", "", "號" }, addr.Tokens[4]);
    }

    [Fact]
    public void TestAddressWithLane()
    {
        var addr = new AddressTokenizer("臺北市大安區市府路1巷2弄3號");

        Assert.Equal(6, addr.Tokens.Count);
        Assert.Equal(new[] { "1", "", "", "巷" }, addr.Tokens[3]);
        Assert.Equal(new[] { "2", "", "", "弄" }, addr.Tokens[4]);
        Assert.Equal(new[] { "3", "", "", "號" }, addr.Tokens[5]);
    }

    [Fact]
    public void TestAddressWithFloor()
    {
        var addr = new AddressTokenizer("臺北市大安區市府路1號2樓");

        Assert.Equal(5, addr.Tokens.Count);
        Assert.Equal(new[] { "1", "", "", "號" }, addr.Tokens[3]);
        Assert.Equal(new[] { "2", "", "", "樓" }, addr.Tokens[4]);
    }

    [Fact]
    public void TestCountyTownVillage()
    {
        var addr = new AddressTokenizer("桃園縣中壢市普義");

        Assert.Equal(3, addr.Tokens.Count);
        Assert.Equal(new[] { "", "", "桃園", "縣" }, addr.Tokens[0]);
        Assert.Equal(new[] { "", "", "中壢", "市" }, addr.Tokens[1]);
        Assert.Equal(new[] { "", "", "普義", "" }, addr.Tokens[2]);
    }

    [Fact]
    public void TestRoadWithNumber()
    {
        var addr = new AddressTokenizer("臺北市中山區敬業1路10號");

        Assert.Equal(4, addr.Tokens.Count);
        Assert.Equal(new[] { "", "", "敬業一", "路" }, addr.Tokens[2]);
        Assert.Equal(new[] { "10", "", "", "號" }, addr.Tokens[3]);
    }

    [Theory]
    [InlineData("臺北市信義區", 2)]
    [InlineData("臺北市信義區市府路", 3)]
    [InlineData("臺北市信義區市府路1號", 4)]
    [InlineData("臺北市信義區市府路1巷2號", 5)]
    [InlineData("臺北市信義區市府路1巷2弄3號", 6)]
    public void TestTokenCount(string address, int expectedCount)
    {
        var addr = new AddressTokenizer(address);
        Assert.Equal(expectedCount, addr.Tokens.Count);
    }
}
