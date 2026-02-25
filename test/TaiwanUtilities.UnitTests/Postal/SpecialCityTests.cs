namespace TaiwanUtilities.UnitTests;

using Xunit;
using Xunit.Abstractions;

/// <summary>
/// 測試特殊行政區名稱的地址解析
/// 包括：南海諸島、釣魚臺列嶼等特殊地名
/// </summary>
[Collection("DatabaseSingleton")]
public class SpecialCityTests
{
    private readonly ITestOutputHelper _output;

    public SpecialCityTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [InlineData("南海諸東沙東沙", "817001")]
    [InlineData("南海諸南沙南沙", "819001")]
    [InlineData("釣魚臺釣魚臺列釣魚臺列嶼", "290001")]
    public void TestSpecialCityZipCodeLookup(string address, string expectedZipCode)
    {
        // Act
        var result = ZipCode.Find(address);

        // Debug output
        _output.WriteLine($"輸入地址: {address}");
        _output.WriteLine($"查詢結果: {result.ZipCode}");
        _output.WriteLine($"預期郵遞區號: {expectedZipCode}");

        // Assert
        Assert.Equal(expectedZipCode, result.ZipCode);
    }

    [Theory]
    [InlineData("南海諸東沙東沙")]
    [InlineData("南海諸南沙南沙")]
    [InlineData("釣魚臺釣魚臺列釣魚臺列嶼")]
    [InlineData("南海諸東沙東沙1號")]
    [InlineData("釣魚臺釣魚臺列釣魚臺列嶼1號")]
    public void TestSpecialCityAddressParsing(string address)
    {
        // Act
        var parsed = PostalAddress.Parse(address);

        // Debug output
        _output.WriteLine($"原始地址: {address}");
        _output.WriteLine($"解析結果:");
        _output.WriteLine($"  City: '{parsed.City ?? "<null>"}'");
        _output.WriteLine($"  District: '{parsed.District ?? "<null>"}'");
        _output.WriteLine($"  Road: '{parsed.Road ?? "<null>"}'");
        _output.WriteLine($"  RawAddress: '{parsed.RawAddress}'");
        _output.WriteLine($"  NormalizedAddress: '{parsed.NormalizedAddress}'");

        // Assert - 地址應該能成功解析
        Assert.NotNull(parsed);
        Assert.NotNull(parsed.City);
        Assert.NotEqual("", parsed.City);
    }

    [Fact]
    public void TestGeneratedAddressesWithSpecialCities()
    {
        // Arrange
        var generator = new PostalAddressGenerator();

        // Act - 生成 10 萬筆地址
        var addresses = generator.Generate(100_000);

        // 找出所有南海諸島和釣魚臺的地址
        var specialAddresses = addresses
            .Where(a => a.Address.City != null &&
                       (a.Address.City.Contains("南海諸") ||
                        a.Address.City.Contains("釣魚臺")))
            .ToList();

        _output.WriteLine($"總共生成: {addresses.Count:N0} 筆地址");
        _output.WriteLine($"特殊地名地址: {specialAddresses.Count} 筆");
        _output.WriteLine("");

        // Debug - 輸出所有特殊地名的範例
        foreach (var addr in specialAddresses.Take(10))
        {
            _output.WriteLine($"地址: {addr.FullAddress}");
            _output.WriteLine($"  City: '{addr.Address.City}'");
            _output.WriteLine($"  District: '{addr.Address.District ?? "<null>"}'");
            _output.WriteLine($"  Road: '{addr.Address.Road ?? "<null>"}'");
            _output.WriteLine($"  ZipCode: {addr.ZipCode}");
            _output.WriteLine($"  來源: {addr.Source}");

            // 驗證地址是否能正確查詢郵遞區號
            var findResult = ZipCode.Find(addr.FullAddress);
            _output.WriteLine($"  Find結果: {findResult.ZipCode} (預期: {addr.ZipCode})");
            _output.WriteLine($"  驗證: {(findResult.ZipCode == addr.ZipCode ? "✓" : "✗")}");
            _output.WriteLine("");
        }

        // Assert - 所有特殊地名的地址都應該能正確驗證
        foreach (var addr in specialAddresses)
        {
            var isValid = addr.Validate();
            if (!isValid)
            {
                _output.WriteLine($"❌ 驗證失敗: {addr.FullAddress}");
                var findResult = ZipCode.Find(addr.FullAddress);
                _output.WriteLine($"   生成郵遞區號: {addr.ZipCode}");
                _output.WriteLine($"   查詢郵遞區號: {findResult.ZipCode}");
            }
            Assert.True(isValid, $"特殊地名地址驗證失敗: {addr.FullAddress}");
        }
    }

    [Fact]
    public void TestSpecialCityPostalRules()
    {
        // Act - 從資料庫查詢所有南海諸島和釣魚臺的投遞規則
        var allRules = PostalDatabase.ExecuteQuery(repo => repo.QueryRandomPostalRules(100_000));

        var specialRules = allRules
            .Where(r => r.City.Contains("南海諸") || r.City.Contains("釣魚臺"))
            .ToList();

        _output.WriteLine($"總規則數: {allRules.Count:N0}");
        _output.WriteLine($"特殊地名規則數: {specialRules.Count}");
        _output.WriteLine("");

        // 輸出所有特殊地名的規則
        foreach (var rule in specialRules)
        {
            _output.WriteLine($"郵遞區號: {rule.ZipCode}");
            _output.WriteLine($"  City: '{rule.City}'");
            _output.WriteLine($"  Area: '{rule.Area}'");
            _output.WriteLine($"  Road: '{rule.Road}'");
            _output.WriteLine($"  完整地址: {rule.City}{rule.Area}{rule.Road}");
            _output.WriteLine("");
        }

        // Assert
        Assert.NotEmpty(specialRules);
    }
}
