namespace TaiwanUtilities.UnitTests;

using Xunit;
using Xunit.Abstractions;

[Collection("DatabaseSingleton")]
public class DuplicateNameTests
{
    private readonly ITestOutputHelper _output;

    public DuplicateNameTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void TestFushiAddressTokenization()
    {
        // 測試「花蓮縣秀林鄉富世村富世291號」的 tokenization
        // 「富世」重複出現：一次是村名，一次是路名
        var addr = new AddressTokenizer("花蓮縣秀林鄉富世村富世291號");

        _output.WriteLine($"正規化地址: {addr.Flat()}");
        _output.WriteLine($"Token 數量: {addr.Tokens.Count}");

        for (int i = 0; i < addr.Tokens.Count; i++)
        {
            var token = addr.Tokens[i];
            _output.WriteLine($"Token[{i}]: NO={token[AddressTokenizer.NO]}, SUBNO={token[AddressTokenizer.SUBNO]}, NAME={token[AddressTokenizer.NAME]}, UNIT={token[AddressTokenizer.UNIT]}");
        }
    }

    [Fact]
    public void Parse_FushiAddress_ExtractsAllComponents()
    {
        // Arrange & Act
        var addr = PostalAddress.Parse("花蓮縣秀林鄉富世村富世291號");

        // Assert - 輸出結果
        _output.WriteLine($"原始地址:     {addr.RawAddress}");
        _output.WriteLine($"正規化地址:   {addr.NormalizedAddress}");
        _output.WriteLine($"縣市:         {addr.City ?? "(null)"}");
        _output.WriteLine($"行政區:       {addr.District ?? "(null)"}");
        _output.WriteLine($"村:           {addr.Village ?? "(null)"}");
        _output.WriteLine($"路街:         {addr.Road ?? "(null)"}");
        _output.WriteLine($"Locality: {addr.Locality ?? "(null)"}");
        _output.WriteLine($"門牌號:       {addr.Number?.ToString() ?? "(null)"}");

        // 基本斷言
        Assert.Equal("花蓮縣", addr.City);
        Assert.Equal("秀林鄉", addr.District);
        Assert.Equal("富世村", addr.Village);
        Assert.Equal(291, addr.Number);

        // 「富世」應該被識別為什麼？
        // 選項1: Road = "富世" (沒有單位字的路名)
        // 選項2: Locality = "富世" (地區名稱)
        // 選項3: 被忽略（因為與村名重複）
    }

    [Theory]
    [InlineData("花蓮縣秀林鄉富世村富世291號")]
    [InlineData("花蓮縣秀林鄉富世村台8線291號")]        // 對照：有明確路名
    [InlineData("花蓮縣秀林鄉富世村台8-1線291號")]      // 對照：公路支線
    public void Parse_FushiVariousFormats_ComparesResults(string address)
    {
        // Act
        var addr = PostalAddress.Parse(address);

        // Assert - 輸出比較
        _output.WriteLine($"地址: {address}");
        _output.WriteLine($"  Village: {addr.Village ?? "(null)"}");
        _output.WriteLine($"  Road: {addr.Road ?? "(null)"}");
        _output.WriteLine($"  Locality: {addr.Locality ?? "(null)"}");
        _output.WriteLine($"  Number: {addr.Number}");
        _output.WriteLine($"  NormalizedAddress: {addr.NormalizedAddress}");
        _output.WriteLine("");
    }

    [Fact]
    public void Parse_FushiAddress_CanQueryZipCode()
    {
        // Arrange
        var addr = PostalAddress.Parse("花蓮縣秀林鄉富世村富世291號");

        // Act
        var result = ZipCode.Find(addr.NormalizedAddress);

        // Assert
        _output.WriteLine($"郵遞區號: {result.ZipCode ?? "(null)"}");
        _output.WriteLine($"結果類型: {result.ResultType}");
        _output.WriteLine($"是否精確匹配: {result.IsExactMatch}");

        Assert.NotNull(result.ZipCode);
        Assert.NotEmpty(result.ZipCode);
    }
}
