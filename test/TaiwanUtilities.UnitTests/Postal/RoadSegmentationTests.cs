namespace TaiwanUtilities.UnitTests;

using Xunit;

/// <summary>
/// 路名斷詞回歸測試：路名內部含 路/街/道/段 的官方道路 key
/// 曾被 regex 的非貪婪 name 在第一個單位字切斷，如「鐵路街」切成
/// Road=鐵路、Locality=街。修法改以資料庫的路名清單為準剝離最長路名。
/// </summary>
[Collection("DatabaseSingleton")]
public class RoadSegmentationTests
{
    [Theory]
    [InlineData("高雄市鼓山區樹德里鐵路街3號", "高雄市", "鼓山區", "樹德里", "鐵路街", null)]
    [InlineData("臺中市東區鐵路街550號", "臺中市", "東區", null, "鐵路街", null)]
    [InlineData("南投縣南投市三和二路一街1號", "南投縣", "南投市", null, "三和2路1街", null)]
    [InlineData("臺北市中正區忠孝東路一段1號", "臺北市", "中正區", null, "忠孝東路", "1段")]
    [InlineData("臺北市信義區市府路1號", "臺北市", "信義區", null, "市府路", null)]
    public void Parse_KnownRoadWithInnerUnitChar_SegmentsCorrectly(
        string address,
        string city,
        string district,
        string village,
        string road,
        string section)
    {
        var comp = PostalAddress.Parse(address);

        Assert.Equal(city, comp.City);
        Assert.Equal(district, comp.District);
        Assert.Equal(village, comp.Village);
        Assert.Equal(road, comp.Road);
        Assert.Equal(section, comp.Section);
    }

    [Theory]
    [InlineData("高雄市鼓山區樹德里鐵路街3號")]
    [InlineData("臺中市東區鐵路街550號")]
    [InlineData("南投縣南投市三和二路一街1號")]
    [InlineData("臺北市中正區忠孝東路一段1號")]
    [InlineData("臺北市信義區市府路1號")]
    public void Find_KnownRoadWithInnerUnitChar_ResolvesToExactMatch(string address)
    {
        var result = ZipCode.Find(address);

        Assert.Equal(ZipCodeResultType.ExactMatch, result.ResultType);
        Assert.NotEmpty(result.ZipCode);
    }
}
