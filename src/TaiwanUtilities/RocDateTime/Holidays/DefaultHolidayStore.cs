//namespace TaiwanUtilities;

//using System.Collections.Concurrent;
//using System.Collections.Generic;

//internal partial class HolidayStoreBase : IRocHolidayStore
//{
//    private readonly ConcurrentDictionary<int, ISet<RocDateTime>> _table;
//    public HolidayStoreBase()
//    {
//        _table = new ConcurrentDictionary<int, ISet<RocDateTime>>();

//        OnInit(_table);
//    }

//    private partial void OnInit(ConcurrentDictionary<int, ISet<RocDateTime>> table);


//    public RocHolidayInfo? GetHolidayInfo(RocDateTime date)
//    { 
//    }

//}
