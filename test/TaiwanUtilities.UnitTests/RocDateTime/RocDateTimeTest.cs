namespace TaiwanUtilities.UnitTests;

using System;

using Xunit;

[Trait("Category", "Unit")]
public partial class RocDateTimeTest
{
    [Fact]
    public static void 預設值()
    {
        Assert.Equal(
            expected: RocDateTime.Era,
            actual: default(RocDateTime));
    }

    [Fact]
    public static void 國定假日判斷()
    {
        Assert.True(RocDateTime.Parse("114/1/1").IsHoliday);
    }

    [Fact]
    public static void 國定假日超出範圍判斷()
    {
        Assert.False(RocDateTime.Parse("999/12/31").IsHoliday);
    }

    [Fact]
    public static void 年份轉換測試()
    {
        Assert.Equal(
            expected: new RocDateTime(-2, 1, 1),
            actual: RocDateTime.Era.AddYears(-2));

        Assert.Equal(
            expected: new RocDateTime(-1, 1, 1),
            actual: RocDateTime.Era.AddYears(-1));

        Assert.Equal(
            expected: new RocDateTime(0, 1, 1),
            actual: RocDateTime.Era.AddYears(0));

        Assert.Equal(
            expected: new RocDateTime(1, 1, 1),
            actual: RocDateTime.Era.AddYears(0));

        Assert.Equal(
            expected: new RocDateTime(2, 1, 1),
            actual: RocDateTime.Era.AddYears(1));

        Assert.Equal(
            expected: new RocDateTime(3, 1, 1),
            actual: RocDateTime.Era.AddYears(2));



        var _1913 = RocDateTime.Era.AddYears(+1);
        Assert.Equal(
            expected: 2,
            actual: _1913.Year);
        Assert.False(_1913.BeforeEra);

        var _1912 = RocDateTime.Era.AddYears(+0);
        Assert.Equal(
            expected: 1,
            actual: _1912.Year);
        Assert.False(_1912.BeforeEra);

        var _1911 = RocDateTime.Era.AddYears(-1);
        Assert.Equal(
            expected: 1,
            actual: _1911.Year);
        Assert.True(_1911.BeforeEra);

        var _1910 = RocDateTime.Era.AddYears(-2);
        Assert.Equal(
            expected: 2,
            actual: _1910.Year);
        Assert.True(_1910.BeforeEra);

        var _1909 = RocDateTime.Era.AddYears(-3);
        Assert.Equal(
            expected: 3,
            actual: _1909.Year);
        Assert.True(_1909.BeforeEra);
    }
}
