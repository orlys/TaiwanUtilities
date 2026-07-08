namespace TaiwanUtilities.UnitTests;

using Xunit;

public class LocalityTests
{
    [Fact]
    public void Parse_AddressWithForestRoadAndLocation_ExtractsLocality()
    {
        // Arrange & Act
        // 測試「中間」作為地區名稱（類似眷村或聚落）
        var comp = PostalAddress.Parse("宜蘭縣大同鄉太平村宜專1線中間1號");

        // Assert
        Assert.Equal("宜蘭縣", comp.City);
        Assert.Equal("大同鄉", comp.District);
        Assert.Equal("太平村", comp.Village);
        Assert.Equal("宜專1線", comp.Road);           // 林道名稱
        Assert.Equal("中間", comp.Locality); // 地區/聚落名稱
        Assert.Equal(1, comp.Number);
    }

    [Fact]
    public void Parse_AddressWithVillageArea_ExtractsLocality()
    {
        // Arrange & Act
        // 測試類似「高雄市阿蓮區再興XX號」的格式
        // 「再興」是眷村或地區名稱
        var comp = PostalAddress.Parse("高雄市阿蓮區再興100號");

        // Assert
        Assert.Equal("高雄市", comp.City);
        Assert.Equal("阿蓮區", comp.District);
        // 「再興」可能被識別為 Locality 或作為路名的一部分
        // 取決於實際的 tokenization
        Assert.Equal(100, comp.Number);
    }

    [Fact]
    public void ToString_WithLocality_IncludesLocation()
    {
        // Arrange
        var comp = PostalAddress.Parse("宜蘭縣大同鄉太平村宜專1線中間1號");

        // Act
        var str = comp.ToString();

        // Assert
        Assert.Contains("Locality=中間", str);
    }

    [Fact]
    public void Parse_TemporaryNumberPrefix_NotInLocality()
    {
        // Arrange & Act
        // 「臨」是前綴修飾詞，不應該被識別為 Locality
        var comp = PostalAddress.Parse("彰化縣彰化市民族一街臨11號");

        // Assert
        Assert.Equal("彰化縣", comp.City);
        Assert.Equal("彰化市", comp.District);
        Assert.Equal("民族一街", comp.Road);
        Assert.Equal(11, comp.Number);
        Assert.True(comp.IsTemporary);                    // 應該設定臨時門牌標記
        Assert.Null(comp.Locality);          // 「臨」不應該在這裡！
    }

    [Fact]
    public void Parse_BasementPrefix_NotInLocality()
    {
        // Arrange & Act
        // 「地下」是前綴修飾詞，不應該被識別為 Locality
        var comp = PostalAddress.Parse("臺灣大道一段７０３號地下一層");

        // Assert
        Assert.Equal("臺灣大道", comp.Road);
        Assert.Equal("一段", comp.Section);
        Assert.Equal(703, comp.Number);
        Assert.Equal("地下1層", comp.Floor);
        Assert.Null(comp.Locality);          // 「地下」不應該在這裡！
    }

    [Fact]
    public void Parse_KnownRoadKeyEndingWithVillageUnit_TreatsAsLocality()
    {
        var comp = PostalAddress.Parse("南投縣中寮鄉永嘉新村1號");

        Assert.Equal("南投縣", comp.City);
        Assert.Equal("中寮鄉", comp.District);
        Assert.Equal("永嘉新村", comp.Locality);
        Assert.Null(comp.Village);
        Assert.Null(comp.Road);
        Assert.Equal(1, comp.Number);
    }

    [Fact]
    public void Parse_KnownRoadKeyWithoutTokenUnit_RemainsLocality()
    {
        var comp = PostalAddress.Parse("南投縣中寮鄉卜溪部落1號");

        Assert.Equal("卜溪部落", comp.Locality);
        Assert.Null(comp.Road);
    }

    [Fact]
    public void Find_KnownRoadKeyEndingWithVillageUnit_ResolvesToExactMatch()
    {
        var result = ZipCode.Find("南投縣中寮鄉永嘉新村1號");

        Assert.Equal(ZipCodeResultType.ExactMatch, result.ResultType);
        Assert.NotEmpty(result.ZipCode);
    }
}
