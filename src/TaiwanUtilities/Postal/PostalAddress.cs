namespace TaiwanUtilities;

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

/// <summary>
/// 地址
/// </summary>

#if NET8_0_OR_GREATER
[Experimental(nameof(PostalAddress), UrlFormat = "https://github.com/orlys/TaiwanUtilities/blob/master/docs/experimental.md#{0}")]
#else
[Obsolete("This API is experimental and subject to change.", false)]
#endif
public sealed partial class PostalAddress
{

    private PostalAddress(
        string county,
        string town,
        string? village,
        string? neighbor,
        string area,
        string? road,
        string? lane,
        string? alley,
        string? subAlley,
        string? number,
        string? floor,
        string? room,
        string address,
        bool isTemporary)
    {
        County = county;
        Town = town;
        Village = village;
        Neighbor = neighbor;
        Area = area;

        Road = road;
        Lane = lane;
        Alley = alley;
        SubAlley = subAlley;
        Number = number;
        Floor = floor;
        Room = room;
        Address = address;

        IsTemporary = isTemporary;
    }

    /// <summary>
    /// 縣市
    /// </summary>
    public string County { get; }

    /// <summary>
    /// 鄉鎮市
    /// </summary>
    public string Town { get; }

    /// <summary>
    /// 村里
    /// </summary>
    public string? Village { get; }

    /// <summary>
    /// 鄰
    /// </summary>
    public string? Neighbor { get; }

    /// <summary>
    /// 地名
    /// </summary>
    public string Area { get; }

    /// <summary>
    /// 路名
    /// </summary>
    public string? Road { get; }

    /// <summary>
    /// 巷名
    /// </summary>
    public string? Lane { get; }

    /// <summary>
    /// 弄號
    /// </summary>
    public string? Alley { get; }

    /// <summary>
    /// 衖衕
    /// </summary>
    public string? SubAlley { get; }

    /// <summary>
    /// 戶號
    /// </summary>
    public string? Number { get; }

    /// <summary>
    /// 樓
    /// </summary>
    public string? Floor { get; }

    /// <summary>
    /// 室
    /// </summary>
    public string? Room { get; }

    /// <summary>
    /// 地址
    /// </summary>
    public string? Address { get; }


    /// <summary>
    /// 是否為臨時地址
    /// </summary>
    public bool IsTemporary { get; }

    private static string? NullIfEmpty(string s)
    {
        return string.IsNullOrEmpty(s) ? null : s;
    }

    ///// <summary>
    ///// 分級
    ///// </summary>
    //[Flags]
    //public enum Grading
    //{
    //    None,
    //    County = 2,
    //    Town = 4,
    //    Village = 8,
    //    Neighbor = 16,

    //    Area = County | Town | Village | Neighbor,

    //    Road = 32,
    //    Lane = 64,
    //    Alley = 128,
    //    SubAlley = 256,
    //    Number = 512,
    //    Floor = 1024,
    //    Room = 2048,

    //    Address = Road | Lane | Alley | SubAlley | Number | Floor | Room,
    //}

    private string GetRawValue()
    {
        return Value;
    }

    public string Value
    {
        get
        {
            return Area + Address;
        }
    }
}
