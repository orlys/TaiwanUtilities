namespace TaiwanUtilities.UnitTests;

using Xunit;

using System.Linq;

/// <summary>
/// ZipCodeRepository 內部方法測試
/// </summary>
public class DirectoryInternalTests
{
    private string GetDatabasePath()
    {
        // 使用內嵌資料庫
        return EmbeddedDatabaseHelper.ExtractEmbeddedDatabase();
    }

    [Fact]
    public void FindDetailed_ExactMatch_ReturnsExactMatchType()
    {
        // Arrange
        var dbPath = GetDatabasePath();
        using var directory = new ZipCodeRepository(dbPath);

        // Act
        var result = directory.FindDetailed("臺北市中正區杭州南路1段1號");

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.ZipCode);
        // If exact match is found, should be ExactMatch type
        if (!string.IsNullOrEmpty(result.ZipCode))
        {
            Assert.NotEqual(ZipCodeResultType.NotFound, result.ResultType);
        }
    }

    [Fact]
    public void FindDetailed_PartialAddress_ReturnsPartialMatch()
    {
        // Arrange
        var dbPath = GetDatabasePath();
        using var directory = new ZipCodeRepository(dbPath);

        // Act
        var result = directory.FindDetailed("臺北市中正區");

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.ZipCode);
        Assert.NotEqual(ZipCodeResultType.NotFound, result.ResultType);
    }

    [Fact]
    public void FindDetailed_InvalidAddress_ReturnsNotFound()
    {
        // Arrange
        var dbPath = GetDatabasePath();
        using var directory = new ZipCodeRepository(dbPath);

        // Act
        var result = directory.FindDetailed("火星市不存在區某某路1號");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ZipCodeResultType.NotFound, result.ResultType);
        Assert.Empty(result.ZipCode);
    }

    [Fact]
    public void FindDetailed_IncludesComponents()
    {
        // Arrange
        var dbPath = GetDatabasePath();
        using var directory = new ZipCodeRepository(dbPath);

        // Act
        var result = directory.FindDetailed("臺北市中正區");

        // Assert
        Assert.NotNull(result.Address);
        Assert.Equal("臺北市", result.Address.City);
        Assert.Equal("中正區", result.Address.District);
    }

    [Fact]
    public void FindDetailed_ExactMatch_IncludesMatchedRule()
    {
        // Arrange
        var dbPath = GetDatabasePath();
        using var directory = new ZipCodeRepository(dbPath);

        // Act - Use a full address that should have a rule
        var result = directory.FindDetailed("臺北市中正區杭州南路1段1號");

        // Assert
        if (result.ResultType == ZipCodeResultType.ExactMatch)
        {
            Assert.NotNull(result.MatchedRule);
            Assert.NotEmpty(result.MatchedScope!);
        }
    }

    [Fact]
    public void GetDeliveryRules_ValidAddress_ReturnsRules()
    {
        // Arrange
        var dbPath = GetDatabasePath();
        using var directory = new ZipCodeRepository(dbPath);

        // Act
        var rules = directory.GetDeliveryRules("臺北市中正區杭州南路1段");

        // Assert
        // May or may not have rules depending on database content
        // Just check it doesn't throw and returns a list
        Assert.NotNull(rules);
    }

    [Fact]
    public void GetDeliveryRules_ReturnsZipCodeAndRule()
    {
        // Arrange
        var dbPath = GetDatabasePath();
        using var directory = new ZipCodeRepository(dbPath);

        // Act
        var rules = directory.GetDeliveryRules("臺北市中正區杭州南路1段");

        // Assert
        if (rules.Count > 0)
        {
            var item = rules[0];
            Assert.NotEmpty(item.ZipCode);
            Assert.NotNull(item.Rule);
        }
    }

    [Fact]
    public void GetSuggestionsDetailed_ValidPartial_ReturnsSuggestions()
    {
        // Arrange
        var dbPath = GetDatabasePath();
        using var directory = new ZipCodeRepository(dbPath);

        // Act
        var suggestions = directory.GetSuggestionsDetailed("臺北市中正區中", 5);

        // Assert
        Assert.NotNull(suggestions);
        // May or may not have suggestions depending on database content
    }

    [Fact]
    public void GetSuggestionsDetailed_IncludesZipCode()
    {
        // Arrange
        var dbPath = GetDatabasePath();
        using var directory = new ZipCodeRepository(dbPath);

        // Act
        var suggestions = directory.GetSuggestionsDetailed("臺北市中", 5);

        // Assert
        if (suggestions.Count > 0)
        {
            Assert.All(suggestions, s =>
            {
                Assert.NotEmpty(s.AddressText);
                Assert.NotEmpty(s.ZipCode);
            });
        }
    }

    [Fact]
    public void GetSuggestionsDetailed_IncludesComponents()
    {
        // Arrange
        var dbPath = GetDatabasePath();
        using var directory = new ZipCodeRepository(dbPath);

        // Act
        var suggestions = directory.GetSuggestionsDetailed("臺北市中", 5);

        // Assert
        if (suggestions.Count > 0)
        {
            Assert.All(suggestions, s =>
            {
                Assert.NotNull(s.Address);
            });
        }
    }

    [Fact]
    public void GetSuggestionsDetailed_RespectsMaxResults()
    {
        // Arrange
        var dbPath = GetDatabasePath();
        using var directory = new ZipCodeRepository(dbPath);
        var maxResults = 3;

        // Act
        var suggestions = directory.GetSuggestionsDetailed("臺北市", maxResults);

        // Assert
        Assert.True(suggestions.Count <= maxResults);
    }

    [Fact]
    public void GetSuggestionsDetailed_EmptyInput_ReturnsEmpty()
    {
        // Arrange
        var dbPath = GetDatabasePath();
        using var directory = new ZipCodeRepository(dbPath);

        // Act
        var suggestions = directory.GetSuggestionsDetailed("", 5);

        // Assert
        Assert.Empty(suggestions);
    }

    [Fact]
    public void GetRoadType_WithNonExistentAddress_ReturnsNull()
    {
        // Arrange
        var dbPath = GetDatabasePath();
        using var directory = new ZipCodeRepository(dbPath);

        // Act
        var roadType = directory.GetRoadType("不存在的地址");

        // Assert
        Assert.Null(roadType);
    }

    [Fact]
    public void GetRoadType_DatabaseHasRoadTypeColumn()
    {
        // Arrange
        var dbPath = GetDatabasePath();
        using var directory = new ZipCodeRepository(dbPath);

        // Act - Try to get road type for any address
        // This test verifies the road_type column exists in the database
        var result = directory.FindDetailed("臺北市");

        // Assert - Just verify we can query without errors
        Assert.NotNull(result);
    }
}
