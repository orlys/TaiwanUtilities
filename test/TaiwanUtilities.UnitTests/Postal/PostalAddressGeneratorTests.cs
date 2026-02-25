namespace TaiwanUtilities.UnitTests;

using System;
using System.Linq;

using Xunit;
using Xunit.Abstractions;

/// <summary>
/// PostalAddressGenerator 驗證測試
/// </summary>
[Collection("DatabaseSingleton")]
public class PostalAddressGeneratorTests
{
    private readonly ITestOutputHelper _output;

    public PostalAddressGeneratorTests(ITestOutputHelper output)
    {
        _output = output;
    }
    [Fact]
    public void Generate_ShouldReturnAddresses()
    {
        // Arrange
        var generator = new PostalAddressGenerator();

        // Act
        var addresses = generator.Generate(10);

        // Assert
        Assert.NotEmpty(addresses);
        Assert.Equal(10, addresses.Count);
    }

    [Fact]
    public void Generate_ShouldProduceValidAddresses()
    {
        // Arrange
        var generator = new PostalAddressGenerator();

        // Act
        var addresses = generator.Generate(100);

        // Assert - 使用 PostalAddress.Validate() 驗證地址品質
        var validCount = 0;
        var invalidAddresses = new List<string>();

        foreach (var addr in addresses)
        {
            if (addr.Validate())
            {
                validCount++;
            }
            else
            {
                invalidAddresses.Add(addr.FullAddress);
            }
        }

        // 輸出統計資訊以便診斷
        var validRate = (double)validCount / addresses.Count * 100;
        _output.WriteLine($"地址生成統計:");
        _output.WriteLine($"  總數: {addresses.Count}");
        _output.WriteLine($"  有效: {validCount} ({validRate:F1}%)");
        _output.WriteLine($"  無效: {invalidAddresses.Count} ({100 - validRate:F1}%)");
        _output.WriteLine("");

        // 如果有無效地址，輸出以便診斷
        if (invalidAddresses.Any())
        {
            _output.WriteLine($"❌ 發現 {invalidAddresses.Count} 個無效地址:");
            foreach (var invalidAddr in invalidAddresses)
            {
                _output.WriteLine($"  - {invalidAddr}");
            }
            _output.WriteLine("");
            _output.WriteLine("這些地址應該被生成器正確處理。請檢查投遞規則是否完整涵蓋。");
        }

        // 所有地址都應該 100% 有效，否則代表有規則沒涵蓋到
        Assert.True(validRate == 100.0,
            $"所有生成的地址都應該有效（100%）。\n" +
            $"當前有效率: {validRate:F1}%（{validCount}/{addresses.Count}）\n" +
            $"無效地址數: {invalidAddresses.Count}\n" +
            $"如果不是 100%，代表生成器的投遞規則涵蓋不完整，需要修正。");
    }

    [Fact]
    public void Generate_MultipleAddresses_ShouldReturnDifferentCities()
    {
        // Arrange
        var generator = new PostalAddressGenerator();

        // Act - 生成更多地址以確保包含多個縣市
        var addresses = generator.Generate(500);

        // Assert
        Assert.NotEmpty(addresses);
        Assert.Equal(500, addresses.Count);

        // 應該包含多個不同的縣市
        var uniqueCities = addresses.Select(a => a.Address.City).Distinct().Count();
        Assert.True(uniqueCities > 1, $"應該包含多個縣市，實際: {uniqueCities}");

        // 驗證地址品質
        var validCount = addresses.Count(a => a.Validate());
        Assert.True(validCount > 0, $"應該至少有一些有效地址。有效: {validCount}/{addresses.Count}");
    }

    [Fact]
    public void Generate_ValidationQuality_ShouldHaveHighValidRate()
    {
        // Arrange
        var generator = new PostalAddressGenerator();

        // Act - 生成較多地址來測試品質
        var addresses = generator.Generate(50);

        // Assert - 使用 PostalAddress.Validate() 檢查有效率
        var validCount = addresses.Count(a => a.Validate());
        var validRate = (double)validCount / addresses.Count * 100;

        // 輸出統計以便診斷問題
        Assert.True(validRate > 50,
            $"有效率應該 > 50%，實際: {validRate:F1}% ({validCount}/{addresses.Count})");
    }
}
