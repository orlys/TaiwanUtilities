namespace TaiwanUtilities.UnitTests;

using TaiwanUtilities;

using Xunit;

/// <summary>
/// 地址驗證功能測試
/// </summary>
[Collection("DatabaseSingleton")]
public class ValidationTests
{

    [Fact]
    public void TestValidateAddress_ValidAddress()
    {
                var result = ZipCode.ValidateAddress("臺北市信義區市府路1號");

        Assert.True(result.IsValid);
        Assert.NotEmpty(result.ZipCode);
        Assert.Equal(PostalValidationFailureReason.None, result.FailureReason);
        Assert.Equal("臺北市信義區市府路1號", result.NormalizedAddress);
    }

    [Fact]
    public void TestValidateAddress_ValidAddressWithNormalization()
    {
                // 使用簡體字和全形數字
        var result = ZipCode.ValidateAddress("台北市信義區市府路１號");

        Assert.True(result.IsValid);
        Assert.NotEmpty(result.ZipCode);
        Assert.Equal("臺北市信義區市府路1號", result.NormalizedAddress);
    }

    [Fact]
    public void TestValidateAddress_EmptyAddress()
    {
                var result = ZipCode.ValidateAddress("");

        Assert.False(result.IsValid);
        Assert.Equal(PostalValidationFailureReason.InvalidFormat, result.FailureReason);
        Assert.Contains("不能為空", result.Messages[0]);
    }

    [Fact]
    public void TestValidateAddress_NullAddress()
    {
        var result = ZipCode.ValidateAddress((string)null!);

        Assert.False(result.IsValid);
        Assert.Equal(PostalValidationFailureReason.InvalidFormat, result.FailureReason);
    }

    [Fact]
    public void TestValidateAddress_WhitespaceAddress()
    {
                var result = ZipCode.ValidateAddress("   ");

        Assert.False(result.IsValid);
        Assert.Equal(PostalValidationFailureReason.InvalidFormat, result.FailureReason);
    }

    [Fact]
    public void TestValidateAddress_PartialAddressNoNumber()
    {
                var result = ZipCode.ValidateAddress("臺北市信義區市府路");

        Assert.False(result.IsValid);
        Assert.Equal(PostalValidationFailureReason.NumberRuleMismatch, result.FailureReason);
    }

    [Fact]
    public void TestValidateAddress_NonExistentStreet()
    {
                var result = ZipCode.ValidateAddress("臺北市信義區不存在路123號");

        Assert.False(result.IsValid);
        // 可能是 NumberOutOfRange 或 AddressNotFound
        Assert.True(
            result.FailureReason == PostalValidationFailureReason.NumberOutOfRange ||
            result.FailureReason == PostalValidationFailureReason.AddressNotFound
        );
    }

    [Fact]
    public void TestValidateAddress_ValidAddressWithSubNumber()
    {
                var result = ZipCode.ValidateAddress("臺北市信義區市府路1之1號");

        Assert.True(result.IsValid);
        Assert.NotEmpty(result.ZipCode);
        Assert.Contains("之", result.NormalizedAddress);
    }

    [Fact]
    public void TestValidateAddress_DifferentCity()
    {
                var result = ZipCode.ValidateAddress("高雄市左營區博愛二路777號");

        Assert.True(result.IsValid);
        Assert.NotEmpty(result.ZipCode);
        Assert.StartsWith("8", result.ZipCode); // 高雄市郵遞區號以 8 開頭
    }

    [Fact]
    public void TestValidateAddress_ReturnsNormalizedAddress()
    {
                var result = ZipCode.ValidateAddress("台北市，信義區，市府路　１　號");

        Assert.NotEmpty(result.NormalizedAddress);
        Assert.DoesNotContain("，", result.NormalizedAddress);
        Assert.DoesNotContain("　", result.NormalizedAddress);
    }

    [Fact]
    public void TestValidateAddress_ReturnsMessages()
    {
                var result1 = ZipCode.ValidateAddress("臺北市信義區市府路1號");
        Assert.NotEmpty(result1.Messages);

        var result2 = ZipCode.ValidateAddress("");
        Assert.NotEmpty(result2.Messages);
    }

    [Theory]
    [InlineData("臺北市中正區仁愛路1段1號")]
    [InlineData("臺北市大安區敦化南路2段105號")]
    [InlineData("新北市板橋區中山路1段1號")]
    public void TestValidateAddress_MultipleValidAddresses(string address)
    {
                var result = ZipCode.ValidateAddress(address);

        Assert.True(result.IsValid, $"地址 {address} 應該有效");
        Assert.NotEmpty(result.ZipCode);
    }

    [Fact]
    public void TestValidateAddress_InvalidFormatReturnsCorrectReason()
    {
                var result = ZipCode.ValidateAddress("");

        Assert.Equal(PostalValidationFailureReason.InvalidFormat, result.FailureReason);
        Assert.Empty(result.ZipCode);
    }
}
