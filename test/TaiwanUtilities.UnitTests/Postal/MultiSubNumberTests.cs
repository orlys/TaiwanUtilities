namespace TaiwanUtilities.UnitTests;

using Xunit;

/// <summary>
/// 測試多層附號和鄰號解析
/// </summary>
[Collection("DatabaseSingleton")]
public class MultiSubNumberTests
{
    [Fact]
    public void Parse_MultipleSubNumbers_ExtractsAllLayers()
    {
        // 全台唯一的三層附號地址
        var comp = PostalAddress.Parse("台中市中區光復里第30鄰平等街150之1之1之1號");

        Assert.Equal("臺中市", comp.City);
        Assert.Equal("中區", comp.District);
        Assert.Equal("光復里", comp.Village);
        Assert.Equal("30鄰", comp.Neighborhood);
        Assert.Equal("平等街", comp.Road);
        Assert.Equal(150, comp.Number);
        Assert.NotNull(comp.SubNumbers);
        Assert.Equal(3, comp.SubNumbers.Count);
        Assert.Equal(new[] { 1, 1, 1 }, comp.SubNumbers);
        Assert.Equal(1, comp.SubNumbers[0]); // 第一層
    }

    [Fact]
    public void GetFullNumber_MultipleSubNumbers_ReturnsCompleteString()
    {
        var comp = PostalAddress.Parse("台中市中區平等街150之1之1之1號");

        var fullNumber = comp.GetFullNumber();

        Assert.Equal("150之1之1之1號", fullNumber);
    }

    [Theory]
    [InlineData("台北市信義區市府路1之2號", 1, 2, "1之2號")]
    [InlineData("高雄市三民區安寧街3之4之5號", 3, 4, "3之4之5號")]
    [InlineData("台中市中區平等街150之1之1之1號", 150, 1, "150之1之1之1號")]
    public void Parse_VariousSubNumberFormats_WorksCorrectly(
        string address, int expectedNumber, int expectedFirstSubNo, string expectedFullNumber)
    {
        var comp = PostalAddress.Parse(address);

        Assert.Equal(expectedNumber, comp.Number);
        Assert.NotNull(comp.SubNumbers);
        Assert.Equal(expectedFirstSubNo, comp.SubNumbers[0]);
        Assert.Equal(expectedFullNumber, comp.GetFullNumber());
    }

    [Theory]
    [InlineData("第30鄰", "30鄰")]
    [InlineData("30鄰", "30鄰")]
    [InlineData("第1鄰", "1鄰")]
    [InlineData("5鄰", "5鄰")]
    public void Parse_NeighborhoodFormats_ExtractsNumber(string input, string expected)
    {
        var comp = PostalAddress.Parse($"台北市大安區光復里{input}敦化南路1號");

        Assert.Equal(expected, comp.Neighborhood);
    }

    [Fact]
    public void ParseFull_MultipleSubNumbers_ReturnsAllNumbers()
    {
        var addr = new AddressTokenizer("150之1之1之1號");
        var (no, subnos) = addr.Parse(-1); // 最後一個 token

        Assert.Equal(150, no);
        Assert.Equal(3, subnos.Count);
        Assert.Equal(new[] { 1, 1, 1 }, subnos);
    }

    [Fact]
    public void Parse_BackwardCompatibility_SingleSubNumber()
    {
        // 確保單層附號的行為不變
        var comp = PostalAddress.Parse("台北市信義區市府路1之2號");

        Assert.Equal(1, comp.Number);
        Assert.NotNull(comp.SubNumbers);
        Assert.Single(comp.SubNumbers);
        Assert.Equal(2, comp.SubNumbers[0]);
        Assert.Equal("1之2號", comp.GetFullNumber());
    }

    [Fact]
    public void ZipCode_ComplexAddress_ReturnsExactMatch()
    {
                var result = ZipCode.Find("台中市中區光復里第30鄰平等街150之1之1之1號");

        Assert.Equal(ZipCodeResultType.ExactMatch, result.ResultType);
        Assert.Equal("400620", result.ZipCode);
        Assert.NotNull(result.Address);
        Assert.Equal(150, result.Address.Number);
        Assert.Equal(3, result.Address.SubNumbers?.Count);
        Assert.Equal("30鄰", result.Address.Neighborhood);
    }

    [Theory]
    [InlineData("台中市中區平等街150號", "400620")]
    [InlineData("台中市中區平等街150之1號", "400620")]
    [InlineData("台中市中區平等街150之1之1號", "400620")]
    [InlineData("台中市中區平等街150之1之1之1號", "400620")]
    public void Find_VariousSubNumberLevels_ReturnsCorrectZipCode(string address, string expectedZip)
    {
        
        var zipcode = ZipCode.Find(address).ZipCode;

        Assert.Equal(expectedZip, zipcode);
    }
}
