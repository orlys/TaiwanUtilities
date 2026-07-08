namespace TaiwanUtilities.UnitTests;

using Xunit;

/// <summary>
/// 行政區斷詞回歸測試：區名內部含 市/鎮/鄉 的行政區（如「前鎮區」的「鎮」）
/// 曾被 regex 在內部單位字處誤切成「前鎮」+「區…路」，導致查無此區。
/// 修法改以資料庫的區清單為準剝離行政區。
/// 全台此類行政區共 4 個，全部納入回歸。
/// </summary>
[Collection("DatabaseSingleton")]
public class DistrictSegmentationTests
{
    [Theory]
    [InlineData("桃園市平鎮區三和路1號", "桃園市", "平鎮區")]     // 鎮 在內部
    [InlineData("臺南市左鎮區二寮1號", "臺南市", "左鎮區")]       // 鎮 在內部
    [InlineData("臺南市新市區三民街1號", "臺南市", "新市區")]     // 市 在內部
    [InlineData("高雄市前鎮區一心一路1號", "高雄市", "前鎮區")]   // 鎮 在內部
    public void Parse_DistrictWithInnerUnitChar_SegmentsCorrectly(string address, string city, string district)
    {
        var comp = PostalAddress.Parse(address);

        Assert.Equal(city, comp.City);
        Assert.Equal(district, comp.District);
        // 路名不應被前綴的「區」污染
        Assert.False(comp.Road?.StartsWith("區") ?? false, $"Road 不應以『區』開頭：{comp.Road}");
    }

    [Theory]
    [InlineData("桃園市平鎮區三和路1號")]
    [InlineData("臺南市左鎮區二寮1號")]
    [InlineData("臺南市新市區三民街1號")]
    [InlineData("高雄市前鎮區一心一路1號")]
    public void Find_DistrictWithInnerUnitChar_ResolvesToExactMatch(string address)
    {
        var result = ZipCode.Find(address);

        // 郵遞區號值隨季更資料變動，僅斷言能精確命中（不寫死 zip 避免脆化）
        Assert.Equal(ZipCodeResultType.ExactMatch, result.ResultType);
        Assert.NotEmpty(result.ZipCode);
    }

    [Theory]
    [InlineData("高雄市三民區十全一路1號", "三民區")]   // 一般區（無內部單位字）不受影響
    [InlineData("臺北市中正區重慶南路一段122號", "中正區")]
    [InlineData("花蓮縣秀林鄉富世291號", "秀林鄉")]     // 鄉，且無路名走 locality
    public void Parse_NormalDistrict_Unaffected(string address, string district)
    {
        Assert.Equal(district, PostalAddress.Parse(address).District);
    }
}
