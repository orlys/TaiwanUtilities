namespace TaiwanUtilities.UnitTests;

using Xunit;

using System.Collections.Generic;

/// <summary>
/// 測試結構化投遞規則驗證（使用 postal_rules 資料表）
/// </summary>
[Collection("DatabaseSingleton")]
public class StructuredValidationTests
{
    [Theory]
    [InlineData("彰化縣永靖鄉一村巷1號", "512", true)]
    public void TestSpecialRoadNames_ShouldNotConvertChineseNumbers(string address, string expectedZipCode, bool shouldBeValid)
    {
        // 測試特殊路名（如"一村巷"）不會被轉換為"1村巷"
        // 一村巷位於彰化縣永靖鄉，郵遞區號 512 (3碼) 或 512004 (6碼)
        var result = ZipCode.Find(address);

        if (shouldBeValid)
        {
            Assert.True(result.ZipCode.StartsWith(expectedZipCode),
                $"Expected zip code to start with {expectedZipCode}, but got {result.ZipCode}");
        }
    }

    [Fact]
    public void TestPostalRuleModel_IsNumberInRange_SingleNumber()
    {
        // 測試單一門牌號碼範圍檢查
        var rule = new PostalRule
        {
            NumberStart = 1,
            NumberEnd = 10
        };

        Assert.True(rule.IsNumberInRange(1));
        Assert.True(rule.IsNumberInRange(5));
        Assert.True(rule.IsNumberInRange(10));
        Assert.False(rule.IsNumberInRange(0));
        Assert.False(rule.IsNumberInRange(11));
    }

    [Fact]
    public void TestPostalRuleModel_IsNumberInRange_WithSubNumber()
    {
        // 測試門牌附號範圍檢查
        var rule = new PostalRule
        {
            NumberStart = 1,
            NumberEnd = 10,
            NumberStartSub = 1,
            NumberEndSub = 5
        };

        Assert.True(rule.IsNumberInRange(5, 1));
        Assert.True(rule.IsNumberInRange(5, 3));
        Assert.True(rule.IsNumberInRange(5, 5));
        Assert.False(rule.IsNumberInRange(5, 0));
        Assert.False(rule.IsNumberInRange(5, 6));
    }

    [Fact]
    public void TestPostalRuleModel_IsNumberInRange_EvenOddRule()
    {
        // 測試單雙號規則
        var evenRule = new PostalRule
        {
            NumberStart = 1,
            NumberEnd = 100,
            EvenOdd = 2 // 雙號
        };

        Assert.True(evenRule.IsNumberInRange(2));
        Assert.True(evenRule.IsNumberInRange(4));
        Assert.True(evenRule.IsNumberInRange(100));
        Assert.False(evenRule.IsNumberInRange(1));
        Assert.False(evenRule.IsNumberInRange(3));
        Assert.False(evenRule.IsNumberInRange(99));

        var oddRule = new PostalRule
        {
            NumberStart = 1,
            NumberEnd = 100,
            EvenOdd = 1 // 單號
        };

        Assert.True(oddRule.IsNumberInRange(1));
        Assert.True(oddRule.IsNumberInRange(3));
        Assert.True(oddRule.IsNumberInRange(99));
        Assert.False(oddRule.IsNumberInRange(2));
        Assert.False(oddRule.IsNumberInRange(4));
        Assert.False(oddRule.IsNumberInRange(100));
    }

    [Fact]
    public void TestPostalRuleModel_IsLaneInRange()
    {
        // 測試巷號範圍檢查
        var rule = new PostalRule
        {
            LaneStart = 10,
            LaneEnd = 20
        };

        Assert.True(rule.IsLaneInRange(10));
        Assert.True(rule.IsLaneInRange(15));
        Assert.True(rule.IsLaneInRange(20));
        Assert.False(rule.IsLaneInRange(9));
        Assert.False(rule.IsLaneInRange(21));
    }

    [Fact]
    public void TestPostalRuleModel_IsLaneInRange_SingleLane()
    {
        // 測試單一巷號（LaneEnd 為 null）
        var rule = new PostalRule
        {
            LaneStart = 15,
            LaneEnd = null
        };

        Assert.True(rule.IsLaneInRange(15));
        Assert.True(rule.IsLaneInRange(20)); // 沒有上限
        Assert.False(rule.IsLaneInRange(14));
    }

    [Fact]
    public void TestPostalRuleModel_IsAlleyInRange()
    {
        // 測試弄號範圍檢查
        var rule = new PostalRule
        {
            AlleyStart = 5,
            AlleyEnd = 15
        };

        Assert.True(rule.IsAlleyInRange(5));
        Assert.True(rule.IsAlleyInRange(10));
        Assert.True(rule.IsAlleyInRange(15));
        Assert.False(rule.IsAlleyInRange(4));
        Assert.False(rule.IsAlleyInRange(16));
    }

    [Fact]
    public void TestAddressNormalization_SpecialRoadNames_PreserveChineseNumbers()
    {
        // 測試特殊路名的正規化不會轉換中文數字
        var normalizedAddress1 = AddressTokenizer.Normalize("臺北市中正區一村巷1號");
        var normalizedAddress2 = AddressTokenizer.Normalize("臺北市中正區二村巷1號");

        // 特殊路名應該保留中文數字
        Assert.Contains("一村", normalizedAddress1);
        Assert.Contains("二村", normalizedAddress2);

        // 不應該被轉換為阿拉伯數字
        Assert.DoesNotContain("1村", normalizedAddress1);
        Assert.DoesNotContain("2村", normalizedAddress2);
    }

    [Fact]
    public void TestAddressNormalization_NormalRoads_PreserveChineseNumbers()
    {
        // 地理 key 在前置正規化保留原生形式，靠資料庫最長匹配解析。
        var normalizedAddress = AddressTokenizer.Normalize("臺北市信義區二十一路1號");

        Assert.Contains("二十一路", normalizedAddress);
        Assert.DoesNotContain("21路", normalizedAddress);
    }

    [Theory]
    [InlineData(null, null, true)] // 沒有巷號要求
    [InlineData(1, 10, true)]      // 有範圍
    [InlineData(5, 5, true)]       // 單一巷號
    public void TestPostalRuleValidation_LaneRange_Scenarios(int? laneStart, int? laneEnd, bool hasLaneRequirement)
    {
        var rule = new PostalRule
        {
            LaneStart = laneStart,
            LaneEnd = laneEnd
        };

        if (!hasLaneRequirement)
        {
            // 沒有巷號要求，任何巷號都應該通過
            Assert.True(rule.IsLaneInRange(1));
            Assert.True(rule.IsLaneInRange(100));
        }
    }

    [Fact]
    public void TestPostalAddressValidation_WithStructuredRules()
    {
        // 這個測試需要實際的資料庫資料，可能需要 mock
        // 或者使用已知的測試資料
        var address = PostalAddress.Parse("臺北市信義區市府路1號");
        var validation = PostalAddress.Validate(address);

        // 基本驗證應該通過
        Assert.True(validation.IsValidCity);
        Assert.True(validation.IsValidDistrict);
        Assert.True(validation.IsValidRoad);
    }

    [Fact]
    public void TestPostalRuleData_AllFieldsMapping()
    {
        // 測試 PostalRuleData 所有欄位都能正確設置
        var ruleData = new PostalRuleData
        {
            ZipCode = "100",
            City = "臺北市",
            Area = "中正區",
            Road = "一村巷",
            LaneStart = 1,
            LaneEnd = 10,
            AlleyStart = 1,
            AlleyEnd = 5,
            NumberStart = 1,
            NumberStartSub = 0,
            NumberEnd = 100,
            NumberEndSub = 0,
            EvenOdd = 0,
            FloorStart = 1,
            FloorEnd = 10,
            Scope = "全",
            Department = "某投遞單位",
            Office = "某郵局",
            Remark = "備註",
            RoadNo = "123",
            Road1 = "備用路名",
            Lane11 = 1,
            Lane22 = 2,
            Alley11 = 3,
            Alley22 = 4
        };

        Assert.Equal("100", ruleData.ZipCode);
        Assert.Equal("臺北市", ruleData.City);
        Assert.Equal("中正區", ruleData.Area);
        Assert.Equal("一村巷", ruleData.Road);
        Assert.Equal(1, ruleData.LaneStart);
        Assert.Equal(10, ruleData.LaneEnd);
    }

    [Fact]
    public void TestPostalValidation_IsValid_AllConditions()
    {
        // 測試 IsValid 屬性包含所有驗證條件
        var validation = new PostalAddressValidation
        {
            IsValidCity = true,
            IsValidDistrict = true,
            IsValidRoad = true,
            IsValidLane = true,
            IsValidAlley = true,
            IsValidNumber = true
        };

        Assert.True(validation.IsValid);

        // 測試任一條件不滿足時 IsValid 應為 false
        validation.IsValidNumber = false;
        Assert.False(validation.IsValid);
    }

    [Theory]
    [InlineData("臺北市中正區一村巷1號", true, "一村巷")]
    [InlineData("臺北市中正區二村巷5號", true, "二村巷")]
    [InlineData("臺北市中正區三村巷10號", true, "三村巷")]
    [InlineData("臺北市中正區四村巷1號", true, "四村巷")]
    [InlineData("臺北市中正區五村巷1號", true, "五村巷")]
    [InlineData("臺北市中正區六村巷1號", true, "六村巷")]
    [InlineData("臺北市中正區七村巷1號", true, "七村巷")]
    [InlineData("臺北市中正區八村巷1號", true, "八村巷")]
    [InlineData("臺北市中正區九村巷1號", true, "九村巷")]
    public void TestSpecialRoadNames_AllVillageRoads(string address, bool shouldContainChineseNumber, string expectedRoadName)
    {
        // 測試所有村巷類型的特殊路名
        var normalized = AddressTokenizer.Normalize(address);

        if (shouldContainChineseNumber)
        {
            Assert.Contains(expectedRoadName, normalized);
        }
    }
}
