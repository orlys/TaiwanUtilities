namespace TaiwanUtilities.UnitTests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;

partial class RocDateTimeTest
{
    [Theory]
    [InlineData("114/12/01", "d")]
    [InlineData("114/12/01", "date")]
    [InlineData("114年12月1日", "DATE")]
    [InlineData("114年12月1日", "D")]
    [InlineData("114年12月01", "Mdd")]
    [InlineData("114年12月", "M")]
    [InlineData("114/12", "m")]
    [InlineData("114", "yyy")]
    [InlineData("114", "year")]
    [InlineData("12", "MM")]
    [InlineData("12", "month")]
    [InlineData("01", "dd")]
    [InlineData("01", "day")]
    [InlineData("10", "hh")]
    [InlineData("10", "hour")]
    [InlineData("23", "mm")]
    [InlineData("23", "min")]
    [InlineData("23", "minute")]
    [InlineData("45", "ss")]
    [InlineData("45", "sec")]
    [InlineData("45", "second")]
    [InlineData("10:23:45", "t")]
    [InlineData("10:23:45", "time")]
    [InlineData("10時23分45秒", "T")]
    [InlineData("10時23分45秒", "TIME")]
    [InlineData("114/12/01 10:23:45", "f")]
    [InlineData("114/12/01 10:23:45", "full")]
    [InlineData("114年12月1日10時23分45秒", "F")]
    [InlineData("114年12月1日10時23分45秒", "FULL")]
    [InlineData("114/12/01 10:23:45", "g")]
    [InlineData("114年12月1日 10時23分45秒", "G")]
    [InlineData("民國一百一十四年十二月一日", "民國日期")]
    [InlineData("一百一十四年十二月一日", "日期")]
    [InlineData("十時二十三分四十五秒", "時間")]
    [InlineData("一百一十四年十二月一日十時二十三分四十五秒", "日期時間")]
    [InlineData("民國一百一十四年十二月一日十時二十三分四十五秒", "民國日期時間")]
    [InlineData("114/12/0110:23:45", "datetime")]
    [InlineData("114/12/01 10:23:45", "date time")]
    [InlineData("114-12-01", "yyy-MM-dd")]
    [InlineData("1141201", "yyyMMdd")]
    public static void 民國年轉字串(string expected, string format)
    {
        var value = new RocDateTime(114, 12, 1, 10, 23, 45);

        Assert.Equal(expected, value.ToString(format));
    }


    [Fact]
    public static void 民國年轉字串_格式_G()
    {
        // 格式 G (通用格式) 在 time 為 0 時會把省略掉，

        var value = new RocDateTime(114, 12, 1, 10, 23, 00);
        var expected = "114年12月1日 10時23分0秒";
        var actual = value.ToString("G");
        Assert.Equal(expected, actual);


        expected = "114年12月1日";
        // Date: new RocDateTime(114, 12, 1, 0, 0, 0);
        actual = value.Date.ToString("G");
        Assert.Equal(expected, actual);
    }
}
