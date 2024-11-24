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
}
