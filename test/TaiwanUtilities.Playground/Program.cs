using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;

using TaiwanUtilities;

static class Program
{
    static void Main(string[] args)
    {
        //var r = new Regex(@"[\u4E00-\uFFF3]");
        var v = "𢊬";
        var m = IsChinese("1" + "𢊬");
        
        var vx = Encoding.Unicode.GetBytes("𢊬");
        Console.WriteLine(vx.Length);
    }

    static bool IsChinese(string s)
    {
        
        var l = s.Length;
        return s.EnumerateRunes().All(r =>
            (r.Value >= 0x4E00 && r.Value <= 0x9FFF) ||   // 基本
            (r.Value >= 0x3400 && r.Value <= 0x4DBF) ||   // 擴展A
            (r.Value >= 0x20000 && r.Value <= 0x2A6DF) || // 擴展B
            (r.Value >= 0x2A700 && r.Value <= 0x2B73F) || // 擴展C
            (r.Value >= 0x2B740 && r.Value <= 0x2B81F) || // 擴展D
            (r.Value >= 0x2B820 && r.Value <= 0x2CEAF) || // 擴展E
            (r.Value >= 0x2CEB0 && r.Value <= 0x2EBEF) || // 擴展F
            (r.Value >= 0x30000 && r.Value <= 0x3134F));  // 擴展G
    }
}

public class JsonC : JsonConverter<bool>
{
    public JsonC()
    {

    }
    public override bool ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return base.ReadAsPropertyName(ref reader, typeToConvert, options);
    }
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is JsonTokenType.True or JsonTokenType.False)
        {
            return reader.GetBoolean();
        }

        if (reader.TokenType is JsonTokenType.String &&
            reader.GetString() is { } str)
        {
            return str is "是" ? true :
                   str is "否" ? false :
                   throw new JsonException($"Invalid boolean value: {str}");
        }

        throw new JsonException($"Unexpected token {reader.TokenType} when parsing boolean value.");
    }

    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}

sealed class DateInfo
{
    [JsonPropertyName("date")]
    public string Date { get; set; }

    [JsonConverter(typeof(JsonC))]
    //[JsonPropertyName("isholiday")]
    public bool IsHoliday { get; set; }

    public override int GetHashCode()
    {
        {
            return Date?.GetHashCode() ?? 0;
        }
    }

    public override bool Equals(object obj)
    {
        {
            return obj is DateInfo di ? GetHashCode() == di.GetHashCode() : false;
        }
    }
}


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
