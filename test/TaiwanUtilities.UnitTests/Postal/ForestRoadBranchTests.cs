namespace TaiwanUtilities.UnitTests;

using Xunit;
using Xunit.Abstractions;

public class ForestRoadBranchTests
{
    private readonly ITestOutputHelper _output;

    public ForestRoadBranchTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void TestForestRoadBranchTokenization()
    {
        // 測試「嘉專1-1線」（林道支線）的 tokenization
        var addr = new AddressTokenizer("宜蘭縣大同鄉太平村嘉專1-1線中間1號");

        _output.WriteLine($"Token 數量: {addr.Tokens.Count}");
        for (int i = 0; i < addr.Tokens.Count; i++)
        {
            var token = addr.Tokens[i];
            _output.WriteLine($"Token[{i}]: NO={token[AddressTokenizer.NO]}, SUBNO={token[AddressTokenizer.SUBNO]}, NAME={token[AddressTokenizer.NAME]}, UNIT={token[AddressTokenizer.UNIT]}");
        }

        // 驗證基本結構
        Assert.True(addr.Tokens.Count >= 5, "應該至少有 5 個 tokens");
    }

    [Fact]
    public void Parse_ForestRoadBranch_ParsesCorrectly()
    {
        // Arrange & Act
        var addr = PostalAddress.Parse("宜蘭縣大同鄉太平村嘉專1-1線中間1號");

        // Assert
        Assert.Equal("宜蘭縣", addr.City);
        Assert.Equal("大同鄉", addr.District);
        Assert.Equal("太平村", addr.Village);

        // 輸出實際結果以供檢查
        _output.WriteLine($"Road: {addr.Road ?? "(null)"}");
        _output.WriteLine($"Locality: {addr.Locality ?? "(null)"}");
        _output.WriteLine($"Number: {addr.Number}");
        _output.WriteLine($"NormalizedAddress: {addr.NormalizedAddress}");
    }

    [Theory]
    [InlineData("宜蘭縣大同鄉太平村嘉專1-1線中間1號", "嘉專1-1線", "中間")]  // 林道支線 + 地區
    [InlineData("花蓮縣秀林鄉富世村台8-1線291號", "臺8-1線", null)]        // 公路支線（台→臺）
    [InlineData("南投縣信義鄉人和村台21-1線96號", "臺21-1線", null)]       // 公路支線
    public void Parse_ForestRoadBranch_ExtractsAllComponents(
        string address, string expectedRoad, string? expectedLocation)
    {
        // Act
        var addr = PostalAddress.Parse(address);

        // Assert
        _output.WriteLine($"地址: {address}");
        _output.WriteLine($"Road: {addr.Road ?? "(null)"}");
        _output.WriteLine($"Locality: {addr.Locality ?? "(null)"}");
        _output.WriteLine($"期望Road: {expectedRoad}");
        _output.WriteLine($"期望Location: {expectedLocation ?? "(null)"}");

        Assert.Equal(expectedRoad, addr.Road);
        Assert.Equal(expectedLocation, addr.Locality);
    }
}
