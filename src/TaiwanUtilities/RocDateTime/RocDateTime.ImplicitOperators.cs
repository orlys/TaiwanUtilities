namespace TaiwanUtilities;
using System;
using System.Data.SqlTypes;

partial struct RocDateTime
{

    public static implicit operator RocDateTime(DateTimeOffset dateTimeOffset) => new(dateTimeOffset);

    public static implicit operator RocDateTime(DateTime dateTime) => new RocDateTime(dateTime);
    public static implicit operator DateTime(RocDateTime rocDateTime) => rocDateTime.GetRawValue().DateTime;
    public static implicit operator DateTimeOffset(RocDateTime rocDateTime) => rocDateTime.GetRawValue();


    public static implicit operator SqlDateTime(RocDateTime rocDateTime) => new SqlDateTime(rocDateTime.Date);
    public static implicit operator RocDateTime(SqlDateTime sqlDateTime) => new RocDateTime(sqlDateTime.Value);



#if NET6_0_OR_GREATER
    public void Deconstruct(out DateOnly date, out TimeOnly time)
    {
#if NET8_0_OR_GREATER
        (date, time) = GetRawValue().DateTime;
#else
        var dt = GetRawValue();
        date = new DateOnly(dt.Year, dt.Month, dt.Day);
        time = new TimeOnly(dt.Hour, dt.Minute, dt.Second, dt.Millisecond);
#endif
    }
#endif
}
