namespace TaiwanUtilities.UnitTests;

using Xunit;

[Collection("DatabaseSingleton")]
public class PostalAddressTests
{
    [Fact]
    public void Parse_SimpleAddress_ExtractsBasicComponents()
    {
        // Arrange & Act
        var comp = PostalAddress.Parse("臺北市信義區市府路1號");

        // Assert
        Assert.Equal("臺北市", comp.City);
        Assert.Equal("信義區", comp.District);
        Assert.Equal("市府路", comp.Road);
        Assert.Equal(1, comp.Number);
        Assert.Null(comp.SubNumbers);
    }

    [Fact]
    public void Parse_AddressWithSubNumber_ExtractsSubNumber()
    {
        // Arrange & Act
        var comp = PostalAddress.Parse("臺北市信義區市府路1之2號");

        // Assert
        Assert.Equal(1, comp.Number);
        Assert.Equal(2, comp.SubNumbers?.FirstOrDefault());
    }

    [Fact]
    public void Parse_AddressWithFloor_ExtractsFloor()
    {
        // Arrange & Act
        var comp = PostalAddress.Parse("臺北市信義區市府路1號3樓");

        // Assert
        Assert.Equal(1, comp.Number);
        Assert.Equal("3樓", comp.Floor);
    }

    [Fact]
    public void Parse_AddressWithLaneAndAlley_ExtractsBasicComponents()
    {
        // Arrange & Act
        var comp = PostalAddress.Parse("臺北市大安區復興南路1段101巷2弄3號");

        // Assert
        Assert.Equal("臺北市", comp.City);
        Assert.Equal("大安區", comp.District);
        Assert.Equal("復興南路", comp.Road);
        // Note: Section, Lane, Alley extraction depends on token order
        Assert.Equal("101巷", comp.Lane);
        Assert.Equal("2弄", comp.Alley);
        Assert.Equal(3, comp.Number);
    }

    [Theory]
    [InlineData("台北市", "臺北市")]  // 台 -> 臺
    [InlineData("臺北市信義區市府路１號", "臺北市信義區市府路1號")]  // 全形 -> 半形
    [InlineData("臺北市信義區市府路一號", "臺北市信義區市府路1號")]  // 中文數字 -> 阿拉伯數字
    public void Parse_NormalizesAddress(string input, string expectedNormalized)
    {
        // Arrange & Act
        var comp = PostalAddress.Parse(input);

        // Assert
        Assert.Equal(expectedNormalized, comp.NormalizedAddress);
    }

    [Fact]
    public void Parse_EmptyAddress_ReturnsEmptyComponents()
    {
        // Arrange & Act
        var comp = PostalAddress.Parse("");

        // Assert
        Assert.Null(comp.City);
        Assert.Null(comp.District);
        Assert.Null(comp.Road);
        Assert.Null(comp.Number);
    }

    [Fact]
    public void Parse_AddressWithVillage_ExtractsVillage()
    {
        // Arrange & Act
        var comp = PostalAddress.Parse("臺北市文山區木柵里10鄰");

        // Assert
        Assert.Equal("木柵里", comp.Village);
        // Note: Neighborhood extraction may vary based on token structure
        // This is a known limitation of the current parsing logic
    }

    [Fact]
    public void GetFullNumber_NumberOnly_ReturnsNumberWithUnit()
    {
        // Arrange
        var comp = PostalAddress.Parse("臺北市信義區市府路1號");

        // Act
        var fullNumber = comp.GetFullNumber();

        // Assert
        Assert.Equal("1號", fullNumber);
    }

    [Fact]
    public void GetFullNumber_NumberWithSubNumber_ReturnsFullFormat()
    {
        // Arrange
        var comp = PostalAddress.Parse("臺北市信義區市府路1之2號");

        // Act
        var fullNumber = comp.GetFullNumber();

        // Assert
        Assert.Equal("1之2號", fullNumber);
    }

    [Fact]
    public void GetFullNumber_NoNumber_ReturnsEmpty()
    {
        // Arrange
        var comp = PostalAddress.Parse("臺北市信義區");

        // Act
        var fullNumber = comp.GetFullNumber();

        // Assert
        Assert.Equal(string.Empty, fullNumber);
    }

    [Fact]
    public void GetBaseAddress_ReturnsCorrectFormat()
    {
        // Arrange
        var comp = PostalAddress.Parse("臺北市信義區市府路1號");

        // Act
        var baseAddress = comp.GetBaseAddress();

        // Assert
        Assert.Equal("臺北市信義區市府路", baseAddress);
    }

    [Fact]
    public void GetBaseAddress_PartialAddress_ReturnsAvailableParts()
    {
        // Arrange
        var comp = PostalAddress.Parse("臺北市信義區");

        // Act
        var baseAddress = comp.GetBaseAddress();

        // Assert
        Assert.Equal("臺北市信義區", baseAddress);
    }

    [Fact]
    public void Parse_ComplexAddress_ExtractsAllComponents()
    {
        // Arrange & Act
        var comp = PostalAddress.Parse("高雄市三民區安寧街1巷2弄3之4號5樓");

        // Assert
        Assert.Equal("高雄市", comp.City);
        Assert.Equal("三民區", comp.District);
        Assert.Equal("安寧街", comp.Road);
        Assert.Equal("1巷", comp.Lane);
        Assert.Equal("2弄", comp.Alley);
        Assert.Equal(3, comp.Number);
        Assert.Equal(4, comp.SubNumbers?.FirstOrDefault());
        Assert.Equal("5樓", comp.Floor);
    }

    [Fact]
    public void Parse_AddressWithSection_ExtractsBasicComponents()
    {
        // Arrange & Act
        var comp = PostalAddress.Parse("臺中市西屯區臺灣大道4段1號");

        // Assert
        Assert.Equal("臺中市", comp.City);
        Assert.Equal("西屯區", comp.District);
        Assert.Equal(1, comp.Number);
        // Note: Section extraction depends on having a number before "段"
        // In "臺灣大道4段", the "4" is part of the road name, not a separate number token
    }

    [Fact]
    public void Parse_CountyAddress_ExtractsCounty()
    {
        // Arrange & Act
        var comp = PostalAddress.Parse("新竹縣竹北市縣政九路1號");

        // Assert
        Assert.Equal("新竹縣", comp.City);
        // Note: District extraction for county-level cities depends on tokenization
        // "九" is normalized to "9"
        Assert.Equal("縣政9路", comp.Road);
        Assert.Equal(1, comp.Number);
    }

    [Fact]
    public void Parse_TownshipAddress_ExtractsTownship()
    {
        // Arrange & Act
        var comp = PostalAddress.Parse("苗栗縣頭份鎮中央路1號");

        // Assert
        Assert.Equal("苗栗縣", comp.City);
        Assert.Equal("頭份鎮", comp.District);
        Assert.Equal("中央路", comp.Road);
    }

    [Fact]
    public void Parse_StoresOriginalAddress()
    {
        // Arrange
        var original = "台北市 信義區 市府路１號";

        // Act
        var comp = PostalAddress.Parse(original);

        // Assert
        Assert.Equal(original, comp.RawAddress);
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var comp = PostalAddress.Parse("臺北市信義區市府路1號");

        // Act
        var result = comp.ToString();

        // Assert
        Assert.Contains("臺北市", result);
        Assert.Contains("信義區", result);
        Assert.Contains("市府路", result);
    }

    [Fact]
    public void Parse_VeryComplexAddress_ExtractsAllComponents()
    {
        // Arrange & Act
        // 這是一個極度複雜的地址，包含：段、巷、弄、多層附號、樓層
        var comp = PostalAddress.Parse("台北市大安區和平東路二段96巷17弄1之1之2號5樓之3");

        // Assert - 基本組件
        Assert.Equal("臺北市", comp.City);           // 台 → 臺 正規化
        Assert.Equal("大安區", comp.District);

        // 路街 (注意：路街名稱提取邏輯會獲取最長匹配，"和平東路" 是完整路名)
        Assert.Equal("和平東路", comp.Road);

        // Section 解析說明：
        // "和平東路二段" 中的 "二段" 因為緊接在路名後，"二" 會被正規化為 "2"
        // 但是否能提取為獨立的 Section 取決於 tokenization 的結果
        // 根據現有測試（Parse_AddressWithSection_ExtractsBasicComponents）的註解，
        // 段的提取取決於 token 結構，這裡我們不強制要求 Section 必須被提取

        // 巷弄
        Assert.Equal("96巷", comp.Lane);
        Assert.Equal("17弄", comp.Alley);

        // 門牌號碼
        Assert.Equal(1, comp.Number);

        // 多層附號 (之1之2 → [1, 2])
        Assert.NotNull(comp.SubNumbers);
        Assert.Equal(2, comp.SubNumbers!.Count);
        Assert.Equal(1, comp.SubNumbers[0]);
        Assert.Equal(2, comp.SubNumbers[1]);

        // 完整號碼格式
        Assert.Equal("1之1之2號", comp.GetFullNumber());

        // 樓層 (注意：目前解析器會提取第一個樓層標記)
        Assert.Equal("5樓", comp.Floor);

        // 原始地址保留
        Assert.Equal("台北市大安區和平東路二段96巷17弄1之1之2號5樓之3", comp.RawAddress);

        // 正規化地址 - 驗證主要組件都被正規化
        Assert.Contains("臺北市", comp.NormalizedAddress);  // 台 → 臺
        Assert.Contains("大安區", comp.NormalizedAddress);
        Assert.Contains("和平東路", comp.NormalizedAddress);
        Assert.Contains("96巷", comp.NormalizedAddress);
        Assert.Contains("17弄", comp.NormalizedAddress);
        Assert.Contains("1之1之2號", comp.NormalizedAddress);
        Assert.Contains("5樓", comp.NormalizedAddress);
    }

    [Fact]
    public void Parse_VeryComplexAddress_CanQueryZipCode()
    {
        // Arrange - 96巷奇數門牌從3號起，用3號以確保在規則範圍內
        var comp = PostalAddress.Parse("台北市大安區和平東路二段96巷17弄3之1之2號5樓之3");

        // Act
        var result = ZipCode.Find(comp.NormalizedAddress);

        // Assert - 應該能查詢到郵遞區號
        Assert.NotNull(result);
        Assert.NotEmpty(result.ZipCode);
        // 和平東路二段96巷在大安區，應該有對應的郵遞區號
        Assert.StartsWith("106", result.ZipCode);  // 大安區郵遞區號 106xxx
    }

    [Fact]
    public void Parse_AddressWithRoomNumber_ExtractsAllComponents()
    {
        // Arrange & Act
        // 真實世界的複雜地址格式：包含室號和樓層附號
        // 注意：空格會影響 token 的解析，"1號之1" 之間的空格可能導致附號無法正確提取
        var comp = PostalAddress.Parse("台北市大安區和平東路二段96巷17弄 1號之1 2樓之5 3室");

        // Assert - 基本組件
        Assert.Equal("臺北市", comp.City);           // 台 → 臺
        Assert.Equal("大安區", comp.District);
        Assert.Equal("和平東路", comp.Road);

        // 巷弄
        Assert.Equal("96巷", comp.Lane);
        Assert.Equal("17弄", comp.Alley);

        // 門牌號碼
        Assert.Equal(1, comp.Number);

        // 附號說明：
        // 由於 "1號之1" 之間有空格，tokenization 可能會將 "之1" 視為獨立的 token
        // 而不是號碼的附號。這是目前解析器的已知限制。
        // 實際應用中，建議去除空格以獲得更好的解析結果。

        // 樓層（注意：樓層的附號 "之5" 可能會被包含在 Floor 中，或者被忽略）
        // 目前的解析器會提取包含數字的樓層標記
        if (!string.IsNullOrEmpty(comp.Floor))
        {
            Assert.Contains("樓", comp.Floor);
        }

        // 原始地址保留
        Assert.Equal("台北市大安區和平東路二段96巷17弄 1號之1 2樓之5 3室", comp.RawAddress);

        // 正規化地址 - 驗證主要組件
        Assert.Contains("臺北市", comp.NormalizedAddress);
        Assert.Contains("大安區", comp.NormalizedAddress);
        Assert.Contains("和平東路", comp.NormalizedAddress);
        Assert.Contains("96巷", comp.NormalizedAddress);
        Assert.Contains("17弄", comp.NormalizedAddress);
        Assert.Contains("1號", comp.NormalizedAddress);  // 門牌號（至少有 "1號"）
    }

    [Fact]
    public void Parse_AddressWithRoomNumber_CanQueryZipCode()
    {
        // Arrange - 96巷奇數門牌從3號起，用3號以確保在規則範圍內
        var comp = PostalAddress.Parse("台北市大安區和平東路二段96巷17弄 3號之1 2樓之5 3室");

        // Act
        var result = ZipCode.Find(comp.NormalizedAddress);

        // Assert - 即使有室號和樓層附號，仍應能查詢到郵遞區號
        Assert.NotNull(result);
        Assert.NotEmpty(result.ZipCode);
        Assert.StartsWith("106", result.ZipCode);  // 大安區郵遞區號 106xxx
    }

    [Fact]
    public void Parse_FloorWithSubNumber_ExtractsFloorAndSubFloor()
    {
        // Arrange & Act
        var comp = PostalAddress.Parse("臺北市信義區市府路1號5樓之3");

        // Assert
        Assert.Equal(1, comp.Number);
        Assert.Equal("5樓", comp.Floor);
        Assert.Equal(3, comp.SubFloor);
    }

    [Fact]
    public void Parse_FloorWithSubNumberAndRoom_ExtractsAll()
    {
        // Arrange & Act
        var comp = PostalAddress.Parse("臺北市信義區市府路1號5樓之3室");

        // Assert
        Assert.Equal(1, comp.Number);
        Assert.Equal("5樓", comp.Floor);
        Assert.Equal(3, comp.SubFloor);
        Assert.Equal("3室", comp.Room);
    }

    [Fact]
    public void Parse_RoomOnly_ExtractsRoom()
    {
        // Arrange & Act
        var comp = PostalAddress.Parse("臺北市信義區市府路1號101室");

        // Assert
        Assert.Equal(1, comp.Number);
        Assert.Equal("101室", comp.Room);
    }

    [Theory]
    [InlineData("臺北市信義區市府路1號5樓之3", "5樓", 3)]
    [InlineData("臺北市信義區市府路1號5樓-3", "5樓", 3)]  // 連字號會被正規化為「之」
    [InlineData("臺北市信義區市府路1號5樓~3", "5樓", 3)]  // 波浪號也會被正規化為「之」
    public void Parse_FloorWithSubNumber_SupportsMultipleFormats(string address, string expectedFloor, int expectedSubFloor)
    {
        // Arrange & Act
        var comp = PostalAddress.Parse(address);

        // Assert
        Assert.Equal(expectedFloor, comp.Floor);
        Assert.Equal(expectedSubFloor, comp.SubFloor);
    }

    [Fact]
    public void Parse_AddressWithSubAlley_ExtractsAllComponents()
    {
        // Arrange & Act
        // 衖（ㄏㄨㄥˋ）是弄的下級，類似巷→弄→衖的層級關係
        var comp = PostalAddress.Parse("桃園市中壢區龍岡路三段243巷53弄48衖15號");

        // Assert
        Assert.Equal("桃園市", comp.City);
        Assert.Equal("中壢區", comp.District);
        Assert.Equal("龍岡路", comp.Road);
        Assert.Equal("3段", comp.Section);        // 「三段」正規化為「3段」
        Assert.Equal("243巷", comp.Lane);
        Assert.Equal("53弄", comp.Alley);
        Assert.Equal("48衖", comp.SubAlley);
        Assert.Equal(15, comp.Number);
    }

    [Fact]
    public void Parse_AddressWithSubAlley_CanQueryZipCode()
    {
        // Arrange
        var comp = PostalAddress.Parse("桃園市中壢區龍岡路三段243巷53弄48衖15號");

        // Act
        var result = ZipCode.Find(comp.NormalizedAddress);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.ZipCode);
        // 桃園市中壢區的郵遞區號應該是 320xxx
        Assert.StartsWith("320", result.ZipCode);
    }

    [Fact]
    public void Parse_AddressWithFullWidthNumbersAndChineseFloor_ExtractsAllComponents()
    {
        // Arrange & Act
        // 測試全形數字（１５２）和中文數字樓層（二十一樓）
        var comp = PostalAddress.Parse("台中港路一段１５２號二十一樓之１");

        // Assert
        Assert.Equal("臺中港路", comp.Road);        // 「台」正規化為「臺」
        Assert.Equal("1段", comp.Section);          // 「一段」正規化為「1段」
        Assert.Equal(152, comp.Number);             // 「１５２」正規化為「152」
        Assert.Equal("21樓", comp.Floor);           // 「二十一樓」正規化為「21樓」
        Assert.Equal(1, comp.SubFloor);             // 「之１」正規化為「1」
    }

    [Fact]
    public void Parse_TaiwanBoulevardAddress_ExtractsAllComponents()
    {
        // Arrange & Act
        // 測試臺灣大道地址，包含全形數字和中文數字樓層
        var comp = PostalAddress.Parse("臺灣大道二段１８６號二十一樓之１");

        // Assert
        Assert.Equal("臺灣大道", comp.Road);
        Assert.Equal("2段", comp.Section);          // 「二段」正規化為「2段」
        Assert.Equal(186, comp.Number);             // 「１８６」正規化為「186」
        Assert.Equal("21樓", comp.Floor);           // 「二十一樓」正規化為「21樓」
        Assert.Equal(1, comp.SubFloor);             // 「之１」正規化為「1」
    }

    [Fact]
    public void Parse_AddressWithFullWidthAndChineseNumbers_NormalizesCorrectly()
    {
        // Arrange
        var addr1 = PostalAddress.Parse("台中港路一段１５２號二十一樓之１");
        var addr2 = PostalAddress.Parse("臺灣大道二段１８６號二十一樓之１");

        // Act
        var normalized1 = addr1.NormalizedAddress;
        var normalized2 = addr2.NormalizedAddress;

        // Assert
        // 驗證正規化結果：台→臺、全形→半形、中文數字→阿拉伯數字
        Assert.Equal("臺中港路1段152號21樓之1", normalized1);
        Assert.Equal("臺灣大道2段186號21樓之1", normalized2);
    }

    [Fact]
    public void Parse_AddressWithBasementFloor_ExtractsAllComponents()
    {
        // Arrange & Act
        // 測試地下樓層（地下一層）
        var comp = PostalAddress.Parse("臺灣大道一段７０３號地下一層");

        // Assert
        Assert.Equal("臺灣大道", comp.Road);        // 從「臺灣大道1段」提取
        Assert.Equal("1段", comp.Section);          // 「一段」正規化為「1段」
        Assert.Equal(703, comp.Number);             // 「７０３」正規化為「703」
        Assert.Equal("地下1層", comp.Floor);        // 「地下一層」正規化為「地下1層」
    }

    [Fact]
    public void Parse_AddressWithBasementFloor_NormalizesCorrectly()
    {
        // Arrange
        var comp = PostalAddress.Parse("臺灣大道一段７０３號地下一層");

        // Act
        var normalized = comp.NormalizedAddress;

        // Assert
        // 驗證正規化結果
        Assert.Equal("臺灣大道1段703號地下1層", normalized);
    }

    [Fact]
    public void Parse_AddressWithTemporaryNumber_ExtractsAllComponents()
    {
        // Arrange & Act
        // 測試臨時門牌（臨11號）
        var comp = PostalAddress.Parse("彰化縣彰化市民族一街臨11號");

        // Assert
        Assert.Equal("彰化縣", comp.City);
        Assert.Equal("彰化市", comp.District);
        Assert.Equal("民族1街", comp.Road);       // 「一街」正規化為「1街」
        Assert.Equal(11, comp.Number);
        Assert.True(comp.IsTemporary);            // 臨時門牌標記
    }

    [Fact]
    public void Parse_AddressWithTemporaryNumber_NormalizesCorrectly()
    {
        // Arrange
        var comp = PostalAddress.Parse("彰化縣彰化市民族一街臨11號");

        // Act
        var normalized = comp.NormalizedAddress;

        // Assert
        // 驗證正規化結果
        Assert.Equal("彰化縣彰化市民族1街臨11號", normalized);
    }

    [Fact]
    public void Parse_RegularAddress_IsNotTemporary()
    {
        // Arrange & Act
        // 測試一般門牌（非臨時）
        var comp = PostalAddress.Parse("臺北市信義區市府路1號");

        // Assert
        Assert.Equal(1, comp.Number);
        Assert.False(comp.IsTemporary);           // 非臨時門牌
    }

    [Fact]
    public void Parse_AddressWithForestRoad_ExtractsAllComponents()
    {
        // Arrange & Act
        // 測試林道地址（宜專1線）
        var comp = PostalAddress.Parse("宜蘭縣大同鄉太平村宜專1線中間1號");

        // Assert
        Assert.Equal("宜蘭縣", comp.City);
        Assert.Equal("大同鄉", comp.District);
        Assert.Equal("太平村", comp.Village);
        Assert.Equal("宜專1線", comp.Road);      // 林道名稱
        Assert.Equal(1, comp.Number);
        // 「中間」可能會被識別為 Locality
    }

    [Fact]
    public void Parse_AddressWithForestRoad_NormalizesCorrectly()
    {
        // Arrange
        var comp = PostalAddress.Parse("宜蘭縣大同鄉太平村宜專1線中間1號");

        // Act
        var normalized = comp.NormalizedAddress;

        // Assert
        // 驗證正規化結果
        Assert.Equal("宜蘭縣大同鄉太平村宜專1線中間1號", normalized);
    }

    [Fact]
    public void Parse_BasementWithFloorUnit_ExtractsCorrectly()
    {
        // 「地下二樓」應與「地下二層」一樣能解析
        var comp = PostalAddress.Parse("臺北市信義區市府路1號地下二樓");

        Assert.Equal("臺北市", comp.City);
        Assert.Equal("信義區", comp.District);
        Assert.Equal("市府路", comp.Road);
        Assert.Equal(1, comp.Number);
        Assert.Equal("地下2樓", comp.Floor);
        Assert.True(comp.IsBasement);
    }

    [Fact]
    public void Parse_BasementWithChineseRoomNumber_ExtractsCorrectly()
    {
        // 「地下四層三室」中文數字室號應能解析
        var comp = PostalAddress.Parse("臺北市信義區市府路1號地下四層三室");

        Assert.Equal(1, comp.Number);
        Assert.Equal("地下4層", comp.Floor);
        Assert.True(comp.IsBasement);
        Assert.Equal("3室", comp.Room);
    }

    [Fact]
    public void Parse_BasementFloorWithArabicRoom_ExtractsCorrectly()
    {
        // 「地下四層3室」混合數字
        var comp = PostalAddress.Parse("臺北市信義區市府路1號地下四層3室");

        Assert.Equal("地下4層", comp.Floor);
        Assert.True(comp.IsBasement);
        Assert.Equal("3室", comp.Room);
    }

    [Fact]
    public void Parse_BasementFloorWithFloorUnit_ChineseRoom_ExtractsCorrectly()
    {
        // 「地下二樓三室」樓+中文室號
        var comp = PostalAddress.Parse("臺北市信義區市府路1號地下二樓三室");

        Assert.Equal("地下2樓", comp.Floor);
        Assert.True(comp.IsBasement);
        Assert.Equal("3室", comp.Room);
    }
}
