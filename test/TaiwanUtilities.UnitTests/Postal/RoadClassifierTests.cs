namespace TaiwanUtilities.UnitTests;

using System;

using Xunit;

using TaiwanUtilities;

public class RoadClassifierTests
{
    [Theory]
    [InlineData("中山路", RoadType.Road)]
    [InlineData("中正街", RoadType.Street)]
    [InlineData("凱達格蘭大道", RoadType.Boulevard)]
    [InlineData("仁心路博愛巷", RoadType.Lane)]
    [InlineData("延平路二段４３０巷居易一弄", RoadType.Alley)]
    [InlineData("八德路一段", RoadType.Section)]
    public void Classify_StandardRoads_ReturnsCorrectType(string road, RoadType expected)
    {
        var actual = RoadClassifier.Classify(road);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("再興", RoadType.Settlement)]
    [InlineData("成功新屯", RoadType.Settlement)]
    [InlineData("港東新屯", RoadType.Settlement)]
    [InlineData("自強新村", RoadType.Settlement)]
    [InlineData("忠義新村", RoadType.Settlement)]
    [InlineData("弘祥新村", RoadType.Settlement)]  // 所有「XX新村」歸類為 Settlement
    public void Classify_SettlementNames_ReturnsSettlement(string road, RoadType expected)
    {
        var actual = RoadClassifier.Classify(road);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("仁愛村", RoadType.Village)]
    [InlineData("介壽村", RoadType.Village)]
    [InlineData("四維村", RoadType.Village)]
    public void Classify_Villages_ReturnsVillage(string road, RoadType expected)
    {
        var actual = RoadClassifier.Classify(road);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("金山里", RoadType.Neighborhood)]
    [InlineData("宮后里", RoadType.Neighborhood)]
    [InlineData("光復里", RoadType.Neighborhood)]
    public void Classify_Neighborhoods_ReturnsNeighborhood(string road, RoadType expected)
    {
        var actual = RoadClassifier.Classify(road);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("王軍寮", RoadType.TraditionalBuilding)]
    [InlineData("社寮", RoadType.TraditionalBuilding)]
    [InlineData("南邦寮", RoadType.TraditionalBuilding)]
    public void Classify_TraditionalBuildings_ReturnsTraditionalBuilding(string road, RoadType expected)
    {
        var actual = RoadClassifier.Classify(road);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("福德坑", RoadType.Geographic)]
    [InlineData("曲尺坑", RoadType.Geographic)]
    [InlineData("炮子崙", RoadType.Geographic)]
    [InlineData("龜子山", RoadType.Geographic)]
    [InlineData("大湖", RoadType.Geographic)]
    [InlineData("磺溪", RoadType.Geographic)]
    [InlineData("公館崙", RoadType.Geographic)]
    [InlineData("坪頂", RoadType.Geographic)]
    [InlineData("崁腳", RoadType.Geographic)]
    public void Classify_GeographicFeatures_ReturnsGeographic(string road, RoadType expected)
    {
        var actual = RoadClassifier.Classify(road);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("中山路新市場", RoadType.Market)]
    [InlineData("公有市場", RoadType.Market)]
    [InlineData("建國市場", RoadType.Market)]
    public void Classify_Markets_ReturnsMarket(string road, RoadType expected)
    {
        var actual = RoadClassifier.Classify(road);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("花源一街地下層", RoadType.Basement)]
    [InlineData("大同路地下商場", RoadType.Basement)]
    [InlineData("中山地下街", RoadType.Basement)]
    public void Classify_Basements_ReturnsBasement(string road, RoadType expected)
    {
        var actual = RoadClassifier.Classify(road);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("重慶街南門商場", RoadType.ShoppingCenter)]
    [InlineData("光華商場", RoadType.ShoppingCenter)]
    public void Classify_ShoppingCenters_ReturnsShoppingCenter(string road, RoadType expected)
    {
        var actual = RoadClassifier.Classify(road);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("二棟", RoadType.Building)]
    [InlineData("三棟", RoadType.Building)]
    public void Classify_Buildings_ReturnsBuilding(string road, RoadType expected)
    {
        var actual = RoadClassifier.Classify(road);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("2樓", RoadType.Floor)]
    [InlineData("3樓", RoadType.Floor)]
    [InlineData("10層", RoadType.Floor)]
    public void Classify_Floors_ReturnsFloor(string road, RoadType expected)
    {
        var actual = RoadClassifier.Classify(road);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("富國新城", RoadType.ResidentialComplex)]
    [InlineData("陽明山莊", RoadType.ResidentialComplex)]
    [InlineData("明德社區", RoadType.ResidentialComplex)]
    [InlineData("幸福家園", RoadType.ResidentialComplex)]
    public void Classify_ResidentialComplexes_ReturnsResidentialComplex(string road, RoadType expected)
    {
        var actual = RoadClassifier.Classify(road);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("加工出口區工業區", RoadType.IndustrialZone)]
    [InlineData("中港工業區", RoadType.IndustrialZone)]
    public void Classify_IndustrialZones_ReturnsIndustrialZone(string road, RoadType expected)
    {
        var actual = RoadClassifier.Classify(road);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("基隆嶼", RoadType.Island)]
    [InlineData("彭佳嶼", RoadType.Island)]
    [InlineData("龜山嶼", RoadType.Island)]
    public void Classify_Islands_ReturnsIsland(string road, RoadType expected)
    {
        var actual = RoadClassifier.Classify(road);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("中正紀念公園", RoadType.Park)]
    [InlineData("大安森林公園", RoadType.Park)]
    [InlineData("二二八和平公園", RoadType.Park)]
    public void Classify_Parks_ReturnsPark(string road, RoadType expected)
    {
        var actual = RoadClassifier.Classify(road);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("一號碼頭", RoadType.Dock)]
    [InlineData("十二號碼頭", RoadType.Dock)]
    public void Classify_Docks_ReturnsDock(string road, RoadType expected)
    {
        var actual = RoadClassifier.Classify(road);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("中興", RoadType.Settlement)]  // 「興」結尾
    [InlineData("新興", RoadType.Settlement)]
    [InlineData("廣興", RoadType.Settlement)]
    [InlineData("復興", RoadType.Settlement)]
    public void Classify_XingNames_ReturnsSettlement(string road, RoadType expected)
    {
        var actual = RoadClassifier.Classify(road);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("李棟山", RoadType.Geographic)]  // 含「棟」但有「山」，應為地理特徵
    [InlineData("觀音山", RoadType.Geographic)]  // 「山」結尾
    public void Classify_MountainsWithBuildingKeyword_ReturnsGeographic(string road, RoadType expected)
    {
        var actual = RoadClassifier.Classify(road);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("層", RoadType.Geographic)]  // 單獨「層」無數字，應為地理特徵
    public void Classify_LayerWithoutDigits_ReturnsGeographic(string road, RoadType expected)
    {
        var actual = RoadClassifier.Classify(road);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Classify_EmptyString_ReturnsUnknown()
    {
        var actual = RoadClassifier.Classify("");
        Assert.Equal(RoadType.Unknown, actual);
    }

    [Fact]
    public void Classify_NullString_ReturnsUnknown()
    {
        var actual = RoadClassifier.Classify(null!);
        Assert.Equal(RoadType.Unknown, actual);
    }

    [Fact]
    public void IsStandardRoad_Road_ReturnsTrue()
    {
        Assert.True(RoadClassifier.IsStandardRoad("中山路"));
        Assert.True(RoadClassifier.IsStandardRoad("中正街"));
        Assert.True(RoadClassifier.IsStandardRoad("凱達格蘭大道"));
    }

    [Fact]
    public void IsStandardRoad_Settlement_ReturnsFalse()
    {
        Assert.False(RoadClassifier.IsStandardRoad("再興"));
        Assert.False(RoadClassifier.IsStandardRoad("福德坑"));
        Assert.False(RoadClassifier.IsStandardRoad("王軍寮"));
    }

    [Fact]
    public void GetDescription_ReturnsCorrectDescription()
    {
        Assert.Equal("路", RoadClassifier.GetDescription(RoadType.Road));
        Assert.Equal("歷史聚落/眷村", RoadClassifier.GetDescription(RoadType.Settlement));
        Assert.Equal("地理特徵", RoadClassifier.GetDescription(RoadType.Geographic));
    }
}
