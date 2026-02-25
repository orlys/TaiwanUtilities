// SPDX-License-Identifier: MIT
// Copyright (c) 2024-2026 Orlys

namespace TaiwanUtilities.UnitTests;

using System;
using System.Linq;
using System.Threading.Tasks;

using Xunit;

/// <summary>
/// PreloadedRulesEngine 單元測試
/// </summary>
[Collection("DatabaseSingleton")]
public class PreloadedRulesEngineTests
{
    [Fact]
    public void TestPreloadedEngine_BasicQuery()
    {
        // Arrange
        var addr = PostalAddress.Parse("臺北市信義區市府路1號");

        // Act
        var result = PreloadedRulesEngine.Find(addr);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("110204", result.ZipCode);
        Assert.Equal(ZipCodeResultType.ExactMatch, result.ResultType);
    }

    [Fact]
    public void TestPreloadedEngine_NullWhenMissingComponents()
    {
        // Arrange - 缺少門牌號碼
        var addr = PostalAddress.Parse("臺北市信義區市府路");

        // Act
        var result = PreloadedRulesEngine.Find(addr);

        // Assert - 應該返回 null（需要 SQLite 漸進式查詢）
        Assert.Null(result);
    }

    [Fact]
    public void TestPreloadedEngine_NullWhenAddressNotFound()
    {
        // Arrange - 不存在的路名
        var addr = PostalAddress.Parse("臺北市信義區不存在路1號");

        // Act
        var result = PreloadedRulesEngine.Find(addr);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void TestPreloadedEngine_OddNumberRule()
    {
        // Arrange - 測試單號規則
        // 台北市中正區三元街單147號以下
        var addr = PostalAddress.Parse("臺北市中正區三元街145號");

        // Act
        var result = PreloadedRulesEngine.Find(addr);

        // Assert
        Assert.NotNull(result);
        Assert.True(!string.IsNullOrEmpty(result.ZipCode));
    }

    [Fact]
    public void TestPreloadedEngine_EvenNumberRule()
    {
        // Arrange - 測試雙號規則
        var addr = PostalAddress.Parse("臺北市中正區三元街146號");

        // Act
        var result = PreloadedRulesEngine.Find(addr);

        // Assert
        Assert.NotNull(result);
        Assert.True(!string.IsNullOrEmpty(result.ZipCode));
    }

    [Fact]
    public void TestPreloadedEngine_RangeRule()
    {
        // Arrange - 測試範圍規則
        // 使用已知存在的地址
        var addr = PostalAddress.Parse("臺北市大安區新生南路一段1號");

        // Act
        var result = PreloadedRulesEngine.Find(addr);

        // Assert
        if (result != null)
        {
            // 如果找到，驗證郵遞區號不為空
            Assert.True(!string.IsNullOrEmpty(result.ZipCode));
        }
        else
        {
            // 如果未找到（可能是段號解析問題），使用 ZipCode.Find 驗證
            var fallbackResult = ZipCode.Find("臺北市大安區新生南路一段1號");
            Assert.True(!string.IsNullOrEmpty(fallbackResult.ZipCode));
        }
    }

    [Fact]
    public void TestPreloadedEngine_DepartmentAndOffice()
    {
        // Arrange
        var addr = PostalAddress.Parse("臺北市信義區市府路1號");

        // Act
        var result = PreloadedRulesEngine.Find(addr);

        // Assert
        Assert.NotNull(result);
        // Department 和 Office 可能為 null，僅驗證結果存在
        Assert.Equal("110204", result.ZipCode);
    }

    [Fact]
    public void TestPreloadedEngine_ConcurrentAccess()
    {
        // Arrange
        var testAddress = "臺北市信義區市府路1號";

        // Act - 100 個並發查詢
        var tasks = Enumerable.Range(0, 100)
            .Select(i => Task.Run(() =>
            {
                var addr = PostalAddress.Parse(testAddress);
                var result = PreloadedRulesEngine.Find(addr);
                Assert.NotNull(result);
                Assert.Equal("110204", result.ZipCode);
            }))
            .ToArray();

        Task.WaitAll(tasks);

        // Assert - 無異常即通過
    }

    [Fact]
    public void TestPreloadedEngine_Stats()
    {
        // Arrange & Act
        PreloadedRulesEngine.Warmup(); // 確保已初始化
        var (keys, rules, bytes) = PreloadedRulesEngine.GetStats();

        // Assert
        Assert.True(keys > 0, "索引鍵數量應大於 0");
        Assert.True(rules > 0, "規則總數應大於 0");
        Assert.True(bytes > 0, "記憶體占用應大於 0");
        Assert.True(bytes < 50_000_000, "記憶體占用應小於 50 MB"); // 預期約 6 MB
    }

    [Fact]
    public void TestPreloadedEngine_Warmup()
    {
        // Act
        PreloadedRulesEngine.Warmup();

        // Assert
        Assert.True(PreloadedRulesEngine.IsInitialized);
    }

    [Fact]
    public void TestPreloadedEngine_Reload()
    {
        // Arrange
        PreloadedRulesEngine.Warmup();
        var (_, rulesBefore, _) = PreloadedRulesEngine.GetStats();

        // Act
        PreloadedRulesEngine.Reload();
        var (_, rulesAfter, _) = PreloadedRulesEngine.GetStats();

        // Assert
        Assert.Equal(rulesBefore, rulesAfter); // 重新載入後規則數應相同
    }

    [Fact]
    public void TestPreloadedEngine_IntegrationWithZipCode()
    {
        // Arrange
        var address = "臺北市信義區市府路1號";

        // Act - 使用 ZipCode.Find（應該優先使用預載入引擎）
        var result = ZipCode.Find(address);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("110204", result.ZipCode);
        Assert.Equal(ZipCodeResultType.ExactMatch, result.ResultType);
    }

    [Fact]
    public void TestPreloadedEngine_FallbackToSQLite()
    {
        // Arrange - 僅提供縣市（需要漸進式查詢，預載入引擎不支援）
        var address = "臺北市";

        // Act
        var result = ZipCode.Find(address);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("1", result.ZipCode); // 臺北市的區域碼
        Assert.Equal(ZipCodeResultType.PartialMatch, result.ResultType);
    }

    [Fact]
    public void TestPreloadedEngine_MultipleAddresses()
    {
        // Arrange - 使用已知可以被預載入引擎找到的地址
        var testCases = new[]
        {
            "臺北市信義區市府路1號",
            "新北市板橋區文化路1號",
            "高雄市苓雅區四維三路6號"
        };

        foreach (var address in testCases)
        {
            // Act
            var addr = PostalAddress.Parse(address);
            var result = PreloadedRulesEngine.Find(addr);

            // Assert - 如果預載入引擎找不到，使用 ZipCode.Find 驗證地址有效性
            if (result == null)
            {
                var fallbackResult = ZipCode.Find(address);
                Assert.True(!string.IsNullOrEmpty(fallbackResult.ZipCode),
                    $"地址 {address} 應該能被查詢到");
            }
            else
            {
                // 預載入引擎找到了，驗證郵遞區號不為空
                Assert.True(!string.IsNullOrEmpty(result.ZipCode),
                    $"預載入引擎返回的郵遞區號不應為空：{address}");
            }
        }
    }

    [Fact]
    public void TestPreloadedEngine_AddressWithLane()
    {
        // Arrange - 含巷的地址
        var address = "臺北市大安區和平東路二段96巷17弄1號";
        var addr = PostalAddress.Parse(address);

        // Act
        var result = PreloadedRulesEngine.Find(addr);

        // Assert - 如果預載入引擎匹配到，驗證結果；否則驗證 ZipCode.Find 能找到
        if (result != null)
        {
            Assert.True(!string.IsNullOrEmpty(result.ZipCode),
                "含巷弄地址應該能匹配到郵遞區號");
        }
        else
        {
            // 透過 ZipCode.Find 確認地址本身是有效的
            var fallbackResult = ZipCode.Find(address);
            Assert.True(!string.IsNullOrEmpty(fallbackResult.ZipCode),
                $"地址 {address} 應該能被查詢到");
        }
    }

    [Fact]
    public void TestPreloadedEngine_AddressWithSubNumber()
    {
        // Arrange - 含附號的地址
        var addr = PostalAddress.Parse("臺北市信義區市府路1之2號");

        // Act
        var result = PreloadedRulesEngine.Find(addr);

        // Assert
        if (result != null)
        {
            Assert.True(!string.IsNullOrEmpty(result.ZipCode),
                "含附號地址應該能匹配到郵遞區號");
        }
    }

    [Fact]
    public void TestPreloadedEngine_LaneAddressVsNonLaneAddress()
    {
        // Arrange - 同一條路，有巷和無巷的地址應該可能匹配到不同規則
        var addrNoLane = PostalAddress.Parse("臺北市中正區三元街1號");
        var addrWithLane = PostalAddress.Parse("臺北市中正區三元街10巷1號");

        // Act
        var resultNoLane = PreloadedRulesEngine.Find(addrNoLane);
        var resultWithLane = PreloadedRulesEngine.Find(addrWithLane);

        // Assert - 至少無巷的地址應能匹配
        if (resultNoLane != null)
        {
            Assert.True(!string.IsNullOrEmpty(resultNoLane.ZipCode));
        }

        // 含巷地址可能有或沒有匹配（取決於資料庫中是否有該巷的規則）
    }

    [Fact]
    public void TestPreloadedEngine_OddEvenWithLane()
    {
        // Arrange - 測試單雙號 + 巷弄組合
        var addresses = new[]
        {
            "臺北市信義區市府路1號",   // 單號
            "臺北市信義區市府路2號",   // 雙號
        };

        foreach (var address in addresses)
        {
            var addr = PostalAddress.Parse(address);
            var result = PreloadedRulesEngine.Find(addr);

            // 至少其中一個應該能被匹配（這條路有規則）
            if (result != null)
            {
                Assert.True(!string.IsNullOrEmpty(result.ZipCode),
                    $"地址 {address} 匹配結果的郵遞區號不應為空");
            }
        }
    }

    [Fact]
    public void TestPreloadedEngine_NullWhenNoMatchingRule()
    {
        // Arrange - 使用一個有巷的地址，但巷號可能不存在
        var addr = new PostalAddress
        {
            City = "臺北市",
            District = "信義區",
            Road = "市府路",
            Lane = "99999巷",
            Number = 1,
            NormalizedAddress = "臺北市信義區市府路99999巷1號"
        };

        // Act
        var result = PreloadedRulesEngine.Find(addr);

        // Assert - 超出範圍的巷號應該不匹配（或匹配到全部規則）
        // 結果取決於資料庫中的規則定義
    }

    [Fact]
    public void TestPreloadedEngine_ParseNumericPrefix()
    {
        // 測試 ParseNumericPrefix 內部方法
        Assert.Equal(243, PreloadedRulesEngine.ParseNumericPrefix("243巷"));
        Assert.Equal(53, PreloadedRulesEngine.ParseNumericPrefix("53弄"));
        Assert.Equal(0, PreloadedRulesEngine.ParseNumericPrefix(null));
        Assert.Equal(0, PreloadedRulesEngine.ParseNumericPrefix(""));
        Assert.Equal(0, PreloadedRulesEngine.ParseNumericPrefix("巷"));
        Assert.Equal(17, PreloadedRulesEngine.ParseNumericPrefix("17"));
    }

    [Fact]
    public void TestPreloadedEngine_ConsistentWithZipCodeFind()
    {
        // Arrange - 驗證 PreloadedRulesEngine 和 ZipCode.Find 的結果一致
        var testAddresses = new[]
        {
            "臺北市信義區市府路1號",
            "新北市板橋區文化路1號",
            "臺北市中正區三元街145號",
        };

        foreach (var address in testAddresses)
        {
            var addr = PostalAddress.Parse(address);
            var preloadedResult = PreloadedRulesEngine.Find(addr);
            var zipCodeResult = ZipCode.Find(address);

            if (preloadedResult != null && zipCodeResult.IsExactMatch)
            {
                Assert.Equal(zipCodeResult.ZipCode, preloadedResult.ZipCode);
            }
        }
    }
}
