namespace TaiwanUtilities.UnitTests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TaiwanUtilities;
using Xunit;


partial class RocDateTimeTest
{

	[Theory]
	[InlineData("民國貳參年陸月玖日", "1934/06/09")]
    [InlineData("民國貳十參年陸月玖日", "1934/06/09")]
    [InlineData("民國101年02月29日", "2012/02/29")]
    public static void 民國年字串解析(string input, string expectedDate)
	{
		Assert.Equal(
			expected: DateTime.Parse(expectedDate), 
			actual: RocDateTime.Parse(input));
	}

    [Theory]
    [InlineData("民國88年2月29日")]  // 1999 非閏年
    [InlineData("民國88年4月31日")]
    public static void 不存在的日期_TryParse_回傳false不拋例外(string input)
    {
        Assert.False(RocDateTime.TryParse(input, out _));
        Assert.False(RocDateTime.TryParse(input.AsSpan(), out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => RocDateTime.Parse(input));
    }



}

