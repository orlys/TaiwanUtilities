namespace TaiwanUtilities.UnitTests;

using Xunit;

[Collection("DatabaseSingleton")]
public class PostalNormalizationLayerSmokeTests
{
    [Theory]
    [InlineData("新竹縣竹北市光明十一路3號")]
    [InlineData("新竹縣竹北市光明11路3號")]
    [InlineData("桃園市復興區溪口台1號")]
    [InlineData("南投縣中寮鄉永嘉新村1號")]
    [InlineData("高雄市苓雅區四維三路6號")]
    [InlineData("高雄市苓雅區四維3路6號")]
    [InlineData("台中市西屯區台灣大道三段99號")]
    [InlineData("臺北市中正區忠孝東路一段1號")]
    public void Find_NormalizationLayerSmokeCases_ReturnExactMatch(string address)
    {
        var result = ZipCode.Find(address);

        Assert.Equal(ZipCodeResultType.ExactMatch, result.ResultType);
        Assert.NotEmpty(result.ZipCode);
    }
}
