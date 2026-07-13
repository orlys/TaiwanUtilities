namespace TaiwanUtilities;

using System.Collections.Generic;
using System.Text;

using TaiwanUtilities.Internals;

partial class PostalAddress
{
    /// <summary>
    /// 取得結構化的地址字串表示（偵錯用）
    /// </summary>
    public override string ToString()
    {
        var parts = new List<string>
        {
            $"City={City}",
            $"District={District}",
            $"Road={Road}"
        };

        if (!string.IsNullOrEmpty(Section))
        {
            parts.Add($"Section={Section}");
        }

        if (!string.IsNullOrEmpty(Lane))
        {
            parts.Add($"Lane={Lane}");
        }

        if (!string.IsNullOrEmpty(Alley))
        {
            parts.Add($"Alley={Alley}");
        }

        if (!string.IsNullOrEmpty(SubAlley))
        {
            parts.Add($"SubAlley={SubAlley}");
        }

        if (!string.IsNullOrEmpty(Locality))
        {
            parts.Add($"Locality={Locality}");
        }

        if (Number.HasValue)
        {
            parts.Add($"Number={Number}");
        }

        if (SubNumbers != null && SubNumbers.Count > 0)
        {
            parts.Add($"SubNumbers=[{string.Join(", ", SubNumbers)}]");
        }

        if (!string.IsNullOrEmpty(Floor))
        {
            parts.Add($"Floor={Floor}");
        }

        if (SubFloor.HasValue)
        {
            parts.Add($"SubFloor={SubFloor}");
        }

        if (!string.IsNullOrEmpty(Room))
        {
            parts.Add($"Room={Room}");
        }

        if (IsTemporary)
        {
            parts.Add($"IsTemporary=True");
        }

        if (IsBasement)
        {
            parts.Add($"IsBasement=True");
        }

        return $"PostalAddress({string.Join(", ", parts)})";
    }

    /// <summary>
    /// 取得正規化的完整地址字串
    /// </summary>
    /// <returns>完整地址字串（如：臺北市信義區市府路1號3樓）</returns>
    public string ToAddressString()
    {
        if (!string.IsNullOrEmpty(NormalizedAddress))
        {
            return NormalizedAddress;
        }

        var sb = new StringBuilder();

        if (!string.IsNullOrEmpty(City))
        {
            sb.Append(City);
        }

        if (!string.IsNullOrEmpty(District))
        {
            sb.Append(District);
        }

        if (!string.IsNullOrEmpty(Village))
        {
            sb.Append(Village);
        }

        if (!string.IsNullOrEmpty(Neighborhood))
        {
            sb.Append(Neighborhood);
        }

        if (!string.IsNullOrEmpty(Road))
        {
            sb.Append(Road);
        }

        if (!string.IsNullOrEmpty(Section))
        {
            sb.Append(Section);
        }

        if (!string.IsNullOrEmpty(Lane))
        {
            sb.Append(Lane);
        }

        if (!string.IsNullOrEmpty(Alley))
        {
            sb.Append(Alley);
        }

        if (!string.IsNullOrEmpty(SubAlley))
        {
            sb.Append(SubAlley);
        }

        if (IsTemporary)
        {
            sb.Append("臨");
        }

        if (Number.HasValue)
        {
            sb.Append(Number);
            if (SubNumbers != null && SubNumbers.Count > 0)
            {
                foreach (var sn in SubNumbers)
                {
                    sb.Append("之");
                    sb.Append(sn);
                }
            }
            sb.Append('號');
        }

        if (!string.IsNullOrEmpty(Floor))
        {
            sb.Append(Floor);
        }

        if (SubFloor.HasValue)
        {
            sb.Append("之");
            sb.Append(SubFloor);
        }

        if (!string.IsNullOrEmpty(Room))
        {
            sb.Append(Room);
        }

        return sb.ToString();
    }

    /// <summary>
    /// 轉換為官方格式英文地址；無法解析或查無官方英文路名時回傳 null。
    /// </summary>
    public string? ToEnglish()
    {
        if (string.IsNullOrEmpty(City) ||
            string.IsNullOrEmpty(District) ||
            (string.IsNullOrEmpty(Road) && string.IsNullOrEmpty(RoadKey)))
        {
            return null;
        }

        var road = Road ?? string.Empty;
        if (!string.IsNullOrEmpty(Section))
        {
            road += PostalRulesEngine.ToChineseSection(Section!);
        }

        // 複合路名鍵以完整原生鍵優先查群組（未拆解時 RoadKey == road）
        int cityIdx = 0, districtIdx = 0, groupIdx = 0;
        bool found = false;
        if (!string.IsNullOrEmpty(RoadKey))
            found = PostalLookup.TryFindIndexed(City, District, RoadKey, out cityIdx, out districtIdx, out groupIdx);
        if (!found)
            found = PostalLookup.TryFindIndexed(City, District, road, out cityIdx, out districtIdx, out groupIdx);
        if (!found)
        {
            var chineseRoad = PostalRulesEngine.ArabicToChineseInRoad(road);
            if (ReferenceEquals(chineseRoad, road) ||
                !PostalLookup.TryFindIndexed(City, District, chineseRoad, out cityIdx, out districtIdx, out groupIdx))
            {
                return null;
            }
        }

        var englishCity = PostalData.EnglishCityNames[cityIdx];
        var englishDistrict = PostalData.EnglishDistrictNames[districtIdx];
        var englishRoad = PostalLookup.GetEnglishRoad(groupIdx);
        if (string.IsNullOrEmpty(englishCity) ||
            string.IsNullOrEmpty(englishDistrict) ||
            string.IsNullOrEmpty(englishRoad))
        {
            return null;
        }

        var parts = new List<string>(7);
        AddIfNotEmpty(parts, FormatFloor());
        AddIfNotEmpty(parts, FormatNumber());
        AddIfNotEmpty(parts, FormatNumericComponent("Aly.", Alley));
        AddIfNotEmpty(parts, FormatNumericComponent("Ln.", Lane));
        parts.Add(englishRoad);
        parts.Add(englishDistrict);
        parts.Add(englishCity);

        return string.Join(", ", parts);
    }

    private string? FormatNumber()
    {
        if (!Number.HasValue)
        {
            return null;
        }

        var sb = new StringBuilder();
        sb.Append("No. ");
        sb.Append(Number.Value);
        if (SubNumbers != null && SubNumbers.Count > 0)
        {
            for (int i = 0; i < SubNumbers.Count; i++)
            {
                sb.Append('-');
                sb.Append(SubNumbers[i]);
            }
        }

        return sb.ToString();
    }

    private string? FormatFloor()
    {
        var floorNumber = PostalRulesEngine.ParseNumericPrefix(Floor);
        if (floorNumber <= 0)
        {
            return null;
        }

        var sb = new StringBuilder();
        if (IsBasement)
        {
            sb.Append('B');
        }

        sb.Append(floorNumber);
        sb.Append("F.");
        if (SubFloor.HasValue)
        {
            sb.Append('-');
            sb.Append(SubFloor.Value);
        }

        return sb.ToString();
    }

    private static string? FormatNumericComponent(string prefix, string? value)
    {
        var n = PostalRulesEngine.ParseNumericPrefix(value);
        return n > 0 ? prefix + " " + n : null;
    }

    private static void AddIfNotEmpty(List<string> parts, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            parts.Add(value!);
        }
    }
}
