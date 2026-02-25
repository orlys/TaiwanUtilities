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
    public static void 初始化()
    {
        Assert.IsType<RocDateTime>(new RocDateTime(115, 1, 1));
    }

    #pragma warning disable CS0618 // IsHoliday is obsolete
    [Fact]
    public static void 國定假日判斷()
    {
        // 使用嵌入資料範圍內的固定年份，避免嵌入資料過期後 flaky
        var dataSet = RocHolidayDataSet.Default;
        var embeddedMaxRocYear = dataSet.EmbeddedMaxYear - 1911;
        Assert.True(RocDateTime.Parse(embeddedMaxRocYear + "/1/1").IsHoliday);
    }

    [Fact]
    public static void 國定假日超出範圍判斷()
    {
        Assert.False(RocDateTime.Parse("999/12/31").IsHoliday);
    }
    #pragma warning restore CS0618

    [Fact]
    public static void Holiday屬性_國定假日()
    {
        // 2025/1/1 開國紀念日
        var date = new RocDateTime(114, 1, 1);
        var holiday = date.Holiday;

        Assert.True(holiday.IsHoliday);
        Assert.True(holiday); // implicit bool
        Assert.Equal(HolidayRole.All, holiday.Role);
        Assert.Equal("開國紀念日", holiday.Description);
    }

    [Fact]
    public static void Holiday屬性_工作日()
    {
        // 2025/1/2 工作日
        var date = new RocDateTime(114, 1, 2);
        var holiday = date.Holiday;

        Assert.False(holiday.IsHoliday);
        Assert.False(holiday); // implicit bool
    }

    [Fact]
    public static void Holiday屬性_勞動節()
    {
        // 2025/5/1 勞動節
        var date = new RocDateTime(114, 5, 1);
        var holiday = date.Holiday;

        Assert.True(holiday);
        Assert.Equal(HolidayRole.Labor, holiday.Role);
        Assert.Equal("勞動節", holiday.Description);
    }

    [Fact]
    public static void Holiday屬性_歷史日期_1998()
    {
        // 1998/1/1 元旦 (民國87年)
        var date = new RocDateTime(87, 1, 1);
        var holiday = date.Holiday;

        Assert.True(holiday);
        Assert.Equal("元旦", holiday.Description);
    }

    [Fact]
    public static void Holiday屬性_超出嵌入範圍()
    {
        // 民國999年不在嵌入範圍內
        var date = RocDateTime.Parse("999/12/31");
        var holiday = date.Holiday;

        Assert.False(holiday);
        Assert.Equal(RocHoliday.None, holiday);
    }

    [Fact]
    public static void RocHoliday隱含布林轉換()
    {
        var holiday = new RocHoliday(true, HolidayRole.All, "測試");
        var nonHoliday = new RocHoliday(false, HolidayRole.None, "工作日");

        Assert.True(holiday);
        Assert.False(nonHoliday);
        Assert.False(RocHoliday.None);
    }

    [Fact]
    public static void HolidayRole_Flags語意()
    {
        // All 包含 Labor、Soldier 和 Teacher
        Assert.True(HolidayRole.All.HasFlag(HolidayRole.Labor));
        Assert.True(HolidayRole.All.HasFlag(HolidayRole.Soldier));
        Assert.True(HolidayRole.All.HasFlag(HolidayRole.Teacher));

        // Labor 不包含 Soldier
        Assert.False(HolidayRole.Labor.HasFlag(HolidayRole.Soldier));

        // None 是 default
        Assert.Equal(HolidayRole.None, default(HolidayRole));
        Assert.Equal(HolidayRole.None, RocHoliday.None.Role);

        // 國定假日（全民）的 Role 可用 HasFlag 判斷
        var newYear = new RocDateTime(114, 1, 1).Holiday;
        Assert.True(newYear.Role.HasFlag(HolidayRole.Labor));
        Assert.True(newYear.Role.HasFlag(HolidayRole.Teacher));

        // 勞動節只適用勞工
        var laborDay = new RocDateTime(114, 5, 1).Holiday;
        Assert.True(laborDay.Role.HasFlag(HolidayRole.Labor));
        Assert.False(laborDay.Role.HasFlag(HolidayRole.Soldier));
        Assert.False(laborDay.Role.HasFlag(HolidayRole.Teacher));
    }

    [Fact]
    public static async System.Threading.Tasks.Task RocHolidayDataSet_手動增刪()
    {
        var dataSet = RocHolidayDataSet.Default;

        // 新增自訂假日
        var testDate = new RocDateTime(114, 6, 15);
        var customHoliday = new RocHoliday(true, HolidayRole.All, "自訂假日");
        dataSet.Add(testDate, customHoliday);

        Assert.Equal(customHoliday, testDate.Holiday);

        // 移除
        Assert.True(dataSet.Remove(testDate));
        Assert.False(testDate.Holiday);

        // 清除 overrides
        await dataSet.ReloadAsync();
    }

    [Fact]
    public static void RocHolidayDataSet_嵌入年份範圍()
    {
        var dataSet = RocHolidayDataSet.Default;

        Assert.Equal(1998, dataSet.EmbeddedMinYear);
        Assert.Equal(2026, dataSet.EmbeddedMaxYear);
    }

    [Fact]
    public static void RocHolidayDataSet_ContainsYear()
    {
        var dataSet = RocHolidayDataSet.Default;

        Assert.True(dataSet.ContainsYear(1998));
        Assert.True(dataSet.ContainsYear(2025));
        Assert.True(dataSet.ContainsYear(2026));
        Assert.False(dataSet.ContainsYear(1997));
        Assert.False(dataSet.ContainsYear(2027));
    }

    [Fact]
    public static void Holiday屬性_軍人節()
    {
        // 2025/9/3 軍人節
        var date = new RocDateTime(114, 9, 3);
        var holiday = date.Holiday;

        Assert.True(holiday);
        Assert.Equal(HolidayRole.Soldier, holiday.Role);
        Assert.Equal("軍人節", holiday.Description);
    }

    [Fact]
    public static void Holiday屬性_教師節()
    {
        // 2025/9/28 教師節（孔子誕辰紀念日）
        var date = new RocDateTime(114, 9, 28);
        var holiday = date.Holiday;

        Assert.True(holiday);
        Assert.Equal(HolidayRole.Teacher, holiday.Role);
    }

    [Fact]
    public static void Holiday屬性_工作日Role為None()
    {
        // 2025/1/2 工作日
        var date = new RocDateTime(114, 1, 2);
        var holiday = date.Holiday;

        Assert.False(holiday);
        Assert.Equal(HolidayRole.None, holiday.Role);
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

        Assert.Throws<ArgumentOutOfRangeException>(() => new RocDateTime(0, 1, 1));

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

    [Fact]
    public static void 確保轉換為台北時間_1()
    {
        var n = DateTimeOffset.Parse("2025/09/26T01:23:45Z");

        var expected = "114年9月26日 9時23分45秒";

        Assert.Equal(expected, ((RocDateTime)n).ToString());
        Assert.Equal(expected, ((RocDateTime)n.ToLocalTime()).ToString());
        Assert.Equal(expected, ((RocDateTime)(n.LocalDateTime)).ToString());
        Assert.Equal(expected, ((RocDateTime)n.UtcDateTime).ToString());
    }

    [Fact]
    public static void 確保轉換為台北時間_2()
    {
        var taipeiNow = DateTimeOffset.UtcNow.ToOffset(RocDateTime.TimeZoneOffset);

        var now = RocDateTime.Now;

        Assert.InRange(now.TimeOfDay, taipeiNow.TimeOfDay, taipeiNow.TimeOfDay.Add(TimeSpan.FromSeconds(1)));
    }
}
