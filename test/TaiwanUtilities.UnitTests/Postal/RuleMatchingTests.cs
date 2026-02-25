namespace TaiwanUtilities.UnitTests;

using TaiwanUtilities;

using Xunit;

/// <summary>
/// 規則匹配進階測試
/// </summary>
public class RuleMatchingTests
{
    [Theory]
    [InlineData("臺北市大安區市府路1號", "臺北市大安區市府路全", true)]
    [InlineData("臺北市大安區市府路99號", "臺北市大安區市府路全", true)]
    [InlineData("臺北市中正區市府路1號", "臺北市大安區市府路全", false)]
    public void TestAllRule(string address, string rule, bool shouldMatch)
    {
        var addr = new AddressTokenizer(address);
        var ruleObj = new DeliveryRuleMatcher(rule);
        Assert.Equal(shouldMatch, ruleObj.Match(addr));
    }

    [Theory]
    [InlineData("臺北市大安區市府路1號", "臺北市大安區市府路單全", true)]
    [InlineData("臺北市大安區市府路3號", "臺北市大安區市府路單全", true)]
    [InlineData("臺北市大安區市府路2號", "臺北市大安區市府路單全", false)]
    [InlineData("臺北市大安區市府路4號", "臺北市大安區市府路單全", false)]
    public void TestOddRule(string address, string rule, bool shouldMatch)
    {
        var addr = new AddressTokenizer(address);
        var ruleObj = new DeliveryRuleMatcher(rule);
        Assert.Equal(shouldMatch, ruleObj.Match(addr));
    }

    [Theory]
    [InlineData("臺北市大安區市府路2號", "臺北市大安區市府路雙全", true)]
    [InlineData("臺北市大安區市府路4號", "臺北市大安區市府路雙全", true)]
    [InlineData("臺北市大安區市府路1號", "臺北市大安區市府路雙全", false)]
    [InlineData("臺北市大安區市府路3號", "臺北市大安區市府路雙全", false)]
    public void TestEvenRule(string address, string rule, bool shouldMatch)
    {
        var addr = new AddressTokenizer(address);
        var ruleObj = new DeliveryRuleMatcher(rule);
        Assert.Equal(shouldMatch, ruleObj.Match(addr));
    }

    [Theory]
    [InlineData("臺北市大安區市府路10號", "臺北市大安區市府路10號以上", true)]
    [InlineData("臺北市大安區市府路15號", "臺北市大安區市府路10號以上", true)]
    [InlineData("臺北市大安區市府路9號", "臺北市大安區市府路10號以上", false)]
    public void TestAboveRule(string address, string rule, bool shouldMatch)
    {
        var addr = new AddressTokenizer(address);
        var ruleObj = new DeliveryRuleMatcher(rule);
        Assert.Equal(shouldMatch, ruleObj.Match(addr));
    }

    [Theory]
    [InlineData("臺北市大安區市府路10號", "臺北市大安區市府路10號以下", true)]
    [InlineData("臺北市大安區市府路5號", "臺北市大安區市府路10號以下", true)]
    [InlineData("臺北市大安區市府路11號", "臺北市大安區市府路10號以下", false)]
    public void TestBelowRule(string address, string rule, bool shouldMatch)
    {
        var addr = new AddressTokenizer(address);
        var ruleObj = new DeliveryRuleMatcher(rule);
        Assert.Equal(shouldMatch, ruleObj.Match(addr));
    }

    [Theory]
    [InlineData("臺北市大安區市府路5號", "臺北市大安區市府路1號至10號", true)]
    [InlineData("臺北市大安區市府路1號", "臺北市大安區市府路1號至10號", true)]
    [InlineData("臺北市大安區市府路10號", "臺北市大安區市府路1號至10號", true)]
    [InlineData("臺北市大安區市府路11號", "臺北市大安區市府路1號至10號", false)]
    [InlineData("臺北市大安區市府路0號", "臺北市大安區市府路1號至10號", false)]
    public void TestRangeRule(string address, string rule, bool shouldMatch)
    {
        var addr = new AddressTokenizer(address);
        var ruleObj = new DeliveryRuleMatcher(rule);
        Assert.Equal(shouldMatch, ruleObj.Match(addr));
    }

    [Theory]
    [InlineData("臺北市大安區市府路5號", "臺北市大安區市府路5號含附號", true)]
    [InlineData("臺北市大安區市府路5之1號", "臺北市大安區市府路5號含附號", true)]
    [InlineData("臺北市大安區市府路5之99號", "臺北市大安區市府路5號含附號", true)]
    [InlineData("臺北市大安區市府路4號", "臺北市大安區市府路5號含附號", false)]
    [InlineData("臺北市大安區市府路6號", "臺北市大安區市府路5號含附號", false)]
    public void TestContainSubNumberRule(string address, string rule, bool shouldMatch)
    {
        var addr = new AddressTokenizer(address);
        var ruleObj = new DeliveryRuleMatcher(rule);
        Assert.Equal(shouldMatch, ruleObj.Match(addr));
    }

    [Theory]
    [InlineData("臺北市大安區市府路5之1號", "臺北市大安區市府路5附號全", true)]
    [InlineData("臺北市大安區市府路5之99號", "臺北市大安區市府路5附號全", true)]
    [InlineData("臺北市大安區市府路5號", "臺北市大安區市府路5附號全", false)]
    [InlineData("臺北市大安區市府路4之1號", "臺北市大安區市府路5附號全", false)]
    public void TestSubNumberOnlyRule(string address, string rule, bool shouldMatch)
    {
        var addr = new AddressTokenizer(address);
        var ruleObj = new DeliveryRuleMatcher(rule);
        Assert.Equal(shouldMatch, ruleObj.Match(addr));
    }

    [Theory]
    [InlineData("臺北市大安區市府路5之3號", "臺北市大安區市府路5之3號及以上附號", true)]
    [InlineData("臺北市大安區市府路5之5號", "臺北市大安區市府路5之3號及以上附號", true)]
    [InlineData("臺北市大安區市府路6號", "臺北市大安區市府路5之3號及以上附號", true)]
    [InlineData("臺北市大安區市府路5之2號", "臺北市大安區市府路5之3號及以上附號", false)]
    [InlineData("臺北市大安區市府路4號", "臺北市大安區市府路5之3號及以上附號", false)]
    public void TestAboveSubNumberRule(string address, string rule, bool shouldMatch)
    {
        var addr = new AddressTokenizer(address);
        var ruleObj = new DeliveryRuleMatcher(rule);
        Assert.Equal(shouldMatch, ruleObj.Match(addr));
    }

    [Theory]
    [InlineData("臺北市大安區市府路單5號至15號", "單,至")]
    [InlineData("臺北市大安區市府路雙10號以下", "雙,以下")]
    [InlineData("臺北市大安區市府路5號含附號全", "含附號全")]
    [InlineData("臺北市大安區市府路連2之4號以上", "以上")]
    public void TestRuleTokenExtraction(string ruleStr, string expectedTokens)
    {
        var rule = new DeliveryRuleMatcher(ruleStr);
        var expected = expectedTokens.Split(',');

        Assert.Equal(expected.Length, rule.RuleTokens.Count);
        foreach (var token in expected)
        {
            Assert.Contains(token, rule.RuleTokens);
        }
    }

    [Fact]
    public void TestComplexRule_OddWithRange()
    {
        var rule = new DeliveryRuleMatcher("臺北市大安區市府路單1號至99號");

        Assert.True(rule.Match(new AddressTokenizer("臺北市大安區市府路1號")));
        Assert.True(rule.Match(new AddressTokenizer("臺北市大安區市府路99號")));
        Assert.True(rule.Match(new AddressTokenizer("臺北市大安區市府路51號")));
        Assert.False(rule.Match(new AddressTokenizer("臺北市大安區市府路2號")));   // 偶數
        Assert.False(rule.Match(new AddressTokenizer("臺北市大安區市府路101號"))); // 超出範圍
    }

    [Fact]
    public void TestComplexRule_EvenWithBelow()
    {
        var rule = new DeliveryRuleMatcher("臺北市大安區市府路雙50號以下");

        Assert.True(rule.Match(new AddressTokenizer("臺北市大安區市府路2號")));
        Assert.True(rule.Match(new AddressTokenizer("臺北市大安區市府路50號")));
        Assert.False(rule.Match(new AddressTokenizer("臺北市大安區市府路1號")));  // 單數
        Assert.False(rule.Match(new AddressTokenizer("臺北市大安區市府路52號"))); // 超過 50
    }
}
