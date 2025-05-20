using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

using TaiwanUtilities;


Console.BufferHeight = short.MaxValue - 1;

var c = ChineseNumeric.Parse("十八");
Console.WriteLine(c.ToString("HW"));


if (PostalAddress.TryParse("南投縣草屯鎮東豐里12鄰中正路254之85之11巷3弄2衕臨2號地下18樓-1A", out var a))
{
    Console.WriteLine(a.Area);
    Console.WriteLine(a.Address);
    Console.WriteLine(a.IsTemporary);
}

return;
var p = Path.Combine([Environment.CurrentDirectory, .. Enumerable.Repeat("..", 5), "data", "zipcode.json"]);
await using var zipCodeStream = File.OpenRead(p);

var s = new HashSet<string>();
await foreach (var zipCode in JsonSerializer.DeserializeAsyncEnumerable<JsonZipCode>(zipCodeStream))
{
    //Console.WriteLine(zipCode.Code + " => " + zipCode.Range);
    var r = zipCode.Range;
    //Console.WriteLine(r);

    r = Regex.Replace(r, @"(?<NUMBER>\s*?\d+)", "x");
    
    s.Add(r);
}
 
foreach (var item in s.OrderBy(x => x.Length).ThenBy(x => x))
{
    Console.WriteLine(item);
}

Console.ReadKey();

// https://regex101.com/r/3pJ6sw/1


public class JsonZipCode
{
    /// <summary>
    /// 五碼郵遞區號
    /// </summary>
    [JsonPropertyName("郵遞區號")]
    public string? Code { get; set; }

    /// <summary>
    /// 三碼郵遞區號
    /// </summary>
    [JsonIgnore]
    public string ShortCode => Code.AsSpan(0, 3).ToString();


    [JsonPropertyName("縣市名稱")]
    public string? City { get; set; }
    [JsonPropertyName("鄉鎮市區")]
    public string? District { get; set; }
    [JsonPropertyName("原始路名")]
    public string? Street { get; set; }
    [JsonPropertyName("投遞範圍")]
    public string? Range { get; set; }
}
