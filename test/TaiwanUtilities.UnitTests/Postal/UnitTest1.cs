using Xunit;

namespace TaiwanUtilities.UnitTests;


public class AddressTests
{
    [Fact]
    public void TestAddressTokenization()
    {
        var addr = new AddressTokenizer("臺北市大安區市府路1號");
        var expected = new[]
        {
            new[] { "", "", "臺北", "市" },
            new[] { "", "", "大安", "區" },
            new[] { "", "", "市府", "路" },
            new[] { "1", "", "", "號" }
        };

        Assert.Equal(4, addr.Tokens.Count);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], addr.Tokens[i]);
        }
    }

    [Fact]
    public void TestAddressWithSubNo()
    {
        var addr = new AddressTokenizer("臺北市大安區市府路1之1號");
        Assert.Equal(4, addr.Tokens.Count);
        Assert.Equal("1", addr.Tokens[3][AddressTokenizer.NO]);
        Assert.Equal("之1", addr.Tokens[3][AddressTokenizer.SUBNO]);
    }

    [Fact]
    public void TestNormalization()
    {
        // 台 -> 臺
        Assert.Equal("臺北市", AddressTokenizer.Normalize("台北市"));

        // 全形數字 -> 半形數字
        Assert.Equal("1", AddressTokenizer.Normalize("１"));
        Assert.Equal("市府路1號", AddressTokenizer.Normalize("市府路１號"));

        // 中文數字 -> 阿拉伯數字
        Assert.Equal("3號", AddressTokenizer.Normalize("三號"));
        Assert.Equal("18號", AddressTokenizer.Normalize("十八號"));
        Assert.Equal("38號", AddressTokenizer.Normalize("三十八號"));

        // 但路名中的數字不轉換
        Assert.Equal("八德路", AddressTokenizer.Normalize("八德路"));
        Assert.Equal("三元街", AddressTokenizer.Normalize("三元街"));

        // 段路街的數字要轉換
        Assert.Equal("信義路1段", AddressTokenizer.Normalize("信義路一段"));
        Assert.Equal("敬業1路", AddressTokenizer.Normalize("敬業一路"));
    }

    [Fact]
    public void TestFlat()
    {
        var addr = new AddressTokenizer("臺北市大安區市府路1之1號");
        Assert.Equal("臺北市", addr.Flat(1));
        Assert.Equal("臺北市大安區", addr.Flat(2));
        Assert.Equal("臺北市大安區市府路", addr.Flat(3));
        Assert.Equal("臺北市大安區市府路1之1號", addr.Flat());
    }

    [Fact]
    public void TestParse()
    {
        var addr = new AddressTokenizer("臺北市大安區市府路5號");
        var result1 = addr.Parse(3);
        Assert.Equal(5, result1.No);
        Assert.Empty(result1.SubNos);

        var addr2 = new AddressTokenizer("臺北市大安區市府路5之3號");
        var result2 = addr2.Parse(3);
        Assert.Equal(5, result2.No);
        Assert.Single(result2.SubNos);
        Assert.Equal(3, result2.SubNos[0]);
    }

    [Fact]
    public void TestFloorWithSubNumberTokenization()
    {
        // 測試「5樓之3室」的 tokenization
        var addr = new AddressTokenizer("臺北市信義區市府路1號5樓之3室");

        // 驗證基本組件
        Assert.Equal(new[] { "", "", "臺北", "市" }, addr.Tokens[0]);
        Assert.Equal(new[] { "", "", "信義", "區" }, addr.Tokens[1]);
        Assert.Equal(new[] { "", "", "市府", "路" }, addr.Tokens[2]);
        Assert.Equal(new[] { "1", "", "", "號" }, addr.Tokens[3]);

        // 驗證「5樓之3室」的解析
        // 應該被解析為：["5", "", "", "樓"] + ["", "", "之", ""] + ["3", "", "", "室"]
        Assert.Equal(new[] { "5", "", "", "樓" }, addr.Tokens[4]);
        Assert.Equal(new[] { "", "", "之", "" }, addr.Tokens[5]);
        Assert.Equal(new[] { "3", "", "", "室" }, addr.Tokens[6]);
    }

    [Fact]
    public void TestFloorWithSubNumberNoRoomTokenization()
    {
        // 測試「5樓之3」（沒有「室」字）的 tokenization
        var addr = new AddressTokenizer("臺北市信義區市府路1號5樓之3");

        // 驗證基本組件
        Assert.Equal(new[] { "", "", "臺北", "市" }, addr.Tokens[0]);
        Assert.Equal(new[] { "", "", "信義", "區" }, addr.Tokens[1]);
        Assert.Equal(new[] { "", "", "市府", "路" }, addr.Tokens[2]);
        Assert.Equal(new[] { "1", "", "", "號" }, addr.Tokens[3]);

        // 驗證「5樓之3」的解析
        // 應該被解析為：["5", "", "", "樓"] + ["", "", "之3", ""]
        Assert.Equal(new[] { "5", "", "", "樓" }, addr.Tokens[4]);
        Assert.Equal(new[] { "", "", "之3", "" }, addr.Tokens[5]);
    }

    [Fact]
    public void TestSubAlleyTokenization()
    {
        // 測試「桃園市中壢區龍岡路三段243巷53弄48衖15號」的 tokenization
        var addr = new AddressTokenizer("桃園市中壢區龍岡路三段243巷53弄48衖15號");

        // 驗證基本組件
        Assert.Equal(8, addr.Tokens.Count);
        Assert.Equal(new[] { "", "", "桃園", "市" }, addr.Tokens[0]);
        Assert.Equal(new[] { "", "", "中壢", "區" }, addr.Tokens[1]);
        Assert.Equal(new[] { "", "", "龍岡", "路" }, addr.Tokens[2]);
        Assert.Equal(new[] { "", "", "3", "段" }, addr.Tokens[3]);      // 「三段」正規化為「3段」，解析為 NAME=3
        Assert.Equal(new[] { "243", "", "", "巷" }, addr.Tokens[4]);
        Assert.Equal(new[] { "53", "", "", "弄" }, addr.Tokens[5]);
        Assert.Equal(new[] { "48", "", "", "衖" }, addr.Tokens[6]);     // 衖（弄的下級）
        Assert.Equal(new[] { "15", "", "", "號" }, addr.Tokens[7]);
    }

    [Fact]
    public void TestFullWidthAndChineseNumberTokenization()
    {
        // 測試兩個地址的 tokenization
        var addr1 = new AddressTokenizer("台中港路一段１５２號二十一樓之１");
        var addr2 = new AddressTokenizer("臺灣大道二段１８６號二十一樓之１");

        // 驗證地址 1: 台中港路一段１５２號二十一樓之１
        // 正規化為: 臺中港路1段152號21樓之1
        Assert.Equal(5, addr1.Tokens.Count);
        Assert.Equal(new[] { "", "", "臺中港", "路" }, addr1.Tokens[0]);
        Assert.Equal(new[] { "", "", "1", "段" }, addr1.Tokens[1]);
        Assert.Equal(new[] { "152", "", "", "號" }, addr1.Tokens[2]);
        Assert.Equal(new[] { "21", "", "", "樓" }, addr1.Tokens[3]);
        Assert.Equal(new[] { "", "", "之1", "" }, addr1.Tokens[4]);

        // 驗證地址 2: 臺灣大道二段１８６號二十一樓之１
        // 正規化為: 臺灣大道2段186號21樓之1
        // 特殊情況：「臺灣大道2段」被解析為單一 token (NAME=臺灣大道2, UNIT=段)
        Assert.Equal(4, addr2.Tokens.Count);
        Assert.Equal(new[] { "", "", "臺灣大道2", "段" }, addr2.Tokens[0]);
        Assert.Equal(new[] { "186", "", "", "號" }, addr2.Tokens[1]);
        Assert.Equal(new[] { "21", "", "", "樓" }, addr2.Tokens[2]);
        Assert.Equal(new[] { "", "", "之1", "" }, addr2.Tokens[3]);
    }

    [Fact]
    public void TestBasementFloorTokenization()
    {
        // 測試「臺灣大道一段７０３號地下一層」的 tokenization
        var addr = new AddressTokenizer("臺灣大道一段７０３號地下一層");

        // 驗證正規化和 tokenization
        // 正規化: 臺灣大道1段703號地下1層
        Assert.Equal(4, addr.Tokens.Count);
        Assert.Equal(new[] { "", "", "臺灣大道1", "段" }, addr.Tokens[0]);
        Assert.Equal(new[] { "703", "", "", "號" }, addr.Tokens[1]);
        Assert.Equal(new[] { "", "", "地下", "" }, addr.Tokens[2]);      // 「地下」前綴
        Assert.Equal(new[] { "1", "", "", "層" }, addr.Tokens[3]);       // 「1層」
    }

    [Fact]
    public void TestTemporaryNumberTokenization()
    {
        // 測試「彰化縣彰化市民族一街臨11號」的 tokenization
        var addr = new AddressTokenizer("彰化縣彰化市民族一街臨11號");

        // 驗證正規化和 tokenization
        // 正規化: 彰化縣彰化市民族1街臨11號
        Assert.Equal(5, addr.Tokens.Count);
        Assert.Equal(new[] { "", "", "彰化", "縣" }, addr.Tokens[0]);
        Assert.Equal(new[] { "", "", "彰化", "市" }, addr.Tokens[1]);
        Assert.Equal(new[] { "", "", "民族1", "街" }, addr.Tokens[2]);
        Assert.Equal(new[] { "", "", "臨", "" }, addr.Tokens[3]);      // 「臨」前綴（臨時門牌）
        Assert.Equal(new[] { "11", "", "", "號" }, addr.Tokens[4]);    // 門牌號碼
    }

    [Fact]
    public void TestForestRoadTokenization()
    {
        // 測試「宜蘭縣大同鄉太平村宜專1線中間1號」的 tokenization
        var addr = new AddressTokenizer("宜蘭縣大同鄉太平村宜專1線中間1號");

        // 驗證正規化和 tokenization
        // 正規化: 宜蘭縣大同鄉太平村宜專1線中間1號
        Assert.Equal(7, addr.Tokens.Count);
        Assert.Equal(new[] { "", "", "宜蘭", "縣" }, addr.Tokens[0]);
        Assert.Equal(new[] { "", "", "大同", "鄉" }, addr.Tokens[1]);
        Assert.Equal(new[] { "", "", "太平", "村" }, addr.Tokens[2]);
        Assert.Equal(new[] { "", "", "宜專", "" }, addr.Tokens[3]);      // 林道名稱前綴
        Assert.Equal(new[] { "1", "", "", "線" }, addr.Tokens[4]);       // 線號（林道編號）
        Assert.Equal(new[] { "", "", "中間", "" }, addr.Tokens[5]);      // 特殊地名
        Assert.Equal(new[] { "1", "", "", "號" }, addr.Tokens[6]);       // 門牌號碼
    }
}

public class RuleTests
{
    [Fact]
    public void TestRuleTokenExtraction()
    {
        var rule = new DeliveryRuleMatcher("臺北市,中正區,八德路１段,全");
        Assert.Contains("全", rule.RuleTokens);

        var rule2 = new DeliveryRuleMatcher("臺北市,中正區,三元街,單全");
        Assert.Contains("單", rule2.RuleTokens);
        Assert.Contains("全", rule2.RuleTokens);

        var rule3 = new DeliveryRuleMatcher("臺北市,中正區,三元街,雙  48號以下");
        Assert.Contains("雙", rule3.RuleTokens);
        Assert.Contains("以下", rule3.RuleTokens);
    }

    [Fact]
    public void TestRuleMatch_All()
    {
        var rule = new DeliveryRuleMatcher("臺北市,中正區,八德路１段,全");
        Assert.True(rule.Match(new AddressTokenizer("臺北市中正區八德路１段1號")));
        Assert.True(rule.Match(new AddressTokenizer("臺北市中正區八德路１段9號")));
        Assert.False(rule.Match(new AddressTokenizer("臺北市中正區八德路２段1號")));
    }

    [Fact]
    public void TestRuleMatch_OddEven()
    {
        var addr5 = new AddressTokenizer("臺北市大安區市府路5號");

        Assert.True(new DeliveryRuleMatcher("臺北市大安區市府路全").Match(addr5));
        Assert.True(new DeliveryRuleMatcher("臺北市大安區市府路單全").Match(addr5));
        Assert.False(new DeliveryRuleMatcher("臺北市大安區市府路雙全").Match(addr5));
    }

    [Fact]
    public void TestRuleMatch_AboveBelow()
    {
        var addr5 = new AddressTokenizer("臺北市大安區市府路5號");

        Assert.False(new DeliveryRuleMatcher("臺北市大安區市府路6號以上").Match(addr5));
        Assert.True(new DeliveryRuleMatcher("臺北市大安區市府路6號以下").Match(addr5));
        Assert.True(new DeliveryRuleMatcher("臺北市大安區市府路5號以上").Match(addr5));
        Assert.True(new DeliveryRuleMatcher("臺北市大安區市府路5號以下").Match(addr5));
        Assert.True(new DeliveryRuleMatcher("臺北市大安區市府路4號以上").Match(addr5));
        Assert.False(new DeliveryRuleMatcher("臺北市大安區市府路4號以下").Match(addr5));
    }

    [Fact]
    public void TestRuleMatch_Range()
    {
        var addr5 = new AddressTokenizer("臺北市大安區市府路5號");

        Assert.False(new DeliveryRuleMatcher("臺北市大安區市府路1號至4號").Match(addr5));
        Assert.True(new DeliveryRuleMatcher("臺北市大安區市府路1號至5號").Match(addr5));
        Assert.True(new DeliveryRuleMatcher("臺北市大安區市府路5號至9號").Match(addr5));
        Assert.False(new DeliveryRuleMatcher("臺北市大安區市府路6號至9號").Match(addr5));
    }
}
