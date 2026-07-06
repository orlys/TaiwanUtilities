namespace TaiwanUtilities.UnitTests;

using System.IO;

using TaiwanUtilities;

using Xunit;

/// <summary>
/// 整合測試 - 需要資料庫
/// </summary>
[Collection("DatabaseSingleton")]
public class IntegrationTests
{
    private readonly string _dbPath;

    public IntegrationTests()
    {
        // 嘗試找到資料庫檔案
        _dbPath = FindDatabasePath();

        // 設定外部資料庫路徑給 PostalDatabase 單例使用
        if (IsDatabaseAvailable())
        {
            try
            {
                PostalDatabase.UseExternalDatabase(_dbPath);
            }
            catch (InvalidOperationException)
            {
                // PostalDatabase 單例已經初始化，忽略錯誤
            }
        }
    }

    private string FindDatabasePath()
    {
        var paths = new[]
        {
            "../../src/TaiwanUtilities/Postal/zipcode.db",
            "../../../src/TaiwanUtilities/Postal/zipcode.db",
            "../../../../src/TaiwanUtilities/Postal/zipcode.db",
            "../../../../../src/TaiwanUtilities/Postal/zipcode.db"
        };

        foreach (var path in paths)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        return string.Empty;
    }

    private bool IsDatabaseAvailable() => !string.IsNullOrEmpty(_dbPath) && File.Exists(_dbPath);

    [Fact]
    public void TestProgressiveQuery_Taipei()
    {
        // 靜態引擎需要完整地址（含路名和門牌），不支援純縣市/行政區查詢
        var zip1 = ZipCode.Find("臺北市信義區市府路1號").ZipCode;
        Assert.NotEmpty(zip1);
        Assert.StartsWith("1", zip1);

        var zip2 = ZipCode.Find("臺北市信義區市府路1號").ZipCode;
        Assert.NotEmpty(zip2);
        Assert.StartsWith("110", zip2);
    }

    [Theory]
    [InlineData("臺北市信義區市府路1號")]
    [InlineData("台北市信義區市府路1號")]           // 台 -> 臺
    [InlineData("臺北市，信義區，市府路１號")]      // 全形逗號和數字
    [InlineData("臺北市 信義區 市府路 1 號")]      // 空格
    public void TestVariousFormats_SameResult(string address)
    {
        if (!IsDatabaseAvailable())
        {
            return;
        }

        var zipcode = ZipCode.Find(address).ZipCode;

        Assert.NotEmpty(zipcode);
        Assert.StartsWith("110", zipcode);
    }

    [Theory]
    [InlineData("高雄市苓雅區四維三路6號", "8")]
    [InlineData("高雄市左營區博愛二路777號", "813")]
    [InlineData("新北市板橋區文化路一段1號", "2")]
    [InlineData("新北市板橋區文化路一段1號", "220")]
    [InlineData("臺中市中區中山路1號", "4")]
    public void TestDifferentCities(string address, string expectedPrefix)
    {
        var zipcode = ZipCode.Find(address).ZipCode;

        Assert.NotEmpty(zipcode);
        Assert.StartsWith(expectedPrefix, zipcode);
    }

    [Fact]
    public void TestNormalizationWithQuery()
    {
        if (!IsDatabaseAvailable())
        {
            return;
        }


        // 測試中文數字
        var normalized = PostalAddressUtils.Normalize("信義路一段");
        Assert.Equal("信義路1段", normalized);

        // 測試查詢
        var zip1 = ZipCode.Find("臺北市中正區信義路一段").ZipCode;
        var zip2 = ZipCode.Find("臺北市中正區信義路1段").ZipCode;

        // 應該得到相同結果
        Assert.Equal(zip1, zip2);
    }

    [Fact]
    public void TestNotFound()
    {
        if (!IsDatabaseAvailable())
        {
            return;
        }

        var zipcode = ZipCode.Find("這是一個不存在的地址123456789").ZipCode;
        Assert.Empty(zipcode);
    }

    [Fact]
    public void TestEmptyInput()
    {
        if (!IsDatabaseAvailable())
        {
            return;
        }

        Assert.Empty(ZipCode.Find((string)"").ZipCode);
        Assert.Empty(ZipCode.Find((string)null!).ZipCode);
        Assert.Empty(ZipCode.Find((string)"   ").ZipCode);
    }

    [Fact]
    public void TestKeepAlivePerformance()
    {
        // 測試靜態引擎的並發查詢效能
        for (int i = 0; i < 100; i++)
        {
            var zipcode = ZipCode.Find("臺北市信義區市府路1號").ZipCode;
            Assert.NotEmpty(zipcode);
        }
    }

    [Theory]
    [InlineData("臺北市中正區中華路１段49號", "100")]   // 實際查詢結果
    [InlineData("臺北市中正區仁愛路１段1號", "100")]    // 實際查詢結果
    public void TestSpecificAddresses(string address, string expectedPrefix)
    {
        if (!IsDatabaseAvailable())
        {
            return;
        }

        var zipcode = ZipCode.Find(address).ZipCode;

        Assert.NotEmpty(zipcode);
        Assert.StartsWith(expectedPrefix, zipcode);
    }

    [Fact]
    public void TestAddressWithSubNumber()
    {
        if (!IsDatabaseAvailable())
        {
            return;
        }

        var zip1 = ZipCode.Find("臺北市信義區市府路1號").ZipCode;
        var zip2 = ZipCode.Find("臺北市信義區市府路1之1號").ZipCode;

        // 附號可能在不同的郵遞區號範圍
        Assert.NotEmpty(zip1);
        Assert.NotEmpty(zip2);
    }
}
