namespace TaiwanUtilities.UnitTests.Postal;

using TaiwanUtilities;
using Xunit;

/// <summary>
/// 驗證「N號之M」慣寫與官方「N之M號」形式解析結果同等。
/// </summary>
public class SubNumberEquivalenceTests
{
    [Theory]
    [InlineData("臺北市信義區市府路2號之3")]
    [InlineData("臺北市信義區市府路2號之三")]
    [InlineData("臺北市信義區市府路2之3號")]
    [InlineData("臺北市信義區市府路2之三號")]
    public void TrailingSubNumberFormsAreEquivalent(string address)
    {
        var parsed = PostalAddress.Parse(address);

        Assert.Equal(2, parsed.Number);
        Assert.NotNull(parsed.SubNumbers);
        Assert.Equal([3], parsed.SubNumbers);
    }

    [Fact]
    public void TrailingSubNumberChainIsPreserved()
    {
        var parsed = PostalAddress.Parse("臺北市信義區市府路150號之1之2");

        Assert.Equal(150, parsed.Number);
        Assert.Equal([1, 2], parsed.SubNumbers);
    }

    [Fact]
    public void FloorSubNumberIsNotRewritten()
    {
        var parsed = PostalAddress.Parse("臺北市信義區市府路2號5樓之3");

        Assert.Equal(2, parsed.Number);
        Assert.Null(parsed.SubNumbers);
        Assert.Equal("5樓", parsed.Floor);
        Assert.Equal(3, parsed.SubFloor);
    }
}
