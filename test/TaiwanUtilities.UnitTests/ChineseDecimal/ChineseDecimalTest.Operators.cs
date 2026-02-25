namespace TaiwanUtilities.UnitTests;

using Xunit;

/// <summary>
/// ChineseNumeric 的 Equality、Comparison、ImplicitOperators 測試
/// </summary>
public partial class ChineseNumericTest
{
    #region Equality

    [Fact]
    public static void Equals_相同值()
    {
        ChineseNumeric a = 42m;
        ChineseNumeric b = 42m;

        Assert.True(a.Equals(b));
        Assert.True(a.Equals((object)b));
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public static void Equals_不同值()
    {
        ChineseNumeric a = 42m;
        ChineseNumeric b = 99m;

        Assert.False(a.Equals(b));
        Assert.False(a.Equals((object)b));
    }

    [Fact]
    public static void Equals_預設值()
    {
        var a = default(ChineseNumeric);
        var b = default(ChineseNumeric);

        Assert.True(a.Equals(b));
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public static void Equals_非ChineseNumeric物件()
    {
        ChineseNumeric a = 42m;
        Assert.False(a.Equals("42"));
        Assert.False(a.Equals(null));
    }

    #endregion

    #region Comparison

    [Fact]
    public static void CompareTo_大於()
    {
        ChineseNumeric a = 100m;
        ChineseNumeric b = 50m;

        Assert.True(a.CompareTo(b) > 0);
        Assert.True(b.CompareTo(a) < 0);
    }

    [Fact]
    public static void CompareTo_相等()
    {
        ChineseNumeric a = 77m;
        ChineseNumeric b = 77m;

        Assert.Equal(0, a.CompareTo(b));
    }

    [Fact]
    public static void CompareTo_Object()
    {
        ChineseNumeric a = 10m;
        ChineseNumeric b = 20m;

        Assert.True(a.CompareTo((object)b) < 0);
    }

    [Fact]
    public static void CompareTo_非法型別()
    {
        ChineseNumeric a = 10m;
        Assert.Throws<System.ArgumentException>(() => a.CompareTo("10"));
    }

    #endregion

    #region Implicit Operators

    [Fact]
    public static void 隱含轉換_decimal到ChineseNumeric()
    {
        ChineseNumeric cn = 123.45m;
        Assert.Equal(123.45m, (decimal)cn);
    }

    [Fact]
    public static void 隱含轉換_ChineseNumeric到decimal()
    {
        ChineseNumeric cn = 999m;
        decimal d = cn;
        Assert.Equal(999m, d);
    }

    [Fact]
    public static void 隱含轉換_零()
    {
        ChineseNumeric cn = 0m;
        Assert.Equal(0m, (decimal)cn);
    }

    [Fact]
    public static void 隱含轉換_負數_應拋出例外()
    {
        Assert.Throws<System.ArgumentOutOfRangeException>(() =>
        {
            ChineseNumeric cn = -42m;
        });
    }

    [Fact]
    public static void 隱含轉換_來回一致()
    {
        decimal original = 12345.678m;
        ChineseNumeric cn = original;
        decimal roundTrip = cn;
        Assert.Equal(original, roundTrip);
    }

    #endregion
}
