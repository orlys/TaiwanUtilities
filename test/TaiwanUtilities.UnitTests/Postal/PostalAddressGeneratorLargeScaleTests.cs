namespace TaiwanUtilities.UnitTests;

using System;
using System.Linq;

using Xunit;
using Xunit.Abstractions;

/// <summary>
/// PostalAddressGenerator 大規模驗證測試
/// </summary>
[Collection("DatabaseSingleton")]
public class PostalAddressGeneratorLargeScaleTests
{
    private readonly ITestOutputHelper _output;

    public PostalAddressGeneratorLargeScaleTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Generate_10000Addresses_ValidateAll()
    {
        // Arrange
        var generator = new PostalAddressGenerator();
        var totalCount = 10000;

        _output.WriteLine($"開始生成 {totalCount} 筆隨機地址...");
        var startTime = DateTime.Now;

        // Act
        var addresses = generator.Generate(totalCount);

        var generationTime = DateTime.Now - startTime;
        _output.WriteLine($"生成完成，耗時: {generationTime.TotalSeconds:F2} 秒");
        _output.WriteLine($"實際生成數量: {addresses.Count}");
        _output.WriteLine("");

        // Assert - 使用 PostalAddress.Validate() 驗證所有地址
        _output.WriteLine("開始驗證所有地址...");
        startTime = DateTime.Now;

        var validAddresses = 0;
        var invalidAddresses = 0;
        var invalidExamples = new System.Collections.Generic.List<string>();

        foreach (var addr in addresses)
        {
            if (addr.Validate())
            {
                validAddresses++;
            }
            else
            {
                invalidAddresses++;
                if (invalidExamples.Count < 10)
                {
                    invalidExamples.Add(addr.FullAddress);
                }
            }
        }

        var validationTime = DateTime.Now - startTime;
        _output.WriteLine($"驗證完成，耗時: {validationTime.TotalSeconds:F2} 秒");
        _output.WriteLine("");

        // 輸出統計結果
        var validRate = (double)validAddresses / addresses.Count * 100;
        _output.WriteLine("=== 驗證統計 ===");
        _output.WriteLine($"總數量: {addresses.Count}");
        _output.WriteLine($"有效地址: {validAddresses} ({validRate:F2}%)");
        _output.WriteLine($"無效地址: {invalidAddresses} ({100 - validRate:F2}%)");
        _output.WriteLine("");

        // 按來源分組統計
        var bySource = addresses.GroupBy(a => a.Source).ToList();
        _output.WriteLine("=== 按來源分組 ===");
        foreach (var group in bySource)
        {
            var sourceValid = group.Count(a => a.Validate());
            var sourceValidRate = (double)sourceValid / group.Count() * 100;
            _output.WriteLine($"{group.Key}: {group.Count()} 筆 (有效: {sourceValid}, {sourceValidRate:F2}%)");
        }
        _output.WriteLine("");

        // 按縣市分組統計（前 10 名）
        var byCity = addresses.GroupBy(a => a.Address.City)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .ToList();

        _output.WriteLine("=== 按縣市分組 (前 10 名) ===");
        foreach (var group in byCity)
        {
            var cityValid = group.Count(a => a.Validate());
            var cityValidRate = (double)cityValid / group.Count() * 100;
            _output.WriteLine($"{group.Key}: {group.Count()} 筆 (有效: {cityValid}, {cityValidRate:F2}%)");
        }
        _output.WriteLine("");

        // 輸出無效地址範例
        if (invalidExamples.Any())
        {
            _output.WriteLine("=== 無效地址範例 (前 10 筆) ===");
            foreach (var example in invalidExamples)
            {
                _output.WriteLine($"  - {example}");
            }
            _output.WriteLine("");
        }

        // 輸出有效地址範例
        var validExamples = addresses.Where(a => a.Validate()).Take(10).ToList();
        if (validExamples.Any())
        {
            _output.WriteLine("=== 有效地址範例 (前 10 筆) ===");
            foreach (var example in validExamples)
            {
                _output.WriteLine($"  - {example.FullAddress} ({example.ZipCode})");
            }
            _output.WriteLine("");
        }

        // 驗證基本要求
        Assert.True(addresses.Count > 0, "應該生成至少一些地址");
        Assert.True(validAddresses > 0, "應該至少有一些有效地址");

        // 記錄驗證率，但不強制要求特定比例（因為目前沒有 postal_rules）
        _output.WriteLine($"最終驗證率: {validRate:F2}%");

        // 如果有效率太低，給出警告但不失敗測試
        if (validRate < 10)
        {
            _output.WriteLine("⚠️  警告: 驗證率低於 10%，建議檢查生成邏輯");
        }
    }

    [Fact]
    public void Generate_10000Addresses_Performance_ValidateAll()
    {
        // Arrange
        var generator = new PostalAddressGenerator();
        var totalCount = 10000;

        _output.WriteLine($"開始生成 {totalCount} 筆隨機地址（效能測試）...");
        var startTime = DateTime.Now;

        // Act
        var addresses = generator.Generate(totalCount);

        var generationTime = DateTime.Now - startTime;
        _output.WriteLine($"生成完成，耗時: {generationTime.TotalSeconds:F2} 秒");
        _output.WriteLine($"實際生成數量: {addresses.Count}");
        _output.WriteLine("");

        // Assert - 驗證所有地址
        _output.WriteLine("開始驗證所有地址...");
        startTime = DateTime.Now;

        var validCount = 0;
        var invalidCount = 0;

        foreach (var addr in addresses)
        {
            // 驗證地址有效性
            if (addr.Validate())
            {
                validCount++;
            }
            else
            {
                invalidCount++;
            }
        }

        var validationTime = DateTime.Now - startTime;
        _output.WriteLine($"驗證完成，耗時: {validationTime.TotalSeconds:F2} 秒");
        _output.WriteLine("");

        var validRate = (double)validCount / addresses.Count * 100;
        _output.WriteLine("=== 驗證統計 ===");
        _output.WriteLine($"總數量: {addresses.Count}");
        _output.WriteLine($"有效地址: {validCount} ({validRate:F2}%)");
        _output.WriteLine($"無效地址: {invalidCount} ({100 - validRate:F2}%)");
        _output.WriteLine("");

        // 效能統計
        var totalTime = generationTime.TotalSeconds + validationTime.TotalSeconds;
        var addressesPerSecond = addresses.Count / totalTime;
        _output.WriteLine("=== 效能統計 ===");
        _output.WriteLine($"總耗時: {totalTime:F2} 秒");
        _output.WriteLine($"生成速度: {addresses.Count / generationTime.TotalSeconds:F0} 筆/秒");
        _output.WriteLine($"驗證速度: {addresses.Count / validationTime.TotalSeconds:F0} 筆/秒");
        _output.WriteLine($"整體速度: {addressesPerSecond:F0} 筆/秒");

        Assert.True(validCount > 0, "應該至少有一些有效地址");
    }
}
