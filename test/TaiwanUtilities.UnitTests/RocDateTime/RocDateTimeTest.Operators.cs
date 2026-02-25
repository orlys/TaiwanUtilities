namespace TaiwanUtilities.UnitTests;

using System;

using Xunit;

/// <summary>
/// RocDateTime 算術運算子、比較運算子、Equals 迴歸測試
/// </summary>
[Trait("Category", "Unit")]
partial class RocDateTimeTest
{
    #region E3: Equals 迴歸測試（驗證使用 GetRawValue 而非 GetHashCode）

    [Fact]
    public static void Equals_相同日期()
    {
        var a = new RocDateTime(114, 1, 1);
        var b = new RocDateTime(114, 1, 1);

        Assert.True(a.Equals(b));
        Assert.True(a == b);
        Assert.False(a != b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public static void Equals_不同日期()
    {
        var a = new RocDateTime(114, 1, 1);
        var b = new RocDateTime(114, 1, 2);

        Assert.False(a.Equals(b));
        Assert.False(a == b);
        Assert.True(a != b);
    }

    [Fact]
    public static void Equals_僅時間不同()
    {
        var a = new RocDateTime(114, 6, 15, 10, 0, 0);
        var b = new RocDateTime(114, 6, 15, 10, 0, 1);

        Assert.False(a.Equals(b));
        Assert.False(a == b);
    }

    [Fact]
    public static void Equals_預設值()
    {
        var a = default(RocDateTime);
        var b = default(RocDateTime);

        Assert.True(a.Equals(b));
        Assert.True(a == b);
    }

    [Fact]
    public static void Equals_Object重載()
    {
        var a = new RocDateTime(114, 3, 15);
        var b = new RocDateTime(114, 3, 15);

        Assert.True(a.Equals((object)b));
        Assert.False(a.Equals("not a date"));
        Assert.False(a.Equals(null));
    }

    #endregion

    #region E2: 比較運算子

    [Fact]
    public static void 比較運算子_大於小於()
    {
        var earlier = new RocDateTime(114, 1, 1);
        var later = new RocDateTime(114, 12, 31);

        Assert.True(later > earlier);
        Assert.True(earlier < later);
        Assert.False(earlier > later);
        Assert.False(later < earlier);
    }

    [Fact]
    public static void 比較運算子_大於等於小於等於()
    {
        var a = new RocDateTime(114, 6, 15);
        var b = new RocDateTime(114, 6, 15);
        var c = new RocDateTime(114, 6, 16);

        Assert.True(a >= b);
        Assert.True(a <= b);
        Assert.True(c >= a);
        Assert.True(a <= c);
        Assert.False(a >= c);
    }

    [Fact]
    public static void CompareTo_排序順序()
    {
        var a = new RocDateTime(113, 1, 1);
        var b = new RocDateTime(114, 1, 1);
        var c = new RocDateTime(115, 1, 1);

        Assert.True(a.CompareTo(b) < 0);
        Assert.True(b.CompareTo(a) > 0);
        Assert.Equal(0, b.CompareTo(new RocDateTime(114, 1, 1)));
        Assert.True(c.CompareTo(b) > 0);
    }

    [Fact]
    public static void CompareTo_Object重載()
    {
        var a = new RocDateTime(114, 1, 1);
        var b = new RocDateTime(114, 6, 1);

        Assert.True(a.CompareTo((object)b) < 0);
        Assert.Throws<ArgumentException>(() => a.CompareTo("invalid"));
    }

    #endregion

    #region E2: 算術運算子

    [Fact]
    public static void AddDays_加減()
    {
        var date = new RocDateTime(114, 1, 1);

        var next = date.AddDays(1);
        Assert.Equal(2, next.Day);

        var prev = date.AddDays(-1);
        Assert.Equal(31, prev.Day);
        Assert.Equal(12, prev.Month);
        Assert.Equal(113, prev.Year);
    }

    [Fact]
    public static void AddMonths_加減()
    {
        var date = new RocDateTime(114, 1, 15);

        var next = date.AddMonths(1);
        Assert.Equal(2, next.Month);

        var prev = date.AddMonths(-1);
        Assert.Equal(12, prev.Month);
        Assert.Equal(113, prev.Year);
    }

    [Fact]
    public static void AddHours_跨日()
    {
        var date = new RocDateTime(114, 1, 1, 23, 0, 0);
        var next = date.AddHours(2);

        Assert.Equal(2, next.Day);
        Assert.Equal(1, next.Hour);
    }

    [Fact]
    public static void Add_TimeSpan()
    {
        var date = new RocDateTime(114, 6, 15, 12, 0, 0);
        var result = date.Add(TimeSpan.FromHours(3));

        Assert.Equal(15, result.Hour);
    }

    #endregion
}
