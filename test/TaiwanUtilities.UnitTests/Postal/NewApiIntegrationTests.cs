namespace TaiwanUtilities.UnitTests;

using Xunit;

using System.Linq;

/// <summary>
/// 新 API 整合測試
/// </summary>
[Collection("DatabaseSingleton")]
public class NewApiIntegrationTests
{
    [Fact]
    public void FindDetailed_ValidAddress_ReturnsDetailedResult()
    {
        // Arrange
        
        // Act
        var result = ZipCode.Find("臺北市中正區杭州南路1段1號");

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.ZipCode);
        Assert.NotNull(result.Address);
        Assert.Equal("臺北市", result.Address.City);
    }

    [Fact]
    public void FindDetailed_InvalidAddress_ReturnsNotFoundResult()
    {
        // Arrange
        
        // Act
        var result = ZipCode.Find("火星市不存在區某某路1號");

        // Assert
        Assert.Equal(ZipCodeResultType.NotFound, result.ResultType);
        Assert.Empty(result.ZipCode);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void FindDetailed_PartialAddress_ReturnsPartialMatch()
    {
        // Arrange
        
        // Act
        var result = ZipCode.Find("臺北市中正區");

        // Assert
        Assert.NotEmpty(result.ZipCode);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void FindDetailed_ConsistentWithOldFind()
    {
        // Arrange
                var address = "臺北市中正區";

        // Act
        var oldResult = ZipCode.Find(address).ZipCode;
        var newResult = ZipCode.Find(address);

        // Assert
        Assert.Equal(oldResult, newResult.ZipCode);
    }

    [Fact]
    public void ParseAddress_ValidAddress_ExtractsComponents()
    {
        // Arrange & Act
        var comp = PostalAddress.Parse("臺北市信義區市府路1之2號3樓");

        // Assert
        Assert.Equal("臺北市", comp.City);
        Assert.Equal("信義區", comp.District);
        Assert.Equal("市府路", comp.Road);
        Assert.Equal(1, comp.Number);
        Assert.Equal(2, comp.SubNumbers?.FirstOrDefault());
        Assert.Equal("3樓", comp.Floor);
    }

    [Fact]
    public void ParseAddress_StaticMethod_WorksWithoutDatabaseConnection()
    {
        // Arrange & Act
        var comp = PostalAddress.Parse("台北市信義區市府路1號");

        // Assert - This should work even without database
        Assert.NotNull(comp);
        Assert.Equal("臺北市", comp.City);
    }

    [Fact]
    public void GetDeliveryRules_ValidAddress_ReturnsRules()
    {
        // Arrange
        
        // Act
        var rules = ZipCode.GetDeliveryRules("臺北市中正區杭州南路1段");

        // Assert
        Assert.NotNull(rules);
        // Rules may or may not exist depending on database content
    }

    [Fact]
    public void GetDeliveryRules_ReturnsZipCodeAndRule()
    {
        // Arrange
        
        // Act
        var rules = ZipCode.GetDeliveryRules("臺北市中正區杭州南路1段");

        // Assert
        if (rules.Count > 0)
        {
            var item = rules[0];
            Assert.NotEmpty(item.ZipCode);
            Assert.NotNull(item.Rule);
            Assert.NotEmpty(item.Rule.RawRule);
        }
    }

    [Fact]
    public void GetSuggestionsDetailed_ValidPartial_ReturnsSuggestions()
    {
        // Arrange
        
        // Act
        var suggestions = ZipCode.GetSuggestions("臺北市中正區中", 5);

        // Assert
        Assert.NotNull(suggestions);
        Assert.True(suggestions.Count <= 5);
    }

    [Fact]
    public void GetSuggestionsDetailed_IncludesZipCodeAndComponents()
    {
        // Arrange
        
        // Act
        var suggestions = ZipCode.GetSuggestions("臺北市中", 5);

        // Assert
        if (suggestions.Count > 0)
        {
            Assert.All(suggestions, s =>
            {
                Assert.NotEmpty(s.AddressText);
                Assert.NotEmpty(s.ZipCode);
                Assert.NotNull(s.Address);
            });
        }
    }

    [Fact]
    public void FindDetailed_EmptyAddress_ReturnsNotFound()
    {
        // Arrange
        
        // Act
        var result = ZipCode.Find("");

        // Assert
        Assert.Equal(ZipCodeResultType.NotFound, result.ResultType);
    }

    [Fact]
    public void FindDetailed_NullAddress_ReturnsNotFound()
    {
        // Arrange

        // Act
        var result = ZipCode.Find((string)null!);

        // Assert
        Assert.Equal(ZipCodeResultType.NotFound, result.ResultType);
    }

    [Fact]
    public void ParseAddress_EmptyAddress_ReturnsEmptyComponents()
    {
        // Arrange & Act
        var comp = PostalAddress.Parse("");

        // Assert
        Assert.Null(comp.City);
        Assert.Null(comp.District);
    }

    [Fact]
    public void GetSuggestionsDetailed_EmptyInput_ReturnsEmpty()
    {
        // Arrange
        
        // Act
        var suggestions = ZipCode.GetSuggestions("", 5);

        // Assert
        Assert.Empty(suggestions);
    }

    [Fact]
    public void ZipCodeResult_HasCorrectProperties()
    {
        // Arrange
        
        // Act
        var result = ZipCode.Find("臺北市中正區");

        // Assert
        Assert.NotEmpty(result.OriginalAddress);
        Assert.NotEmpty(result.NormalizedAddress);
        Assert.NotNull(result.Address);
        Assert.NotEmpty(result.ZipCode3);
    }

    [Fact]
    public void PostalAddressSuggestion_HasAllRequiredProperties()
    {
        // Arrange
        
        // Act
        var suggestions = ZipCode.GetSuggestions("臺北市", 1);

        // Assert
        if (suggestions.Count > 0)
        {
            var s = suggestions[0];
            Assert.NotNull(s.Address);
            Assert.NotNull(s.ZipCode);
            Assert.NotNull(s.Address);
        }
    }

    [Fact]
    public void BackwardCompatibility_OldAPIStillWorks()
    {
        // Arrange
        
        // Act & Assert - All old methods should still work
        var zipcode = ZipCode.Find("臺北市中正區").ZipCode;
        Assert.NotEmpty(zipcode);

        var normalized = AddressUtils.Normalize("台北市信義區");
        Assert.NotEmpty(normalized);

        var validation = ZipCode.ValidateAddress("臺北市中正區杭州南路1段1號");
        Assert.NotNull(validation);

        var suggestions = ZipCode.GetSuggestions("臺北市中", 5);
        Assert.NotNull(suggestions);
    }
}
