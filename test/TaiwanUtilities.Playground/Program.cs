using TaiwanUtilities;

static class Program
{
    static void Main(string[] args)
    {

        //var c = TypeDescriptor.GetConverter(typeof(RocDateTime));

        //var javaScriptSerializer = new JavaScriptSerializer { RecursionLimit = 3 };
        
        //var v = javaScriptSerializer.Serialize(RocDateTime.Now);
        //Console.WriteLine(v);

        var r = ZipCode.Find("台北市中區");

        //Console.WriteLine(Convert.ToString((object)RocDateTime.Now, CultureInfo.CurrentCulture));
        return;
    }
}


//if (OperatingSystem.IsWindows())
//{
//    Console.BufferHeight = short.MaxValue - 1;
//}


//var p = Path.Combine([Environment.CurrentDirectory, .. Enumerable.Repeat("..", 5), "data", "zipcode.json"]);
//await using var zipCodeStream = File.OpenRead(p);

//var trie = new TrieDictionary<JsonZipCode>();

//var rangeRules = new HashSet<string>();
//var map = new Dictionary<string, HashSet<string>>();
//await foreach (var zipCode in JsonSerializer
//    .DeserializeAsyncEnumerable<JsonZipCode>(zipCodeStream))
//{
//    ////Console.WriteLine(zipCode.Code + " => " + zipCode.Range);
//    //var r = zipCode.Range;
//    ////Console.WriteLine(r);
//    //r = Regex.Replace(r, @"(?<NUMBER>\s*?\d+)", "x");
//    //rangeRules.Add(r);

//    //if (PostalAddress.TryParse(zipCode.Address, out var pa))
//    //{
//    //    trie.TryAdd(pa.Value, zipCode);
//    //}
//    //else
//    //{
//    //    Console.WriteLine(zipCode.Address);
//    //}

//    if (!map.TryGetValue(zipCode.County, out var m))
//    {
//        m = new HashSet<string> { zipCode.Town };
//        map.Add(zipCode.County, m);
//    }
//    else
//    {
//        m.Add(zipCode.Town);
//    }
//}


//var list = new List<string>();
//foreach (var counties in map.GroupBy(x => x.Key.Last()))
//{

//    if (counties.Key is '市')
//    {
//        foreach (var city in counties)
//        {
//            var region = city.Value.Where(x => x.EndsWith("區")).ToArray();
//            var islands = city.Value.Where(x => x.EndsWith("島")).ToArray();
//            var parts = new List<string>();

//            if (region.Any())
//            {
//                var s = $"({string.Join("|", region.Select(x => x.TrimEnd('區')))})區";

//                s += "(?<VILLAGE>[\\u4E00-\\u9FFF]{2,3}里)?";

//                parts.Add(s);
//            }

//            if (islands.Any())
//            {

//                parts.Add($"({string.Join("|", islands)})");
//            }

//            list.Add($"((?<COUNTY>{city.Key})(?<TOWN>{string.Join("|", parts)}))");
//        }
//    }

//    else
//    if (counties.Key is '縣')
//    {

//        foreach (var country in counties)
//        {
//            var cities = country.Value.Where(x => x.EndsWith("市")).ToArray();
//            var towns = country.Value.Where(x => x.EndsWith("鎮")).ToArray();
//            var townships = country.Value.Where(x => x.EndsWith("鄉")).ToArray();
//            var parts = new List<string>();
//            var s = "";
//            if (cities.Any())
//            {
//                s += $"({string.Join("|", cities.Select(x => x.TrimEnd('市')))})市";
//            }
//            if (towns.Any())
//            {
//                if (s.Any())
//                {
//                    s += "|";
//                }
//                s += $"({string.Join("|", towns.Select(x => x.TrimEnd('鎮')))})鎮";
//            }

//            if (s.Any())
//            {
//                s += "(?<VILLAGE>[\\u4E00-\\u9FFF]{2,3}里)?";
//            }


//            if (townships.Any())
//            {
//                if (s.Any())
//                {
//                    s += "|";
//                }

//                s += $"({string.Join("|", townships.Select(x => x.TrimEnd('鄉')))})鄉";


//                s += "(?<VILLAGE>[\\u4E00-\\u9FFF]{2,3}村)?";
//            }


//            list.Add($"((?<COUNTY>{country.Key})(?<TOWN>{s}))");
//        }
//    }
//    else
//    {

//        foreach (var s in counties.SelectMany(x => x.Value))
//        {

//            Console.WriteLine("SKIPPED: " + s);
//        }
//        continue;
//    }
//}

//var result = $"(?<AREA>{string.Join("|", list)})".Replace("臺", "[臺台]");
//Console.WriteLine(result);

////foreach (var item in map)
////{
////    Console.WriteLine($"{item.Key}({string.Join("|", item.Value)})");
////}


////foreach (var item in rangeRules.OrderBy(x => x.Length).ThenBy(x => x))
////{
////    Console.WriteLine(item);
////}

//Console.ReadKey();

//// https://regex101.com/r/3pJ6sw/1


//public class JsonZipCode
//{
//    /// <summary>
//    /// 五碼郵遞區號
//    /// </summary>
//    [JsonPropertyName("郵遞區號")]
//    public string? Code { get; set; }

//    /// <summary>
//    /// 三碼郵遞區號
//    /// </summary>
//    [JsonIgnore]
//    public string ShortCode => Code.AsSpan(0, 3).ToString();

//    [JsonIgnore]
//    public string Address => string.Concat(County, Town, Road);

//    [JsonPropertyName("縣市名稱")]
//    public string? County { get; set; }

//    [JsonPropertyName("鄉鎮市區")]
//    public string? Town { get; set; }

//    [JsonPropertyName("原始路名")]
//    public string? Road { get; set; }

//    /// <summary>
//    /// 
//    /// </summary>
//    /// <remarks><see href="https://www.post.gov.tw/post/internet/Postal/sz_a_b_ta1.jsp"/></remarks>
//    [JsonPropertyName("投遞範圍")]
//    public string? Range { get; set; }
//}
