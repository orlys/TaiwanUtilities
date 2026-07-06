namespace TaiwanUtilities.UnitTests;

using Xunit;

/// <summary>
/// 測試 ZipCodeUtils.GetCommonPart() 方法
/// 確保郵遞區號格式符合台灣標準（3碼、5碼或6碼）
/// </summary>
public class GetCommonPartTests
{
    [Theory]
    [InlineData("100055", "100056", "10005")]  // 前5碼相同 → 5碼
    [InlineData("100066", "100066", "100066")] // 完全相同 → 6碼
    [InlineData("100012", "100019", "10001")]  // 前5碼相同 → 5碼
    [InlineData("100089", "100088", "10008")]  // 前5碼相同 → 5碼
    public void GetCommonPart_With5OrMoreCommonDigits_Returns5Or6Digits(
        string zipA, string zipB, string expected)
    {
        var result = ZipCodeUtils.GetCommonPart(zipA, zipB);
        Assert.Equal(expected, result);
        Assert.True(result!.Length == 5 || result.Length == 6);
    }

    [Theory]
    [InlineData("100066", "100255", "100")]    // 前3碼相同 → 3碼
    [InlineData("100012", "100934", "100")]    // 前3碼相同 → 3碼
    [InlineData("110204", "110555", "110")]    // 前3碼相同 → 3碼
    public void GetCommonPart_With3To4CommonDigits_Returns3Digits(
        string zipA, string zipB, string expected)
    {
        var result = ZipCodeUtils.GetCommonPart(zipA, zipB);
        Assert.Equal(expected, result);
        Assert.Equal(3, result!.Length);
    }

    [Theory]
    [InlineData("100066", "110204", "1")]      // 只有1碼相同 → 保留1碼（支持漸進式查詢）
    [InlineData("100066", "120034", "1")]      // 只有1碼相同 → 保留1碼
    [InlineData("100066", "105034", "10")]     // 只有2碼相同 → 保留2碼
    [InlineData("123456", "987654", "")]       // 0碼相同 → 空字串
    public void GetCommonPart_WithLessThan3CommonDigits_ReturnsPrefix(
        string zipA, string zipB, string expected)
    {
        var result = ZipCodeUtils.GetCommonPart(zipA, zipB);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetCommonPart_WithNullA_ReturnsB()
    {
        var result = ZipCodeUtils.GetCommonPart(null, "100066");
        Assert.Equal("100066", result);
    }

    [Fact]
    public void GetCommonPart_WithNullB_ReturnsA()
    {
        var result = ZipCodeUtils.GetCommonPart("100066", null);
        Assert.Equal("100066", result);
    }

    [Fact]
    public void GetCommonPart_WithBothNull_ReturnsNull()
    {
        var result = ZipCodeUtils.GetCommonPart(null, null);
        Assert.Null(result);
    }

    [Theory]
    [InlineData("100", "10001", "100")]        // 3碼 vs 5碼，前3碼相同 → 3碼
    [InlineData("100", "100066", "100")]       // 3碼 vs 6碼，前3碼相同 → 3碼
    [InlineData("10001", "100019", "10001")]   // 5碼 vs 6碼，前5碼相同 → 5碼
    public void GetCommonPart_WithDifferentLengths_ReturnsCorrectFormat(
        string zipA, string zipB, string expected)
    {
        var result = ZipCodeUtils.GetCommonPart(zipA, zipB);
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// 驗證結果永遠不會是 4 碼（不符合台灣郵遞區號標準）
    /// 1碼、2碼用於漸進式查詢（如 "1" 代表臺北）
    /// </summary>
    [Theory]
    [InlineData("100066", "100055")]  // 前4碼相同 → 應截取到3碼
    [InlineData("100066", "100255")]  // 前3碼相同 → 3碼
    [InlineData("100066", "110204")]  // 前1碼相同 → 1碼（用於漸進式查詢）
    [InlineData("100066", "200034")]  // 前1碼相同 → 1碼
    [InlineData("123456", "987654")]  // 0碼相同 → 空字串
    public void GetCommonPart_NeverReturns4Digits(string zipA, string zipB)
    {
        var result = ZipCodeUtils.GetCommonPart(zipA, zipB);

        // 結果長度不能是 4 碼（不標準）
        Assert.NotEqual(4, result!.Length);

        // 允許的長度：0, 1, 2, 3, 5, 6（其中1和2用於漸進式查詢）
        Assert.True(
            result.Length == 0 || result.Length == 1 || result.Length == 2 ||
            result.Length == 3 || result.Length == 5 || result.Length == 6,
            $"不合法的郵遞區號長度: {result.Length} (郵遞區號: {result})");
    }
}
