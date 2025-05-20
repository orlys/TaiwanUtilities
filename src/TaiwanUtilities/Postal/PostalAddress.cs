namespace TaiwanUtilities;

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

/// <summary>
/// 地址
/// </summary>
public sealed partial class PostalAddress
{
    private static string EnsureNotNull(string? value, [CallerArgumentExpression(nameof(value))] string paramName = default)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentNullException(paramName);
        }
        return value;
    }

    private PostalAddress(
        string county,
        string town,
        string? village,
        string? neighbor,
        string area,
        string? street,
        string? lane,
        string? alley,
        string? subAlley,
        string? number,
        string? floor,
        string? room,
        string address,
        bool isTemporary)
    {
        County = EnsureNotNull(county);
        Town = EnsureNotNull(town);
        Village = village;
        Neighbor = neighbor;
        Area = EnsureNotNull(area);

        Road = street;
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
    //    Township = 4,
    //    Village = 8,
    //    Neighborhood = 16,

    //    Region = County | Township | Village | Neighborhood,

    //    Street = 32,
    //    Lane = 64,
    //    Alley = 128,
    //    SubAlley = 256,
    //    Number = 512,
    //    Floor = 1024,

    //    Address = Street | Lane | Alley | SubAlley | Number | Floor,
    //}

    private string GetRawValue()
    {
        return Area + Address;
    }
}
