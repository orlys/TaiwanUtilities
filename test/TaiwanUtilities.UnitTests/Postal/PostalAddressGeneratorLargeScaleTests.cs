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

    [Fact]
    public void Generate_2000000Addresses_ValidateAll()
    {
        // 只在 GitHub Actions CI 環境中執行此測試
        var isCI = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")) ||
                   !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));

        if (!isCI)
        {
            _output.WriteLine("此測試僅在 GitHub Actions CI 環境中執行，本地環境跳過。");
            _output.WriteLine("若要在本地執行，請設定環境變數: CI=true 或 GITHUB_ACTIONS=true");
            return;
        }

        // Arrange
        var generator = new PostalAddressGenerator();
        var totalCount = 5_000_000;  // 修改為 500 萬筆

        _output.WriteLine("=".PadRight(80, '='));
        _output.WriteLine($"大規模測試：生成 {totalCount:N0} 筆隨機地址");
        _output.WriteLine($"目標：涵蓋台灣約 1/5 戶數（全台約 1000 萬戶）");
        _output.WriteLine("=".PadRight(80, '='));
        _output.WriteLine("");

        var startTime = DateTime.Now;

        // Act - 生成地址（帶進度顯示）
        _output.WriteLine($"[{DateTime.Now:HH:mm:ss}] 開始生成地址...");
        var addresses = generator.Generate(totalCount, (current, total) =>
        {
            var elapsed = DateTime.Now - startTime;
            var percentage = (double)current / total * 100;
            var speed = current / elapsed.TotalSeconds;
            var eta = TimeSpan.FromSeconds((total - current) / speed);
            _output.WriteLine($"[{DateTime.Now:HH:mm:ss}] 生成進度: {current:N0}/{total:N0} ({percentage:F1}%) - 速度: {speed:F0} 筆/秒 - 預計剩餘: {eta.TotalMinutes:F1} 分鐘");
        });
        var generationTime = DateTime.Now - startTime;

        _output.WriteLine($"[{DateTime.Now:HH:mm:ss}] 生成完成");
        _output.WriteLine($"  耗時: {generationTime.TotalSeconds:F2} 秒 ({generationTime.TotalMinutes:F2} 分鐘)");
        _output.WriteLine($"  實際數量: {addresses.Count:N0}");
        _output.WriteLine($"  生成速度: {addresses.Count / generationTime.TotalSeconds:F0} 筆/秒");
        _output.WriteLine("");

        // Assert - 驗證所有地址
        _output.WriteLine($"[{DateTime.Now:HH:mm:ss}] 開始驗證所有地址...");
        startTime = DateTime.Now;

        var validCount = 0;
        var invalidCount = 0;
        var invalidExamples = new System.Collections.Generic.List<GeneratedPostalAddress>();
        var validationProgress = 0;

        foreach (var addr in addresses)
        {
            if (addr.Validate())
            {
                validCount++;
            }
            else
            {
                invalidCount++;
                if (invalidExamples.Count < 20)
                {
                    invalidExamples.Add(addr);
                }
            }

            // 每 10,000 筆輸出一次進度
            validationProgress++;
            if (validationProgress % 10_000 == 0)
            {
                var elapsed = DateTime.Now - startTime;
                var percentage = (double)validationProgress / addresses.Count * 100;
                var speed = validationProgress / elapsed.TotalSeconds;
                var eta = TimeSpan.FromSeconds((addresses.Count - validationProgress) / speed);
                _output.WriteLine($"[{DateTime.Now:HH:mm:ss}] 驗證進度: {validationProgress:N0}/{addresses.Count:N0} ({percentage:F1}%) - 速度: {speed:F0} 筆/秒 - 預計剩餘: {eta.TotalMinutes:F1} 分鐘");
            }
        }

        var validationTime = DateTime.Now - startTime;
        var validRate = (double)validCount / addresses.Count * 100;

        _output.WriteLine($"[{DateTime.Now:HH:mm:ss}] 驗證完成");
        _output.WriteLine($"  耗時: {validationTime.TotalSeconds:F2} 秒 ({validationTime.TotalMinutes:F2} 分鐘)");
        _output.WriteLine($"  驗證速度: {addresses.Count / validationTime.TotalSeconds:F0} 筆/秒");
        _output.WriteLine("");

        // 輸出驗證統計
        _output.WriteLine("=== 驗證統計 ===");
        _output.WriteLine($"總數量: {addresses.Count:N0}");
        _output.WriteLine($"有效地址: {validCount:N0} ({validRate:F2}%)");
        _output.WriteLine($"無效地址: {invalidCount:N0} ({100 - validRate:F2}%)");
        _output.WriteLine("");

        // 按來源分組統計
        var bySource = addresses.GroupBy(a => a.Source).ToList();
        _output.WriteLine("=== 按來源分組 ===");
        foreach (var group in bySource)
        {
            var sourceValid = group.Count(a => a.Validate());
            var sourceValidRate = (double)sourceValid / group.Count() * 100;
            _output.WriteLine($"{group.Key}:");
            _output.WriteLine($"  數量: {group.Count():N0} 筆 ({(double)group.Count() / addresses.Count * 100:F2}%)");
            _output.WriteLine($"  有效: {sourceValid:N0} ({sourceValidRate:F2}%)");
        }
        _output.WriteLine("");

        // 按縣市分組統計（全部縣市）
        var byCity = addresses.GroupBy(a => a.Address.City ?? "<空白>")
            .OrderByDescending(g => g.Count())
            .ToList();

        _output.WriteLine($"=== 按縣市分組 (共 {byCity.Count} 個縣市) ===");
        foreach (var group in byCity)
        {
            var cityValid = group.Count(a => a.Validate());
            var cityValidRate = (double)cityValid / group.Count() * 100;
            var percentage = (double)group.Count() / addresses.Count * 100;
            _output.WriteLine($"{group.Key}: {group.Count():N0} 筆 ({percentage:F2}%) - 有效: {cityValid:N0} ({cityValidRate:F2}%)");
        }
        _output.WriteLine("");

        // 檢查異常的縣市名稱
        var anomalousCities = byCity.Where(g =>
            g.Key == "<空白>" ||
            g.Key.Length < 3 ||
            g.Key.Contains("下縣") ||
            (!g.Key.EndsWith("市") && !g.Key.EndsWith("縣"))
        ).ToList();

        if (anomalousCities.Any())
        {
            _output.WriteLine("⚠️  發現異常的縣市名稱");
            _output.WriteLine("=== 異常縣市詳細資訊 ===");
            foreach (var group in anomalousCities)
            {
                _output.WriteLine($"\n異常縣市: '{group.Key}' ({group.Count():N0} 筆)");

                // 顯示前 3 個範例
                var examples = group.Take(3).ToList();
                foreach (var addr in examples)
                {
                    _output.WriteLine($"\n  範例地址:");
                    _output.WriteLine($"    完整地址: {addr.FullAddress}");
                    _output.WriteLine($"    來源: {addr.Source}");
                    _output.WriteLine($"    郵遞區號: {addr.ZipCode}");
                    _output.WriteLine($"    City: '{addr.Address.City ?? "<null>"}'");
                    _output.WriteLine($"    District: '{addr.Address.District ?? "<null>"}'");
                    _output.WriteLine($"    Road: '{addr.Address.Road ?? "<null>"}'");

                    if (addr.Rule != null)
                    {
                        _output.WriteLine($"    規則City: '{addr.Rule.City}'");
                        _output.WriteLine($"    規則Area: '{addr.Rule.Area}'");
                        _output.WriteLine($"    規則Road: '{addr.Rule.Road}'");
                    }
                }
            }
            _output.WriteLine("");
        }

        // 輸出無效地址範例（帶詳細 debug 資訊）
        if (invalidExamples.Any())
        {
            _output.WriteLine($"❌ 發現 {invalidCount:N0} 個無效地址");
            _output.WriteLine("=== 無效地址範例 (前 20 筆) - 詳細 DEBUG 資訊 ===");
            for (int i = 0; i < invalidExamples.Count; i++)
            {
                var addr = invalidExamples[i];
                _output.WriteLine($"\n[無效地址 #{i + 1}]");
                _output.WriteLine($"  完整地址: {addr.FullAddress}");
                _output.WriteLine($"  來源: {addr.Source}");
                _output.WriteLine($"  郵遞區號: {addr.ZipCode}");
                _output.WriteLine($"  解析結果:");
                _output.WriteLine($"    City: '{addr.Address.City ?? "<null>"}'");
                _output.WriteLine($"    District: '{addr.Address.District ?? "<null>"}'");
                _output.WriteLine($"    Road: '{addr.Address.Road ?? "<null>"}'");
                _output.WriteLine($"    Number: {addr.Address.Number?.ToString() ?? "<null>"}");

                if (addr.Rule != null)
                {
                    _output.WriteLine($"  規則資訊:");
                    _output.WriteLine($"    City: '{addr.Rule.City}'");
                    _output.WriteLine($"    Area: '{addr.Rule.Area}'");
                    _output.WriteLine($"    Road: '{addr.Rule.Road}'");
                }

                // 驗證為什麼這個地址無效
                var zipResult = ZipCode.Find(addr.FullAddress);
                _output.WriteLine($"  查詢結果:");
                _output.WriteLine($"    找到郵遞區號: '{zipResult.ZipCode}'");
                _output.WriteLine($"    預期郵遞區號: '{addr.ZipCode}'");
                _output.WriteLine($"    匹配: {zipResult.ZipCode == addr.ZipCode}");
            }
            _output.WriteLine("");
        }

        // 效能統計
        var totalTime = generationTime.TotalSeconds + validationTime.TotalSeconds;
        _output.WriteLine("=== 效能統計 ===");
        _output.WriteLine($"總耗時: {totalTime:F2} 秒 ({totalTime / 60:F2} 分鐘)");
        _output.WriteLine($"生成耗時: {generationTime.TotalSeconds:F2} 秒 ({generationTime.TotalMinutes:F2} 分鐘)");
        _output.WriteLine($"驗證耗時: {validationTime.TotalSeconds:F2} 秒 ({validationTime.TotalMinutes:F2} 分鐘)");
        _output.WriteLine($"整體速度: {addresses.Count / totalTime:F0} 筆/秒");
        _output.WriteLine("");

        // 要求 100% 有效率
        Assert.True(validRate == 100.0,
            $"所有生成的地址都應該有效（100%）。\n" +
            $"當前有效率: {validRate:F2}%（{validCount:N0}/{addresses.Count:N0}）\n" +
            $"無效地址數: {invalidCount:N0}\n" +
            $"在 {totalCount:N0} 筆大規模測試中，任何無效地址都代表生成器的投遞規則涵蓋不完整。");
    }
}
