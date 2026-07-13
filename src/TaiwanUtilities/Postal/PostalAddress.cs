// SPDX-License-Identifier: MIT
// Copyright (c) 2024-2026 Orlys
//
// TaiwanUtilities - Taiwan Postal Code Query Library
// Postal code data from Chunghwa Post under OGDL-Taiwan-1.0

namespace TaiwanUtilities;

using System;
using System.Collections.Generic;
using System.Linq;

using TaiwanUtilities.Internals;

/// <summary>
/// 代表結構化的台灣郵政地址
/// </summary>
public partial class PostalAddress
{
    /// <summary>
    /// 縣市（如：臺北市）
    /// </summary>
    public string? City { get; set; }

    /// <summary>
    /// 行政區（如：信義區）
    /// </summary>
    public string? District { get; set; }

    /// <summary>
    /// 村里
    /// </summary>
    public string? Village { get; set; }

    /// <summary>
    /// 鄰
    /// </summary>
    public string? Neighborhood { get; set; }

    /// <summary>
    /// 路街（如：市府路）
    /// </summary>
    public string? Road { get; set; }

    /// <summary>
    /// 命中資料庫的完整原生路名鍵（未拆解前）。當路名為「規則裡包著別的規則」的
    /// 複合鍵（如 中正路一段篤行三村）時，<see cref="Road"/>/<see cref="Section"/>/
    /// <see cref="Locality"/> 為顯示用的拆解結果，而查詢仍以此完整鍵為準。
    /// 未匹配到資料庫路名時為 null。
    /// </summary>
    internal string? RoadKey { get; set; }

    /// <summary>
    /// 段
    /// </summary>
    public string? Section { get; set; }

    /// <summary>
    /// 巷
    /// </summary>
    public string? Lane { get; set; }

    /// <summary>
    /// 弄
    /// </summary>
    public string? Alley { get; set; }

    /// <summary>
    /// 衖（弄的下級，如：48衖）
    /// </summary>
    public string? SubAlley { get; set; }

    /// <summary>
    /// 門牌號碼
    /// </summary>
    public int? Number { get; set; }

    /// <summary>
    /// 附號（支援多層如：150之1之1之1 → [1, 1, 1]）
    /// </summary>
    public List<int>? SubNumbers { get; set; }

    /// <summary>
    /// 是否為臨時門牌（如：臨11號）
    /// </summary>
    public bool IsTemporary { get; set; }

    /// <summary>
    /// 是否為地下樓層（如：地下一層）
    /// </summary>
    public bool IsBasement { get; set; }

    /// <summary>
    /// 樓
    /// </summary>
    public string? Floor { get; set; }

    /// <summary>
    /// 樓層附號（如：5樓之3 中的 3）
    /// </summary>
    public int? SubFloor { get; set; }

    /// <summary>
    /// 室（如：101室）
    /// </summary>
    public string? Room { get; set; }

    /// <summary>
    /// 原始地址
    /// </summary>
    public string RawAddress { get; set; } = string.Empty;

    /// <summary>
    /// 正規化地址
    /// </summary>
    public string NormalizedAddress { get; set; } = string.Empty;

    /// <summary>
    /// 地區名稱（部落、眷村、聚落等）
    /// </summary>
    public string? Locality { get; set; }

    /// <summary>
    /// 取得完整門牌號碼（含所有附號）
    /// </summary>
    /// <returns>完整門牌號碼字串，如「1之2號」或「150之1之1之1號」</returns>
    /// <example>
    /// <code>
    /// var comp = PostalAddress.Parse("臺北市信義區市府路1之2號");
    /// Console.WriteLine(comp.GetFullNumber()); // "1之2號"
    ///
    /// var comp2 = PostalAddress.Parse("台中市中區平等街150之1之1之1號");
    /// Console.WriteLine(comp2.GetFullNumber()); // "150之1之1之1號"
    /// </code>
    /// </example>
    public string GetFullNumber()
    {
        if (!Number.HasValue)
        {
            return string.Empty;
        }

        if (SubNumbers != null && SubNumbers.Count > 0)
        {
            var subNoStr = string.Join("之", SubNumbers);
            return $"{Number}之{subNoStr}號";
        }

        return $"{Number}號";
    }

    /// <summary>
    /// 取得基本地址（縣市+區+路/村里/歷史地名）
    /// </summary>
    /// <returns>基本地址字串</returns>
    /// <example>
    /// <code>
    /// var comp = PostalAddress.Parse("臺北市信義區市府路1號");
    /// Console.WriteLine(comp.GetBaseAddress()); // "臺北市信義區市府路"
    ///
    /// var comp2 = PostalAddress.Parse("高雄市阿蓮區再興23號");
    /// Console.WriteLine(comp2.GetBaseAddress()); // "高雄市阿蓮區再興"
    /// </code>
    /// </example>
    public string GetBaseAddress()
    {
        var parts = new List<string?>
        {
            City,
            District,
            Village ?? Road ?? Locality
        };
        return string.Concat(parts.Where(p => !string.IsNullOrEmpty(p)));
    }

    /// <summary>
    /// 驗證組件有效性
    /// </summary>
    /// <param name="address">PostalAddress 物件</param>
    /// <returns>驗證結果</returns>
    /// <example>
    /// <code>
    /// var comp = PostalAddress.Parse("臺北市信義區市府路1號");
    /// var validation = PostalAddress.Validate(comp);
    /// Console.WriteLine(validation.IsValidCity);      // true
    /// Console.WriteLine(validation.IsValidDistrict);  // true
    /// </code>
    /// </example>
    public static PostalAddressValidation Validate(PostalAddress address)
    {
        var validation = new PostalAddressValidation();

        if (address == null)
        {
            validation.Messages.Add("地址物件為空");
            return validation;
        }

        // 如果沒有縣市或行政區，無法驗證
        if (string.IsNullOrEmpty(address.City) && string.IsNullOrEmpty(address.District))
        {
            validation.Messages.Add("地址缺少縣市或行政區資訊");
            return validation;
        }

        try
        {
            // 驗證縣市
            if (!string.IsNullOrEmpty(address.City))
            {
                validation.IsValidCity = PostalRulesEngine.CityExists(address.City!);
                if (!validation.IsValidCity)
                {
                    validation.Messages.Add($"縣市「{address.City}」不存在");
                }
            }

            // 驗證縣市+行政區
            if (!string.IsNullOrEmpty(address.City) && !string.IsNullOrEmpty(address.District))
            {
                validation.IsValidDistrict = PostalRulesEngine.DistrictExists(address.City!, address.District!);
                if (!validation.IsValidDistrict)
                {
                    validation.Messages.Add($"行政區「{address.District}」在「{address.City}」中不存在");
                }
            }

            // 驗證路街
            if (!string.IsNullOrEmpty(address.City) && !string.IsNullOrEmpty(address.District) && !string.IsNullOrEmpty(address.Road))
            {
                validation.IsValidRoad = PostalRulesEngine.RoadExists(
                    address.City!, address.District!, address.Road!, address.Section);
                if (!validation.IsValidRoad)
                {
                    validation.Messages.Add($"路街「{address.Road}」在「{address.City}{address.District}」中不存在");
                }
                else if (address.Number.HasValue)
                {
                    // 如果有門牌號碼，使用結構化驗證檢查範圍
                    var structuredValidation = ValidateWithStructuredFields(address);
                    if (structuredValidation != null)
                    {
                        // 合併結構化驗證結果
                        validation.IsValidNumber = structuredValidation.IsValidNumber;
                        validation.IsValidLane = structuredValidation.IsValidLane;
                        validation.IsValidAlley = structuredValidation.IsValidAlley;
                        validation.MatchedRule = structuredValidation.MatchedRule;

                        if (!structuredValidation.IsValidNumber ||
                            !structuredValidation.IsValidLane ||
                            !structuredValidation.IsValidAlley)
                        {
                            validation.Messages.AddRange(structuredValidation.Messages);
                        }
                        else
                        {
                            validation.Messages.Add($"門牌號碼驗證通過（郵遞區號：{structuredValidation.MatchedRule?.ZipCode}）");
                        }
                    }
                }
            }

            // ✨ 驗證地區名稱（部落、眷村、聚落等）
            if (!string.IsNullOrEmpty(address.Locality))
            {
                var locationResult = ZipCode.Find(address.City! + address.District! + address.Locality!).ZipCode;
                validation.IsValidLocality = !string.IsNullOrEmpty(locationResult);

                if (validation.IsValidLocality)
                {
                    validation.Messages.Add($"「{address.Locality}」在資料庫中存在（可能是眷村、老聚落等歷史地名）");
                }
                else
                {
                    validation.Messages.Add($"「{address.Locality}」在資料庫中不存在");
                }
            }
        }
        catch (Exception ex)
        {
            validation.Messages.Add($"驗證時發生錯誤：{ex.Message}");
        }

        return validation;
    }

    /// <summary>
    /// 使用結構化欄位驗證地址（私有方法）
    /// </summary>
    private static PostalAddressValidation? ValidateWithStructuredFields(PostalAddress address)
    {
        if (string.IsNullOrEmpty(address.City) ||
            string.IsNullOrEmpty(address.District) ||
            string.IsNullOrEmpty(address.Road))
        {
            return null;
        }

        var validation = new PostalAddressValidation
        {
            IsValidCity = true,
            IsValidDistrict = true,
            IsValidRoad = true
        };

        try
        {
            // 查詢匹配的 postal_rules
            var rules = PostalLookup.QueryPostalRules(address.City!, address.District!, address.Road!);

            if (rules == null || rules.Count == 0)
            {
                // 沒有結構化規則，無法進行精確驗證
                return null;
            }

            // 解析巷號（從 Lane 字串解析出數字）
            int? laneNumber = null;
            if (!string.IsNullOrEmpty(address.Lane) && int.TryParse(address.Lane, out var ln))
            {
                laneNumber = ln;
            }

            // 解析弄號（從 Alley 字串解析出數字）
            int? alleyNumber = null;
            if (!string.IsNullOrEmpty(address.Alley) && int.TryParse(address.Alley, out var an))
            {
                alleyNumber = an;
            }

            // 解析附號（取第一個附號）
            int? subNumber = null;
            if (address.SubNumbers != null && address.SubNumbers.Count > 0)
            {
                subNumber = address.SubNumbers[0];
            }

            // 嘗試匹配所有規則
            foreach (var rule in rules)
            {
                bool matched = true;

                // 驗證巷號範圍
                if (laneNumber.HasValue)
                {
                    if (!rule.IsLaneInRange(laneNumber.Value))
                    {
                        matched = false;
                        continue;
                    }
                }
                else if (rule.LaneStart.HasValue)
                {
                    // 規則要求巷號，但地址沒有
                    matched = false;
                    continue;
                }

                // 驗證弄號範圍
                if (alleyNumber.HasValue)
                {
                    if (!rule.IsAlleyInRange(alleyNumber.Value))
                    {
                        matched = false;
                        continue;
                    }
                }
                else if (rule.AlleyStart.HasValue)
                {
                    // 規則要求弄號，但地址沒有
                    matched = false;
                    continue;
                }

                // 驗證門牌號碼範圍
                if (address.Number.HasValue)
                {
                    if (!rule.IsNumberInRange(address.Number.Value, subNumber))
                    {
                        matched = false;
                        continue;
                    }
                }

                // 通過所有驗證
                if (matched)
                {
                    validation.IsValidLane = true;
                    validation.IsValidAlley = true;
                    validation.IsValidNumber = true;
                    validation.MatchedRule = rule;
                    return validation;
                }
            }

            // 沒有規則匹配
            validation.IsValidLane = laneNumber.HasValue ? false : true;
            validation.IsValidAlley = alleyNumber.HasValue ? false : true;
            validation.IsValidNumber = false;

            if (laneNumber.HasValue && !validation.IsValidLane)
            {
                validation.Messages.Add($"巷號「{laneNumber}」不在投遞範圍內");
            }

            if (alleyNumber.HasValue && !validation.IsValidAlley)
            {
                validation.Messages.Add($"弄號「{alleyNumber}」不在投遞範圍內");
            }

            if (!validation.IsValidNumber && address.Number.HasValue)
            {
                var numberStr = subNumber.HasValue
                    ? $"{address.Number}之{subNumber}"
                    : address.Number.ToString();
                validation.Messages.Add($"門牌號碼「{numberStr}號」不在投遞範圍內");
            }

            return validation;
        }
        catch
        {
            // 結構化驗證失敗，返回 null（後退到傳統驗證）
            return null;
        }
    }

}

/// <summary>
/// 地址組件驗證結果
/// </summary>
public record PostalAddressValidation
{
    /// <summary>
    /// 縣市是否有效
    /// </summary>
    public bool IsValidCity { get; set; }

    /// <summary>
    /// 行政區是否有效
    /// </summary>
    public bool IsValidDistrict { get; set; }

    /// <summary>
    /// 路街是否有效
    /// </summary>
    public bool IsValidRoad { get; set; }

    /// <summary>
    /// 地區名稱是否在資料庫中存在
    /// </summary>
    public bool IsValidLocality { get; set; }

    /// <summary>
    /// 巷號是否有效（使用結構化驗證）
    /// </summary>
    public bool IsValidLane { get; set; } = true;

    /// <summary>
    /// 弄號是否有效（使用結構化驗證）
    /// </summary>
    public bool IsValidAlley { get; set; } = true;

    /// <summary>
    /// 門牌號碼是否有效（使用結構化驗證）
    /// </summary>
    public bool IsValidNumber { get; set; } = true;

    /// <summary>
    /// 匹配的投遞規則（如果有）
    /// </summary>
    public PostalRule? MatchedRule { get; set; }

    /// <summary>
    /// 驗證訊息
    /// </summary>
    public List<string> Messages { get; set; } = new();

    /// <summary>
    /// 是否全部有效
    /// </summary>
    public bool IsValid => IsValidCity && IsValidDistrict && (IsValidRoad || IsValidLocality) &&
                          IsValidLane && IsValidAlley && IsValidNumber;
}