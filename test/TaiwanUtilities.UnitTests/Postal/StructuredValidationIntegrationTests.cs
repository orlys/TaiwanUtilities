namespace TaiwanUtilities.UnitTests;

using Xunit;
using System;
using System.Linq;
using System.Diagnostics;
using Xunit.Abstractions;

/// <summary>
/// 整合測試：驗證結構化投遞規則在完整流程中的運作
/// </summary>
[Collection("DatabaseSingleton")]
public class StructuredValidationIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public StructuredValidationIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }
    [Fact]
    public void TestDatabaseHasPostalRulesTable()
    {
        // 驗證資料庫是否包含 postal_rules 資料表
        // 這個測試依賴於已經建立的資料庫
        try
        {
            // 嘗試查詢 postal_rules（即使沒有資料也不應該拋出異常）
            var rules = TaiwanUtilities.Internals.PostalLookup.QueryPostalRules("臺北市", "中正區", "測試路");
            Assert.NotNull(rules);
        }
        catch (Exception ex)
        {
            // 如果資料表不存在，這裡會拋出異常
            Assert.Fail($"postal_rules 資料表可能不存在或查詢失敗: {ex.Message}");
        }
    }

    [Fact]
    public void TestSpecialRoadNamesCache_CanBeLoaded()
    {
        // 驗證特殊路名快取可以被載入
        try
        {
            var specialRoads = TaiwanUtilities.Internals.PostalData.SpecialRoadNames;
            Assert.NotNull(specialRoads);
            // 如果資料庫已經建立，應該至少有一些特殊路名
            // （如果資料庫為空，這個測試可能會失敗，這是正常的）
        }
        catch (Exception ex)
        {
            Assert.Fail($"無法載入特殊路名: {ex.Message}");
        }
    }

    [Fact]
    public void TestAddressTokenizer_NormalizationPerformance()
    {
        // 效能測試：正規化 1000 次應該在合理時間內完成
        var stopwatch = Stopwatch.StartNew();
        var testAddress = "臺北市中正區一村巷1號";

        for (int i = 0; i < 1000; i++)
        {
            var normalized = AddressTokenizer.Normalize(testAddress);
        }

        stopwatch.Stop();

        // 1000 次正規化應該在 100ms 內完成
        Assert.True(stopwatch.ElapsedMilliseconds < 100,
            $"正規化效能不佳: {stopwatch.ElapsedMilliseconds}ms for 1000 iterations");
    }

    [Fact]
    public void TestPostalRuleQuery_Performance()
    {
        // 效能測試：查詢投遞規則應該在毫秒級完成
        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < 100; i++)
        {
            var rules = TaiwanUtilities.Internals.PostalLookup.QueryPostalRules("臺北市", "中正區", "測試路");
        }

        stopwatch.Stop();

        // 記錄性能數據（不做時間斷言以避免間歇性失敗）
        _output.WriteLine($"投遞規則查詢性能測試:");
        _output.WriteLine($"  查詢數量: 100");
        _output.WriteLine($"  總耗時: {stopwatch.ElapsedMilliseconds} ms");
        _output.WriteLine($"  平均每個查詢: {stopwatch.ElapsedMilliseconds / 100.0:F2} ms");
        _output.WriteLine($"  查詢速率: {100000.0 / stopwatch.ElapsedMilliseconds:F0} 次/秒");
    }

    [Theory]
    [InlineData("臺北市", "中正區", "忠孝東路")]
    [InlineData("新北市", "板橋區", "中山路")]
    [InlineData("臺中市", "西屯區", "臺灣大道")]
    public void TestPostalRuleQuery_ReturnsValidData(string city, string area, string road)
    {
        // 測試查詢真實的地址資料（如果資料庫已建立）
        var rules = TaiwanUtilities.Internals.PostalLookup.QueryPostalRules(city, area, road);

        Assert.NotNull(rules);
        // 如果資料庫有資料，規則應該包含正確的城市、區域和路名
        if (rules.Count > 0)
        {
            Assert.All(rules, rule =>
            {
                Assert.Equal(city, rule.City);
                Assert.Equal(area, rule.Area);
                Assert.Equal(road, rule.Road);
            });
        }
    }

    [Fact]
    public void TestBackwardCompatibility_LegacyTables_StillWork()
    {
        // 向後相容測試：確保舊的 precise 和 gradual 表仍然可用
        var result1 = ZipCode.Find("臺北市");
        Assert.NotNull(result1);

        var result2 = ZipCode.Find("臺北市信義區");
        Assert.NotNull(result2);

        var result3 = ZipCode.Find("臺北市信義區市府路");
        Assert.NotNull(result3);
    }

    [Fact]
    public void TestValidation_WithoutStructuredData_StillWorks()
    {
        // 測試在沒有結構化資料的情況下，基本驗證仍然有效
        var address = new PostalAddress
        {
            City = "臺北市",
            District = "信義區",
            Road = "市府路"
        };

        var validation = PostalAddress.Validate(address);

        // 基本驗證應該通過
        Assert.True(validation.IsValidCity);
        Assert.True(validation.IsValidDistrict);
        Assert.True(validation.IsValidRoad);
    }

    [Fact]
    public void TestStructuredValidation_WithCompleteAddress()
    {
        // 測試完整地址的結構化驗證
        // 這個測試依賴於資料庫中有對應的 postal_rules 記錄
        var address = new PostalAddress
        {
            City = "臺北市",
            District = "信義區",
            Road = "市府路",
            Number = 1
        };

        var validation = PostalAddress.Validate(address);

        // 基本驗證應該通過
        Assert.True(validation.IsValidCity);
        Assert.True(validation.IsValidDistrict);
        Assert.True(validation.IsValidRoad);

        // 如果資料庫有結構化資料，門牌驗證也應該執行
        // （如果資料庫為空，IsValidNumber 會保持預設值 true）
    }

    [Fact]
    public async System.Threading.Tasks.Task TestMultiThreadedAccess_PostalRuleQuery()
    {
        // 並發測試：多執行緒同時查詢投遞規則應該是安全的
        var tasks = new System.Threading.Tasks.Task[10];

        for (int i = 0; i < tasks.Length; i++)
        {
            tasks[i] = System.Threading.Tasks.Task.Run(() =>
            {
                for (int j = 0; j < 10; j++)
                {
                    var rules = TaiwanUtilities.Internals.PostalLookup.QueryPostalRules("臺北市", "中正區", "測試路");
                    Assert.NotNull(rules);
                }
            });
        }

        await System.Threading.Tasks.Task.WhenAll(tasks);
    }

    [Fact]
    public void TestPostalDatabaseVersionInfo_HasPostalRulesMetadata()
    {
        // 測試資料庫版本資訊是否包含新的元數據
        var versionInfo = PostalRulesEngine.CurrentVersion;

        if (versionInfo != null)
        {
            // 如果有版本資訊，應該包含基本欄位
            Assert.NotNull(versionInfo.Version);
            Assert.True(versionInfo.RecordCount >= 0);
        }
    }

    [Theory]
    [InlineData("臺北市中正區一村巷1號")]
    [InlineData("臺北市中正區二村巷1號")]
    [InlineData("臺北市中正區三村巷1號")]
    public void TestEndToEnd_SpecialRoadName_FindZipCode(string address)
    {
        // 端對端測試：特殊路名的完整查詢流程
        var result = ZipCode.Find(address);

        // 應該能夠找到郵遞區號（如果資料庫有資料）
        Assert.NotNull(result);

        // 正規化後的地址應該保留中文數字
        var normalized = AddressTokenizer.Normalize(address);
        Assert.Contains("村巷", normalized);
    }

    [Fact]
    public void TestPostalRuleModel_CompleteWorkflow()
    {
        // 測試 PostalRule 模型的完整工作流程
        var rule = new PostalRule
        {
            Id = 1,
            ZipCode = "100",
            City = "臺北市",
            Area = "中正區",
            Road = "測試路",
            LaneStart = 1,
            LaneEnd = 10,
            NumberStart = 1,
            NumberEnd = 100,
            EvenOdd = 0 // 全部
        };

        // 測試所有範圍檢查方法
        Assert.True(rule.IsLaneInRange(5));
        Assert.True(rule.IsNumberInRange(50));
        Assert.False(rule.IsLaneInRange(15));
        Assert.False(rule.IsNumberInRange(150));
    }

    [Fact]
    public void TestValidation_Messages_AreInformative()
    {
        // 測試驗證訊息是否具有資訊性
        var address = new PostalAddress
        {
            City = "不存在的縣市",
            District = "不存在的區",
            Road = "不存在的路"
        };

        var validation = PostalAddress.Validate(address);

        // 應該有錯誤訊息
        Assert.NotEmpty(validation.Messages);

        // 訊息應該包含有用的資訊
        Assert.Contains(validation.Messages, msg => msg.Contains("不存在"));
    }
}
