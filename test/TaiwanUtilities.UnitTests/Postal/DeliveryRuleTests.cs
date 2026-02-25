namespace TaiwanUtilities.UnitTests;

using Xunit;

public class DeliveryRuleTests
{
    [Fact]
    public void Parse_AllRule_IdentifiesAll()
    {
        // Arrange & Act
        var rule = DeliveryRule.Parse("臺北市中正區三元街全");

        // Assert
        Assert.Equal(RuleType.All, rule.Type);
        Assert.Equal("全部門牌", rule.GetDescription());
    }

    [Fact]
    public void Parse_OddRule_IdentifiesOdd()
    {
        // Arrange & Act
        var rule = DeliveryRule.Parse("臺北市中正區三元街單147號以下");

        // Assert
        Assert.Equal(RuleType.Odd, rule.Type);
        Assert.Contains("單號", rule.GetDescription());
    }

    [Fact]
    public void Parse_EvenRule_IdentifiesEven()
    {
        // Arrange & Act
        var rule = DeliveryRule.Parse("臺北市大安區復興南路雙200號以上");

        // Assert
        Assert.Equal(RuleType.Even, rule.Type);
        Assert.Contains("雙號", rule.GetDescription());
    }

    [Fact]
    public void Parse_LessOrEqualRule_IdentifiesCorrectly()
    {
        // Arrange & Act
        var rule = DeliveryRule.Parse("臺北市中正區三元街單147號以下");

        // Assert
        // When both odd/even and range exist, type is Odd/Even
        Assert.Equal(RuleType.Odd, rule.Type);
        Assert.Equal(147, rule.EndNumber);
        Assert.Contains("147號以下", rule.GetDescription());
    }

    [Fact]
    public void Parse_GreaterOrEqualRule_IdentifiesCorrectly()
    {
        // Arrange & Act
        var rule = DeliveryRule.Parse("臺北市大安區復興南路雙200號以上");

        // Assert
        // When both odd/even and range exist, type is Odd/Even
        Assert.Equal(RuleType.Even, rule.Type);
        Assert.Equal(200, rule.StartNumber);
        Assert.Contains("200號以上", rule.GetDescription());
    }

    [Fact]
    public void Parse_RangeRule_IdentifiesRange()
    {
        // Arrange & Act
        var rule = DeliveryRule.Parse("臺北市信義區市府路1號至45號");

        // Assert
        Assert.Equal(RuleType.Range, rule.Type);
        Assert.Equal(1, rule.StartNumber);
        Assert.Equal(45, rule.EndNumber);
        Assert.Contains("1號至45號", rule.GetDescription());
    }

    [Fact]
    public void Parse_PureGreaterOrEqualRule_WithoutOddEven()
    {
        // Arrange & Act
        var rule = DeliveryRule.Parse("臺北市大安區復興南路200號以上");

        // Assert
        Assert.Equal(RuleType.GreaterOrEqual, rule.Type);
        Assert.Equal(200, rule.StartNumber);
        Assert.Contains("200號以上", rule.GetDescription());
    }

    [Fact]
    public void Parse_PureLessOrEqualRule_WithoutOddEven()
    {
        // Arrange & Act
        var rule = DeliveryRule.Parse("臺北市中正區三元街147號以下");

        // Assert
        Assert.Equal(RuleType.LessOrEqual, rule.Type);
        Assert.Equal(147, rule.EndNumber);
        Assert.Contains("147號以下", rule.GetDescription());
    }

    [Fact]
    public void Parse_SpecificNumberRule_IdentifiesCorrectly()
    {
        // Arrange & Act
        var rule = DeliveryRule.Parse("臺北市信義區市府路1號");

        // Assert
        Assert.Equal(RuleType.Specific, rule.Type);
        Assert.Equal(1, rule.SpecificNumber);
        Assert.Contains("1號", rule.GetDescription());
    }

    [Fact]
    public void Parse_SpecificNumberWithSubNumber_IdentifiesCorrectly()
    {
        // Arrange & Act
        var rule = DeliveryRule.Parse("臺北市信義區市府路1之2號");

        // Assert
        Assert.Equal(RuleType.Specific, rule.Type);
        Assert.Equal(1, rule.SpecificNumber);
        Assert.Equal(2, rule.SpecificSubNumber);
        Assert.Contains("1之2號", rule.GetDescription());
    }

    [Fact]
    public void Parse_WithSubNumberRule_IdentifiesCorrectly()
    {
        // Arrange & Act
        var rule = DeliveryRule.Parse("臺北市信義區市府路1號含附號");

        // Assert
        Assert.Equal(RuleType.WithSubNumber, rule.Type);
        Assert.Equal(1, rule.SpecificNumber);
        Assert.Contains("含附號", rule.GetDescription());
    }

    [Fact]
    public void Parse_SubNumberOnlyRule_IdentifiesCorrectly()
    {
        // Arrange & Act
        var rule = DeliveryRule.Parse("臺北市信義區市府路1號附號全");

        // Assert
        Assert.Equal(RuleType.SubNumberOnly, rule.Type);
        Assert.Equal(1, rule.SpecificNumber);
        Assert.Contains("僅附號", rule.GetDescription());
    }

    [Fact]
    public void Parse_SubNumberAboveRule_IdentifiesCorrectly()
    {
        // Arrange & Act
        var rule = DeliveryRule.Parse("臺北市信義區市府路1之2號及以上附號");

        // Assert
        Assert.Equal(RuleType.SubNumberAbove, rule.Type);
        Assert.Equal(1, rule.SpecificNumber);
        Assert.Contains("及以上附號", rule.GetDescription());
    }

    [Fact]
    public void Parse_SubNumberBelowRule_IdentifiesCorrectly()
    {
        // Arrange & Act
        var rule = DeliveryRule.Parse("臺北市信義區市府路1之2號含附號以下");

        // Assert
        Assert.Equal(RuleType.SubNumberBelow, rule.Type);
        Assert.Equal(1, rule.SpecificNumber);
        Assert.Contains("含附號以下", rule.GetDescription());
    }

    [Fact]
    public void Matches_OddNumber_MatchesCorrectly()
    {
        // Arrange
        var rule = DeliveryRule.Parse("臺北市中正區三元街單147號以下");
        var addr1 = PostalAddress.Parse("臺北市中正區三元街145號");
        var addr2 = PostalAddress.Parse("臺北市中正區三元街146號");

        // Act & Assert
        Assert.True(rule.Matches(addr1));   // 145 is odd and <= 147
        Assert.False(rule.Matches(addr2));  // 146 is even
    }

    [Fact]
    public void Matches_EvenNumber_MatchesCorrectly()
    {
        // Arrange
        var rule = DeliveryRule.Parse("臺北市大安區復興南路雙200號以上");
        var addr1 = PostalAddress.Parse("臺北市大安區復興南路202號");
        var addr2 = PostalAddress.Parse("臺北市大安區復興南路201號");

        // Act & Assert
        Assert.True(rule.Matches(addr1));   // 202 is even and >= 200
        Assert.False(rule.Matches(addr2));  // 201 is odd
    }

    [Fact]
    public void Matches_LessOrEqual_MatchesCorrectly()
    {
        // Arrange
        var rule = DeliveryRule.Parse("臺北市中正區三元街147號以下");
        var addr1 = PostalAddress.Parse("臺北市中正區三元街100號");
        var addr2 = PostalAddress.Parse("臺北市中正區三元街150號");

        // Act & Assert
        Assert.True(rule.Matches(addr1));   // 100 <= 147
        Assert.False(rule.Matches(addr2));  // 150 > 147
    }

    [Fact]
    public void Matches_GreaterOrEqual_MatchesCorrectly()
    {
        // Arrange
        var rule = DeliveryRule.Parse("臺北市大安區復興南路200號以上");
        var addr1 = PostalAddress.Parse("臺北市大安區復興南路250號");
        var addr2 = PostalAddress.Parse("臺北市大安區復興南路150號");

        // Act & Assert
        Assert.True(rule.Matches(addr1));   // 250 >= 200
        Assert.False(rule.Matches(addr2));  // 150 < 200
    }

    [Fact]
    public void Matches_Range_MatchesCorrectly()
    {
        // Arrange
        var rule = DeliveryRule.Parse("臺北市信義區市府路1號至45號");
        var addr1 = PostalAddress.Parse("臺北市信義區市府路25號");
        var addr2 = PostalAddress.Parse("臺北市信義區市府路50號");

        // Act & Assert
        Assert.True(rule.Matches(addr1));   // 25 is in range [1, 45]
        Assert.False(rule.Matches(addr2));  // 50 is not in range
    }

    [Fact]
    public void GetDescription_OddWithLessOrEqual_ReturnsCorrectDescription()
    {
        // Arrange
        var rule = DeliveryRule.Parse("臺北市中正區三元街單147號以下");

        // Act
        var description = rule.GetDescription();

        // Assert
        Assert.Contains("單號", description);
        Assert.Contains("147號以下", description);
    }

    [Fact]
    public void GetDescription_EvenWithGreaterOrEqual_ReturnsCorrectDescription()
    {
        // Arrange
        var rule = DeliveryRule.Parse("臺北市大安區復興南路雙200號以上");

        // Act
        var description = rule.GetDescription();

        // Assert
        Assert.Contains("雙號", description);
        Assert.Contains("200號以上", description);
    }

    [Fact]
    public void GetDescription_Range_ReturnsCorrectDescription()
    {
        // Arrange
        var rule = DeliveryRule.Parse("臺北市信義區市府路1號至45號");

        // Act
        var description = rule.GetDescription();

        // Assert
        Assert.Contains("1號至45號", description);
    }

    [Fact]
    public void Parse_EmptyRule_ReturnsEmptyRule()
    {
        // Arrange & Act
        var rule = DeliveryRule.Parse("");

        // Assert
        Assert.Equal(string.Empty, rule.RawRule);
    }

    [Fact]
    public void Parse_StoresRawRule()
    {
        // Arrange
        var ruleStr = "臺北市中正區三元街單147號以下";

        // Act
        var rule = DeliveryRule.Parse(ruleStr);

        // Assert
        Assert.Equal(ruleStr, rule.RawRule);
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var rule = DeliveryRule.Parse("臺北市中正區三元街單147號以下");

        // Act
        var result = rule.ToString();

        // Assert
        Assert.Contains("DeliveryRule", result);
    }
}
