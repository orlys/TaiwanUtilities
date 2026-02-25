namespace TaiwanUtilities.UnitTests;

using Xunit;

public class ZipCodeResultTests
{
    [Fact]
    public void NotFound_CreatesCorrectResult()
    {
        // Arrange
        var address = "臺北市不存在區某某路1號";

        // Act
        var result = ZipCodeResult.NotFound(address);

        // Assert
        Assert.Equal(ZipCodeResultType.NotFound, result.ResultType);
        Assert.Equal(address, result.OriginalAddress);
        Assert.Equal(string.Empty, result.ZipCode);
        Assert.False(result.IsValid);
        Assert.False(result.IsExactMatch);
        Assert.NotEmpty(result.Messages);
    }

    [Fact]
    public void PartialMatch_CreatesCorrectResult()
    {
        // Arrange
        var address = "臺北市信義區";
        var zipcode = "110";
        var scope = "臺北市信義區";

        // Act
        var result = ZipCodeResult.PartialMatch(address, zipcode, scope);

        // Assert
        Assert.Equal(ZipCodeResultType.PartialMatch, result.ResultType);
        Assert.Equal(zipcode, result.ZipCode);
        Assert.Equal(address, result.OriginalAddress);
        Assert.Equal(scope, result.MatchedScope);
        Assert.True(result.IsValid);
        Assert.False(result.IsExactMatch);
    }

    [Fact]
    public void ExactMatch_CreatesCorrectResult()
    {
        // Arrange
        var address = "臺北市信義區市府路1號";
        var zipcode = "110204";
        var ruleStr = "臺北市信義區市府路1號至45號";

        // Act
        var result = ZipCodeResult.ExactMatch(address, zipcode, ruleStr);

        // Assert
        Assert.Equal(ZipCodeResultType.ExactMatch, result.ResultType);
        Assert.Equal(zipcode, result.ZipCode);
        Assert.Equal(address, result.OriginalAddress);
        Assert.True(result.IsValid);
        Assert.True(result.IsExactMatch);
        Assert.NotNull(result.MatchedRule);
    }

    [Fact]
    public void ZipCode3_Returns3DigitZipCode()
    {
        // Arrange
        var result = ZipCodeResult.ExactMatch("臺北市信義區市府路1號", "110204", "test");

        // Act
        var zipcode3 = result.ZipCode3;

        // Assert
        Assert.Equal("110", zipcode3);
    }

    [Fact]
    public void ZipCode5_Returns5DigitZipCodeWhenAvailable()
    {
        // Arrange
        var result = ZipCodeResult.ExactMatch("臺北市信義區市府路1號", "110204", "test");

        // Act
        var zipcode5 = result.ZipCode5;

        // Assert
        Assert.Equal("110204", zipcode5);
    }

    [Fact]
    public void ZipCode5_ReturnsNullWhen3DigitOnly()
    {
        // Arrange
        var result = ZipCodeResult.PartialMatch("臺北市信義區", "110", "臺北市信義區");

        // Act
        var zipcode5 = result.ZipCode5;

        // Assert
        Assert.Null(zipcode5);
    }

    [Fact]
    public void Components_AreParsedCorrectly()
    {
        // Arrange & Act
        var result = ZipCodeResult.ExactMatch("臺北市信義區市府路1號", "110204", "test");

        // Assert
        Assert.NotNull(result.Address);
        Assert.Equal("臺北市", result.Address.City);
        Assert.Equal("信義區", result.Address.District);
        Assert.Equal("市府路", result.Address.Road);
        Assert.Equal(1, result.Address.Number);
    }

    [Fact]
    public void NormalizedAddress_IsStored()
    {
        // Arrange
        var address = "台北市 信義區 市府路１號";

        // Act
        var result = ZipCodeResult.ExactMatch(address, "110204", "test");

        // Assert
        Assert.Equal("臺北市信義區市府路1號", result.NormalizedAddress);
    }

    [Fact]
    public void IsValid_ReturnsTrueWhenZipCodeExists()
    {
        // Arrange
        var result = ZipCodeResult.ExactMatch("臺北市信義區市府路1號", "110204", "test");

        // Act & Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void IsValid_ReturnsFalseWhenNoZipCode()
    {
        // Arrange
        var result = ZipCodeResult.NotFound("臺北市不存在區某某路1號");

        // Act & Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void IsExactMatch_ReturnsTrueForExactMatch()
    {
        // Arrange
        var result = ZipCodeResult.ExactMatch("臺北市信義區市府路1號", "110204", "test");

        // Act & Assert
        Assert.True(result.IsExactMatch);
    }

    [Fact]
    public void IsExactMatch_ReturnsFalseForPartialMatch()
    {
        // Arrange
        var result = ZipCodeResult.PartialMatch("臺北市信義區", "110", "scope");

        // Act & Assert
        Assert.False(result.IsExactMatch);
    }

    [Fact]
    public void Messages_CanBeModified()
    {
        // Arrange
        var result = ZipCodeResult.NotFound("test");

        // Act
        result.Messages.Add("Custom message");

        // Assert
        Assert.Contains("Custom message", result.Messages);
    }

    [Fact]
    public void Suggestions_CanBeAdded()
    {
        // Arrange
        var result = ZipCodeResult.NotFound("test");

        // Act
        result.Suggestions.Add("Suggestion 1");
        result.Suggestions.Add("Suggestion 2");

        // Assert
        Assert.Equal(2, result.Suggestions.Count);
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var result = ZipCodeResult.ExactMatch("臺北市信義區市府路1號", "110204", "test");

        // Act
        var str = result.ToString();

        // Assert
        Assert.Contains("ZipCodeResult", str);
        Assert.Contains("110204", str);
    }
}
