namespace TaiwanUtilities;

using System.Collections.Generic;
using System.Text;

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
}