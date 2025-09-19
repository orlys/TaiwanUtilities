//namespace TaiwanUtilities;

//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//partial struct RocDateTime
//{ 
//    public static void SetHolidayStore(IRocHolidayStore holidayStore)
//    {
//        Guard.ThrowIfNull(holidayStore);
//        s_holidayStore = holidayStore;
//    }
//    public static IRocHolidayStore HolidayStore => s_holidayStore ??= new DefaultHolidayStore();
//    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
//    private static volatile IRocHolidayStore s_holidayStore = null!;

//}
