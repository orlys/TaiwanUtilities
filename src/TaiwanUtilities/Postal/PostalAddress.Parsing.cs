namespace TaiwanUtilities;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

partial class PostalAddress
#if NET7_0_OR_GREATER
    : IParsable<PostalAddress>
#endif
{
    /// <summary>
    /// 從地址字串解析組件
    /// </summary>
    /// <param name="address">台灣地址字串</param>
    /// <returns>解析後的地址組件</returns>
    /// <example>
    /// <code>
    /// var comp = PostalAddress.Parse("臺北市信義區市府路1之2號3樓");
    /// Console.WriteLine(comp.City);        // 臺北市
    /// Console.WriteLine(comp.District);    // 信義區
    /// Console.WriteLine(comp.Road);        // 市府路
    /// Console.WriteLine(comp.Number);      // 1
    /// Console.WriteLine(comp.SubNumbers?[0]);   // 2
    /// Console.WriteLine(comp.Floor);       // 3樓
    /// </code>
    /// </example>
    public static PostalAddress Parse(string address)
    {
        return Parse(address, null);
    }

    /// <summary>
    /// 從地址字串解析組件
    /// </summary>
    public static PostalAddress Parse(string address, IFormatProvider? provider)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return new PostalAddress { RawAddress = address ?? string.Empty };
        }

        var addr = new AddressTokenizer(address);
        var components = new PostalAddress
        {
            RawAddress = address,
            NormalizedAddress = addr.Flat()
        };

        // 使用啟發式規則提取各組件
        var tokens = addr.Tokens;

        for (int i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            var no = token[AddressTokenizer.NO];
            var subno = token[AddressTokenizer.SUBNO];
            var name = token[AddressTokenizer.NAME];
            var unit = token[AddressTokenizer.UNIT];

            // 解析門牌號碼和附號
            if (!string.IsNullOrEmpty(no) && unit == "號")
            {
                if (int.TryParse(no, out var number))
                    components.Number = number;

                // 檢查前一個 token 是否為「臨」（臨時門牌）
                if (i >= 1 && tokens[i - 1][AddressTokenizer.NAME] == "臨")
                {
                    components.IsTemporary = true;
                }

                // 解析所有附號（支援多層：之1之1之1）
                if (!string.IsNullOrEmpty(subno))
                {
                    var subNumbers = new List<int>();
                    var parts = subno.Split(new[] { '之' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var part in parts)
                    {
                        if (int.TryParse(part, out var sn))
                            subNumbers.Add(sn);
                    }

                    if (subNumbers.Count > 0)
                    {
                        components.SubNumbers = subNumbers;
                    }
                }
            }

            // 解析樓層
            if (!string.IsNullOrEmpty(no) && unit == "樓")
            {
                components.Floor = no + "樓";

                // 檢查下一個 token 是否為「之N」格式（如：5樓之3）
                if (i + 1 < tokens.Count)
                {
                    var nextToken = tokens[i + 1];
                    var nextName = nextToken[AddressTokenizer.NAME];

                    if (!string.IsNullOrEmpty(nextName) && nextName.StartsWith("之"))
                    {
                        var subFloorStr = nextName.Substring(1);
                        if (int.TryParse(subFloorStr, out var subFloor))
                        {
                            components.SubFloor = subFloor;
                        }
                    }
                }
            }

            // 解析層（地下樓層）
            if (!string.IsNullOrEmpty(no) && unit == "層")
            {
                if (i >= 1 && tokens[i - 1][AddressTokenizer.NAME] == "地下")
                {
                    components.Floor = "地下" + no + "層";
                    components.IsBasement = true;
                }
                else
                {
                    components.Floor = no + "層";
                }
            }

            // 解析室號（處理「5樓之3室」格式）
            if (!string.IsNullOrEmpty(no) && unit == "室")
            {
                components.Room = no + "室";

                if (i >= 2 &&
                    tokens[i - 1][AddressTokenizer.NAME] == "之" &&
                    tokens[i - 2][AddressTokenizer.UNIT] == "樓")
                {
                    if (int.TryParse(no, out var subFloor))
                    {
                        components.SubFloor = subFloor;
                    }
                }
            }

            // 解析縣市
            if (unit == "縣" || (unit == "市" && string.IsNullOrEmpty(components.City)))
            {
                components.City = name + unit;
            }
            else if (string.IsNullOrEmpty(unit) && !string.IsNullOrEmpty(name) &&
                     string.IsNullOrEmpty(components.City) && i == 0)
            {
                components.City = name;
            }

            // 解析區鄉鎮市
            if (unit is "區" or "鄉" or "鎮" || (unit == "市" && !string.IsNullOrEmpty(components.City) && components.City!.EndsWith("縣")))
            {
                components.District = name + unit;
            }

            // 解析村里
            if (unit is "村" or "里")
            {
                if (string.IsNullOrEmpty(components.Village))
                {
                    components.Village = name + unit;
                }
            }

            // 解析鄰
            if (unit == "鄰")
            {
                if (!string.IsNullOrEmpty(no))
                {
                    components.Neighborhood = no + "鄰";
                }
                else if (!string.IsNullOrEmpty(name))
                {
                    var neighborhoodNum = System.Text.RegularExpressions.Regex.Match(name, @"\d+");
                    if (neighborhoodNum.Success)
                    {
                        components.Neighborhood = neighborhoodNum.Value + "鄰";
                    }
                }
            }

            // 解析路街
            if (unit is "路" or "街")
            {
                components.Road = name + unit;
            }

            // 解析線（專用道路）
            if (unit == "線")
            {
                var branchNumber = "";
                if (!string.IsNullOrEmpty(subno))
                {
                    branchNumber = subno.Replace("之", "-");
                }

                if (i >= 1 && !string.IsNullOrEmpty(tokens[i - 1][AddressTokenizer.NAME]) &&
                    string.IsNullOrEmpty(tokens[i - 1][AddressTokenizer.UNIT]))
                {
                    var roadPrefix = tokens[i - 1][AddressTokenizer.NAME];
                    components.Road = roadPrefix + no + branchNumber + unit;
                }
                else if (!string.IsNullOrEmpty(no))
                {
                    components.Road = no + branchNumber + unit;
                }
            }

            // 解析段
            if (unit == "段")
            {
                if (!string.IsNullOrEmpty(no))
                {
                    components.Section = no + "段";
                }
                else if (!string.IsNullOrEmpty(name))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(name, @"^(.+?)(\d+)$");
                    if (match.Success)
                    {
                        var roadName = match.Groups[1].Value;
                        var sectionNo = match.Groups[2].Value;

                        if (string.IsNullOrEmpty(components.Road))
                        {
                            components.Road = roadName;
                        }

                        components.Section = sectionNo + "段";
                    }
                    else
                    {
                        components.Section = name + "段";
                    }
                }
            }

            // 解析巷
            if (unit == "巷")
            {
                if (!string.IsNullOrEmpty(no))
                {
                    components.Lane = no + "巷";
                }
                else if (!string.IsNullOrEmpty(name))
                {
                    if (string.IsNullOrEmpty(components.Road))
                    {
                        components.Road = name + unit;
                    }
                }
            }

            // 解析弄
            if (unit == "弄")
            {
                if (!string.IsNullOrEmpty(no))
                {
                    components.Alley = no + "弄";
                }
                else if (!string.IsNullOrEmpty(name))
                {
                    if (string.IsNullOrEmpty(components.Road))
                    {
                        components.Road = name + unit;
                    }
                }
            }

            // 解析衖（弄的下級）
            if (unit == "衖")
            {
                if (!string.IsNullOrEmpty(no))
                {
                    components.SubAlley = no + "衖";
                }
            }

            // 識別無單位字的地名（眷村、老聚落等）
            if (string.IsNullOrEmpty(unit) && !string.IsNullOrEmpty(name))
            {
                var knownPrefixes = new[] { "臨", "地下", "之" };

                var isRoadLinePrefix = i + 1 < tokens.Count &&
                                      tokens[i + 1][AddressTokenizer.UNIT] == "線";

                if (!string.IsNullOrEmpty(components.City) &&
                    !string.IsNullOrEmpty(components.District) &&
                    i < tokens.Count - 1 &&
                    !knownPrefixes.Contains(name) &&
                    !isRoadLinePrefix)
                {
                    components.Locality = name;
                }
            }
        }

        return components;
    }

    /// <summary>
    /// 嘗試從地址字串解析組件
    /// </summary>
    /// <param name="address">台灣地址字串</param>
    /// <param name="result">解析後的地址組件，若失敗則為 null</param>
    /// <returns>解析是否成功（地址為空或空白時返回 false）</returns>
    public static bool TryParse(
        [NotNullWhen(true)] string? address,
        out PostalAddress? result)
    {
        return TryParse(address, null, out result);
    }

    /// <summary>
    /// 嘗試從地址字串解析組件
    /// </summary>
    public static bool TryParse(
        [NotNullWhen(true)] string? address,
        IFormatProvider? provider,
        [MaybeNullWhen(false)] out PostalAddress? result)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            result = null;
            return false;
        }

        try
        {
            result = Parse(address!, provider);
            return true;
        }
        catch
        {
            result = null;
            return false;
        }
    }
}
