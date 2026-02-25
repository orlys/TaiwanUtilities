namespace TaiwanUtilities.UnitTests;

using System;

using Xunit;

using TaiwanUtilities;

public class PostalRoadClassifierTests
{
    [Theory]
    [InlineData("中山路", PostalRoadType.Road)]
    [InlineData("中正街", PostalRoadType.Street)]
    [InlineData("凱達格蘭大道", PostalRoadType.Boulevard)]
    [InlineData("仁心路博愛巷", PostalRoadType.Lane)]
    [InlineData("延平路二段４３０巷居易一弄", PostalRoadType.Alley)]
    [InlineData("八德路一段", PostalRoadType.Section)]
    public void Classify_StandardRoads_ReturnsCorrectType(string road, PostalRoadType expected)
    {
        var actual = PostalRoadClassifier.Classify(road);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("再興", PostalRoadType.Settlement)]
    [InlineData("成功新屯", PostalRoadType.Settlement)]
    [InlineData("港東新屯", PostalRoadType.Settlement)]
    [InlineData("自強新村", PostalRoadType.Settlement)]
    [InlineData("忠義新村", PostalRoadType.Settlement)]
    [InlineData("弘祥新村", PostalRoadType.Settlement)]  // 所有「XX新村」歸類為 Settlement
    public void Classify_SettlementNames_ReturnsSettlement(string road, PostalRoadType expected)
    {
        var actual = PostalRoadClassifier.Classify(road);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("仁愛村", PostalRoadType.Village)]
    [InlineData("介壽村", PostalRoadType.Village)]
    [InlineData("四維村", PostalRoadType.Village)]
    public void Classify_Villages_ReturnsVillage(string road, PostalRoadType expected)
    {
        var actual = PostalRoadClassifier.Classify(road);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("金山里", PostalRoadType.Neighborhood)]
    [InlineData("宮后里", PostalRoadType.Neighborhood)]
    [InlineData("光復里", PostalRoadType.Neighborhood)]
    public void Classify_Neighborhoods_ReturnsNeighborhood(string road, PostalRoadType expected)
    {
        var actual = PostalRoadClassifier.Classify(road);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("王軍寮", PostalRoadType.TraditionalBuilding)]
    [InlineData("社寮", PostalRoadType.TraditionalBuilding)]
    [InlineData("南邦寮", PostalRoadType.TraditionalBuilding)]
    public void Classify_TraditionalBuildings_ReturnsTraditionalBuilding(string road, PostalRoadType expected)
    {
        var actual = PostalRoadClassifier.Classify(road);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("福德坑", PostalRoadType.Geographic)]
    [InlineData("曲尺坑", PostalRoadType.Geographic)]
    [InlineData("炮子崙", PostalRoadType.Geographic)]
    [InlineData("龜子山", PostalRoadType.Geographic)]
    [InlineData("大湖", PostalRoadType.Geographic)]
    [InlineData("磺溪", PostalRoadType.Geographic)]
    [InlineData("公館崙", PostalRoadType.Geographic)]
    [InlineData("坪頂", PostalRoadType.Geographic)]
    [InlineData("崁腳", PostalRoadType.Geographic)]
    public void Classify_GeographicFeatures_ReturnsGeographic(string road, PostalRoadType expected)
    {
        var actual = PostalRoadClassifier.Classify(road);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("中山路新市場", PostalRoadType.Market)]
    [InlineData("公有市場", PostalRoadType.Market)]
    [InlineData("建國市場", PostalRoadType.Market)]
    public void Classify_Markets_ReturnsMarket(string road, PostalRoadType expected)
    {
        var actual = PostalRoadClassifier.Classify(road);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("花源一街地下層", PostalRoadType.Basement)]
    [InlineData("大同路地下商場", PostalRoadType.Basement)]
    [InlineData("中山地下街", PostalRoadType.Basement)]
    public void Classify_Basements_ReturnsBasement(string road, PostalRoadType expected)
    {
        var actual = PostalRoadClassifier.Classify(road);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("重慶街南門商場", PostalRoadType.ShoppingCenter)]
    [InlineData("光華商場", PostalRoadType.ShoppingCenter)]
    public void Classify_ShoppingCenters_ReturnsShoppingCenter(string road, PostalRoadType expected)
    {
        var actual = PostalRoadClassifier.Classify(road);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("二棟", PostalRoadType.Building)]
    [InlineData("三棟", PostalRoadType.Building)]
    public void Classify_Buildings_ReturnsBuilding(string road, PostalRoadType expected)
    {
        var actual = PostalRoadClassifier.Classify(road);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("2樓", PostalRoadType.Floor)]
    [InlineData("3樓", PostalRoadType.Floor)]
    [InlineData("10層", PostalRoadType.Floor)]
    public void Classify_Floors_ReturnsFloor(string road, PostalRoadType expected)
    {
        var actual = PostalRoadClassifier.Classify(road);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("富國新城", PostalRoadType.ResidentialComplex)]
    [InlineData("陽明山莊", PostalRoadType.ResidentialComplex)]
    [InlineData("明德社區", PostalRoadType.ResidentialComplex)]
    [InlineData("幸福家園", PostalRoadType.ResidentialComplex)]
    public void Classify_ResidentialComplexes_ReturnsResidentialComplex(string road, PostalRoadType expected)
    {
        var actual = PostalRoadClassifier.Classify(road);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("加工出口區工業區", PostalRoadType.IndustrialZone)]
    [InlineData("中港工業區", PostalRoadType.IndustrialZone)]
    public void Classify_IndustrialZones_ReturnsIndustrialZone(string road, PostalRoadType expected)
    {
        var actual = PostalRoadClassifier.Classify(road);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("基隆嶼", PostalRoadType.Island)]
    [InlineData("彭佳嶼", PostalRoadType.Island)]
    [InlineData("龜山嶼", PostalRoadType.Island)]
    public void Classify_Islands_ReturnsIsland(string road, PostalRoadType expected)
    {
        var actual = PostalRoadClassifier.Classify(road);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("中正紀念公園", PostalRoadType.Park)]
    [InlineData("大安森林公園", PostalRoadType.Park)]
    [InlineData("二二八和平公園", PostalRoadType.Park)]
    public void Classify_Parks_ReturnsPark(string road, PostalRoadType expected)
    {
        var actual = PostalRoadClassifier.Classify(road);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("一號碼頭", PostalRoadType.Dock)]
    [InlineData("十二號碼頭", PostalRoadType.Dock)]
    public void Classify_Docks_ReturnsDock(string road, PostalRoadType expected)
    {
        var actual = PostalRoadClassifier.Classify(road);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("中興", PostalRoadType.Settlement)]  // 「興」結尾
    [InlineData("新興", PostalRoadType.Settlement)]
    [InlineData("廣興", PostalRoadType.Settlement)]
    [InlineData("復興", PostalRoadType.Settlement)]
    public void Classify_XingNames_ReturnsSettlement(string road, PostalRoadType expected)
    {
        var actual = PostalRoadClassifier.Classify(road);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("李棟山", PostalRoadType.Geographic)]  // 含「棟」但有「山」，應為地理特徵
    [InlineData("觀音山", PostalRoadType.Geographic)]  // 「山」結尾
    public void Classify_MountainsWithBuildingKeyword_ReturnsGeographic(string road, PostalRoadType expected)
    {
        var actual = PostalRoadClassifier.Classify(road);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("層", PostalRoadType.Geographic)]  // 單獨「層」無數字，應為地理特徵
    public void Classify_LayerWithoutDigits_ReturnsGeographic(string road, PostalRoadType expected)
    {
        var actual = PostalRoadClassifier.Classify(road);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Classify_EmptyString_ReturnsUnknown()
    {
        var actual = PostalRoadClassifier.Classify("");
        Assert.Equal(PostalRoadType.Unknown, actual);
    }

    [Fact]
    public void Classify_NullString_ReturnsUnknown()
    {
        var actual = PostalRoadClassifier.Classify(null!);
        Assert.Equal(PostalRoadType.Unknown, actual);
    }

    [Fact]
    public void IsStandardRoad_Road_ReturnsTrue()
    {
        Assert.True(PostalRoadClassifier.IsStandardRoad("中山路"));
        Assert.True(PostalRoadClassifier.IsStandardRoad("中正街"));
        Assert.True(PostalRoadClassifier.IsStandardRoad("凱達格蘭大道"));
    }

    [Fact]
    public void IsStandardRoad_Settlement_ReturnsFalse()
    {
        Assert.False(PostalRoadClassifier.IsStandardRoad("再興"));
        Assert.False(PostalRoadClassifier.IsStandardRoad("福德坑"));
        Assert.False(PostalRoadClassifier.IsStandardRoad("王軍寮"));
    }

    [Fact]
    public void GetDescription_ReturnsCorrectDescription()
    {
        Assert.Equal("路", PostalRoadClassifier.GetDescription(PostalRoadType.Road));
        Assert.Equal("歷史聚落/眷村", PostalRoadClassifier.GetDescription(PostalRoadType.Settlement));
        Assert.Equal("地理特徵", PostalRoadClassifier.GetDescription(PostalRoadType.Geographic));
    }
}
