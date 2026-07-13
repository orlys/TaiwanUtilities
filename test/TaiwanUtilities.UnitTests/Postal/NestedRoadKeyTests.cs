namespace TaiwanUtilities.UnitTests;

using Xunit;

/// <summary>
/// 巢狀路名鍵拆解回歸測試：資料庫裡有些路名鍵包著另一條規則
/// （如「中正路一段篤行三村」的前綴「中正路一段」本身也是同區獨立 group）。
/// 命中時前綴拆為 Road/Section、尾段依型態歸 Lane/Alley/Locality；
/// 但查詢（ZipCode）仍以完整原生鍵為準，郵遞區號不受拆解影響。
/// </summary>
[Collection("DatabaseSingleton")]
public class NestedRoadKeyTests
{
    [Theory]
    // 段 + 眷村/廠區聚落 → Road + Section + Locality
    [InlineData("宜蘭縣五結鄉中正路一段篤行三村1號", "中正路", "一段", null, null, "篤行三村")]
    [InlineData("彰化縣彰化市彰南路一段台化一莊1號", "彰南路", "一段", null, null, "台化一莊")]
    // 路 + 具名巷 → Road + Lane
    [InlineData("南投縣南投市中興路中一巷3號", "中興路", null, "中一巷", null, null)]
    // 巷（母路）+ 附加資訊 → Road=信筆巷 + Locality
    [InlineData("南投縣信義鄉信筆巷信和產業道1號", "信筆巷", null, null, null, "信和產業道")]
    public void Parse_NestedCompoundKey_DecomposesForDisplay(
        string address, string road, string section, string lane, string alley, string locality)
    {
        var comp = PostalAddress.Parse(address);
        Assert.Equal(road, comp.Road);
        Assert.Equal(section, comp.Section);
        Assert.Equal(lane, comp.Lane);
        Assert.Equal(alley, comp.Alley);
        Assert.Equal(locality, comp.Locality);
    }

    [Theory]
    // 純段（無聚落）不可誤拆成 Locality
    [InlineData("嘉義縣太保市祥和一路東段1號", "祥和一路", "東段")]
    [InlineData("南投縣南投市中正路一段5號", "中正路", "一段")]
    // 編號分支路（一街）緊附母路，整串留作 Road
    [InlineData("南投縣南投市三和二路一街1號", "三和二路一街", null)]
    public void Parse_NotOverSplit_KeepsSectionOrWholeRoad(string address, string road, string section)
    {
        var comp = PostalAddress.Parse(address);
        Assert.Equal(road, comp.Road);
        Assert.Equal(section, comp.Section);
        Assert.Null(comp.Locality);
    }

    [Theory]
    // 拆解只影響顯示欄位；查詢以完整鍵為準，zip 與拆解前一致
    [InlineData("宜蘭縣五結鄉中正路一段篤行三村1號", "268018")]
    [InlineData("彰化縣彰化市彰南路一段台化一莊1號", "500041")]
    [InlineData("南投縣南投市中興路中一巷3號", "540001")]
    [InlineData("南投縣信義鄉信筆巷信和產業道1號", "556004")]
    public void Find_CompoundKey_ResolvesOnFullNativeKey(string address, string expectedZip)
    {
        var result = ZipCode.Find(address);
        Assert.Equal(ZipCodeResultType.ExactMatch, result.ResultType);
        Assert.Equal(expectedZip, result.ZipCode);
    }
}
