namespace TaiwanUtilities.UnitTests;

using Xunit;

/// <summary>
/// 中文地址 → 官方格式英文地址轉換測試
/// 涵蓋 PostalAddress.ToEnglish() 與 ZipCode.ToEnglishAddress() 兩個公開 API。
/// </summary>
[Collection("DatabaseSingleton")]
public class EnglishAddressTests
{
    // =========================================================================
    // §1 正向組裝 — smoke cases（人工確認正確）
    // =========================================================================

    [Theory]
    [InlineData(
        "臺北市中正區忠孝東路一段1號5樓",
        "5F., No. 1, Sec. 1, Zhongxiao E. Rd., Zhongzheng Dist., Taipei City")]
    [InlineData(
        "臺北市大安區忠孝東路三段217巷3弄1之2號5樓之3",
        "5F.-3, No. 1-2, Aly. 3, Ln. 217, Sec. 3, Zhongxiao E. Rd., Da'an Dist., Taipei City")]
    [InlineData(
        "新北市淡水區中正路1號",
        "No. 1, Zhongzheng Rd., Tamsui Dist., New Taipei City")]
    [InlineData(
        "基隆市仁愛區愛一路1號",
        "No. 1, Ai 1st Rd., Ren'ai Dist., Keelung City")]
    [InlineData(
        "高雄市苓雅區四維3路6號",
        "No. 6, Siwei 3rd Rd., Lingya Dist., Kaohsiung City")]
    [InlineData(
        "臺中市西屯區臺灣大道三段99號",
        "No. 99, Sec. 3, Taiwan Blvd., Xitun Dist., Taichung City")]
    [InlineData(
        "金門縣金城鎮民生路1號",
        "No. 1, Minsheng Rd., Jincheng Township, Kinmen County")]
    public void ToEnglish_SmokeCase_ReturnsOfficialFormat(string chinese, string expected)
    {
        // Arrange
        var addr = PostalAddress.Parse(chinese);

        // Act
        var result = addr.ToEnglish();

        // Assert
        Assert.Equal(expected, result);
    }

    // =========================================================================
    // §2 機械格式化規則（§5）
    // =========================================================================

    // --- 號 ---

    [Fact]
    public void FormatNumber_SimpleNumber_ReturnsNoPrefix()
    {
        // 1號 → "No. 1"（含於完整地址輸出）
        var addr = PostalAddress.Parse("臺北市中正區忠孝東路一段1號");
        var result = addr.ToEnglish();

        Assert.NotNull(result);
        Assert.Contains("No. 1", result);
    }

    [Fact]
    public void FormatNumber_WithSubNumber_ReturnsDashSeparated()
    {
        // 1之2號 → "No. 1-2"
        var addr = PostalAddress.Parse("臺北市大安區忠孝東路三段217巷3弄1之2號");
        var result = addr.ToEnglish();

        Assert.NotNull(result);
        Assert.Contains("No. 1-2", result);
    }

    [Fact]
    public void FormatNumber_LargerNumber_FormatsCorrectly()
    {
        // 99號 → "No. 99"
        var addr = PostalAddress.Parse("臺中市西屯區臺灣大道三段99號");
        var result = addr.ToEnglish();

        Assert.NotNull(result);
        Assert.Contains("No. 99", result);
    }

    // --- 樓 ---

    [Fact]
    public void FormatFloor_SimpleFloor_ReturnsFSuffix()
    {
        // 5樓 → "5F."
        var addr = PostalAddress.Parse("臺北市中正區忠孝東路一段1號5樓");
        var result = addr.ToEnglish();

        Assert.NotNull(result);
        Assert.Contains("5F.", result);
    }

    [Fact]
    public void FormatFloor_WithSubFloor_ReturnsDashSuffix()
    {
        // 5樓之3 → "5F.-3"
        var addr = PostalAddress.Parse("臺北市大安區忠孝東路三段217巷3弄1之2號5樓之3");
        var result = addr.ToEnglish();

        Assert.NotNull(result);
        Assert.Contains("5F.-3", result);
    }

    [Fact]
    public void FormatFloor_NoFloor_NotPresentInOutput()
    {
        // 無樓層時輸出不含 "F."
        var addr = PostalAddress.Parse("新北市淡水區中正路1號");
        var result = addr.ToEnglish();

        Assert.NotNull(result);
        Assert.DoesNotContain("F.", result);
    }

    // --- 巷 (Lane) ---

    [Fact]
    public void FormatLane_WithLane_ReturnsLnPrefix()
    {
        // 217巷 → "Ln. 217"
        var addr = PostalAddress.Parse("臺北市大安區忠孝東路三段217巷3弄1之2號5樓之3");
        var result = addr.ToEnglish();

        Assert.NotNull(result);
        Assert.Contains("Ln. 217", result);
    }

    [Fact]
    public void FormatLane_NoLane_NotPresentInOutput()
    {
        // 無巷時輸出不含 "Ln."
        var addr = PostalAddress.Parse("臺北市中正區忠孝東路一段1號5樓");
        var result = addr.ToEnglish();

        Assert.NotNull(result);
        Assert.DoesNotContain("Ln.", result);
    }

    // --- 弄 (Alley) ---

    [Fact]
    public void FormatAlley_WithAlley_ReturnsAlyPrefix()
    {
        // 3弄 → "Aly. 3"
        var addr = PostalAddress.Parse("臺北市大安區忠孝東路三段217巷3弄1之2號5樓之3");
        var result = addr.ToEnglish();

        Assert.NotNull(result);
        Assert.Contains("Aly. 3", result);
    }

    [Fact]
    public void FormatAlley_NoAlley_NotPresentInOutput()
    {
        // 無弄時輸出不含 "Aly."
        var addr = PostalAddress.Parse("新北市淡水區中正路1號");
        var result = addr.ToEnglish();

        Assert.NotNull(result);
        Assert.DoesNotContain("Aly.", result);
    }

    // --- 段（已含於 EROAD，不另處理）---

    [Fact]
    public void Section_IsEmbeddedInERoad_NotDuplicated()
    {
        // 一段 → EROAD 已含 "Sec. 1"，不應出現兩次
        var addr = PostalAddress.Parse("臺北市中正區忠孝東路一段1號5樓");
        var result = addr.ToEnglish();

        Assert.NotNull(result);
        Assert.Contains("Sec. 1", result);
        // 確認 "Sec. 1" 只出現一次
        Assert.Equal(1, CountOccurrences(result, "Sec. 1"));
    }

    [Fact]
    public void Section_ThreeSec_IsEmbeddedInERoad()
    {
        // 三段 → EROAD 已含 "Sec. 3"
        var addr = PostalAddress.Parse("臺中市西屯區臺灣大道三段99號");
        var result = addr.ToEnglish();

        Assert.NotNull(result);
        Assert.Contains("Sec. 3", result);
    }

    // =========================================================================
    // §3 歷史拼法正確性（官方 EROAD，非漢語拼音演算法）
    // =========================================================================

    [Fact]
    public void HistoricalSpelling_Tamsui_NotDanshui()
    {
        // 淡水 → Tamsui（官方歷史拼法），不得出現 Danshui
        var result = ZipCode.ToEnglishAddress("新北市淡水區中正路1號");

        Assert.NotNull(result);
        Assert.Contains("Tamsui", result);
        Assert.DoesNotContain("Danshui", result);
    }

    [Fact]
    public void HistoricalSpelling_Keelung_NotJilong()
    {
        // 基隆 → Keelung（官方歷史拼法），不得出現 Jilong
        var result = ZipCode.ToEnglishAddress("基隆市仁愛區愛一路1號");

        Assert.NotNull(result);
        Assert.Contains("Keelung", result);
        Assert.DoesNotContain("Jilong", result);
    }

    [Fact]
    public void HistoricalSpelling_Kinmen_NotJinmen()
    {
        // 金門 → Kinmen（官方歷史拼法），不得出現 Jinmen
        var result = ZipCode.ToEnglishAddress("金門縣金城鎮民生路1號");

        Assert.NotNull(result);
        Assert.Contains("Kinmen", result);
        Assert.DoesNotContain("Jinmen", result);
    }

    // =========================================================================
    // §4 反序組裝驗證（由小到大：F. → No. → Aly. → Ln. → Road+Sec. → Dist. → City）
    // =========================================================================

    [Fact]
    public void ComponentOrder_FullAddress_IsSmallToLarge()
    {
        // 臺北市大安區忠孝東路三段217巷3弄1之2號5樓之3
        // 期望順序：5F.-3, No. 1-2, Aly. 3, Ln. 217, Sec. 3 ..., Da'an Dist., Taipei City
        var result = ZipCode.ToEnglishAddress("臺北市大安區忠孝東路三段217巷3弄1之2號5樓之3");

        Assert.NotNull(result);
        var parts = result!.Split(", ");

        // 找各組件的位置
        int floorIdx    = IndexOfContaining(parts, "F.");
        int noIdx       = IndexOfContaining(parts, "No.");
        int alyIdx      = IndexOfContaining(parts, "Aly.");
        int lnIdx       = IndexOfContaining(parts, "Ln.");
        int roadIdx     = IndexOfContaining(parts, "Rd.");
        int distIdx     = IndexOfContaining(parts, "Dist.");
        int cityIdx     = IndexOfContaining(parts, "City");

        // 確認由小到大排列
        Assert.True(floorIdx  < noIdx,    $"Floor({floorIdx}) 應在 No.({noIdx}) 之前");
        Assert.True(noIdx     < alyIdx,   $"No.({noIdx}) 應在 Aly.({alyIdx}) 之前");
        Assert.True(alyIdx    < lnIdx,    $"Aly.({alyIdx}) 應在 Ln.({lnIdx}) 之前");
        Assert.True(lnIdx     < roadIdx,  $"Ln.({lnIdx}) 應在 Road({roadIdx}) 之前");
        Assert.True(roadIdx   < distIdx,  $"Road({roadIdx}) 應在 Dist.({distIdx}) 之前");
        Assert.True(distIdx   < cityIdx,  $"Dist.({distIdx}) 應在 City({cityIdx}) 之前");
    }

    [Fact]
    public void ComponentOrder_WithoutLaneAlley_RoadBeforeDistBeforeCity()
    {
        // 無巷弄時：No. → Road → Dist. → City
        var result = ZipCode.ToEnglishAddress("臺北市中正區忠孝東路一段1號5樓");

        Assert.NotNull(result);
        var parts = result!.Split(", ");

        int noIdx    = IndexOfContaining(parts, "No.");
        int roadIdx  = IndexOfContaining(parts, "Rd.");
        int distIdx  = IndexOfContaining(parts, "Dist.");
        int cityIdx  = IndexOfContaining(parts, "City");

        Assert.True(noIdx   < roadIdx,  $"No.({noIdx}) 應在 Road({roadIdx}) 之前");
        Assert.True(roadIdx < distIdx,  $"Road({roadIdx}) 應在 Dist.({distIdx}) 之前");
        Assert.True(distIdx < cityIdx,  $"Dist.({distIdx}) 應在 City({cityIdx}) 之前");
    }

    [Fact]
    public void ComponentOrder_CommaDelimited_EachPartSeparated()
    {
        // 輸出使用 ", " 分隔，不得有多餘空格或缺少逗號
        var result = ZipCode.ToEnglishAddress("臺北市中正區忠孝東路一段1號5樓");

        Assert.NotNull(result);
        // 不以逗號開頭或結尾
        Assert.False(result!.StartsWith(","), "輸出不應以逗號開頭");
        Assert.False(result.EndsWith(","), "輸出不應以逗號結尾");
        // 分隔符為 ", "（逗號後接一個空格）
        Assert.DoesNotContain(",,", result);
    }

    // =========================================================================
    // §5 邊界回 null（§6）
    // =========================================================================

    [Theory]
    [InlineData("這不是地址")]
    [InlineData("火星市不存在區某路1號")]
    [InlineData("Hello World")]
    [InlineData("1234567890")]
    public void ToEnglishAddress_NonAddress_ReturnsNull(string input)
    {
        var result = ZipCode.ToEnglishAddress(input);
        Assert.Null(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ToEnglishAddress_NullOrWhitespace_ReturnsNull(string? input)
    {
        var result = ZipCode.ToEnglishAddress(input!);
        Assert.Null(result);
    }

    [Fact]
    public void ToEnglish_ParsedEmptyAddress_ReturnsNull()
    {
        // Parse 空字串 → City/District/Road 皆 null → ToEnglish 回 null
        var addr = PostalAddress.Parse("");
        var result = addr.ToEnglish();
        Assert.Null(result);
    }

    [Fact]
    public void ToEnglish_CityOnlyNoRoad_ReturnsNull()
    {
        // 無路名時 ToEnglish 應回 null（缺必要組件）
        var addr = PostalAddress.Parse("臺北市中正區");
        var result = addr.ToEnglish();
        Assert.Null(result);
    }

    [Fact]
    public void ToEnglishAddress_DoesNotThrow_OnAnyInput()
    {
        // ZipCode.ToEnglishAddress 無論輸入為何，都不應拋例外
        var inputs = new[]
        {
            null!, "", "   ", "這不是地址",
            "臺北市中正區忠孝東路一段1號5樓",
            "火星市宇宙區銀河路99號99樓"
        };

        foreach (var input in inputs)
        {
            var ex = Record.Exception(() => ZipCode.ToEnglishAddress(input));
            Assert.Null(ex);
        }
    }

    // =========================================================================
    // §6 兩個 API 一致性
    // =========================================================================

    [Theory]
    [InlineData("臺北市中正區忠孝東路一段1號5樓")]
    [InlineData("臺北市大安區忠孝東路三段217巷3弄1之2號5樓之3")]
    [InlineData("新北市淡水區中正路1號")]
    [InlineData("基隆市仁愛區愛一路1號")]
    [InlineData("高雄市苓雅區四維3路6號")]
    [InlineData("臺中市西屯區臺灣大道三段99號")]
    [InlineData("金門縣金城鎮民生路1號")]
    [InlineData("這不是地址")]
    [InlineData("")]
    public void BothApis_SameInput_ReturnSameResult(string address)
    {
        // PostalAddress.Parse(s).ToEnglish() 與 ZipCode.ToEnglishAddress(s) 結果一致
        var viaParseAndToEnglish = PostalAddress.Parse(address).ToEnglish();
        var viaStaticHelper      = ZipCode.ToEnglishAddress(address);

        Assert.Equal(viaParseAndToEnglish, viaStaticHelper);
    }

    // =========================================================================
    // 補充：阿拉伯數字路名正規化（§6 fallback）
    // =========================================================================

    [Fact]
    public void ToEnglish_ArabicNumeralRoad_NormalizedAndResolved()
    {
        // 四維3路 → ArabicToChineseInRoad → 四維三路 → 查 EROAD
        // 高雄市苓雅區四維3路6號 已在 smoke cases 驗證；此測試明確驗證路名正規化路徑
        var result1 = ZipCode.ToEnglishAddress("高雄市苓雅區四維3路6號");    // 阿拉伯數字
        var result2 = ZipCode.ToEnglishAddress("高雄市苓雅區四維三路6號");   // 中文數字

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(result1, result2);
    }

    // =========================================================================
    // 輔助方法
    // =========================================================================

    private static int CountOccurrences(string source, string value)
    {
        int count = 0;
        int idx = 0;
        while ((idx = source.IndexOf(value, idx, StringComparison.Ordinal)) >= 0)
        {
            count++;
            idx += value.Length;
        }
        return count;
    }

    /// <summary>找 parts 陣列中第一個包含 <paramref name="substring"/> 的元素索引；-1 = 未找到。</summary>
    private static int IndexOfContaining(string[] parts, string substring)
    {
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].Contains(substring, StringComparison.Ordinal))
                return i;
        }
        return -1;
    }
}
